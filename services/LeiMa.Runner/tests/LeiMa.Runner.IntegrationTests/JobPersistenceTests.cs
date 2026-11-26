using System.Text.Json;
using LeiMa.Common;
using LeiMa.Common.EntityFrameworkCore;
using LeiMa.Common.Testing;
using LeiMa.Runner.Application.Repository;
using LeiMa.Runner.Domain;
using LeiMa.Runner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeiMa.Runner.IntegrationTests;

/// <summary>Saves and loads jobs through <see cref="IJobRepository"/> against a real PostgreSQL database.</summary>
[Collection(PostgresCollection.Name)]
public class JobPersistenceTests(PostgreSqlFixture postgres)
{
    private static IJobRepository Repository(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IJobRepository>();

    private static RunnerDbContext Context(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<RunnerDbContext>();

    private static async Task<Job> LoadAsync(AsyncServiceScope scope, Guid id) =>
        await Repository(scope).GetAsync(id, CancellationToken.None) ?? throw new InvalidOperationException("The job was not found.");

    private static async Task SaveNewAsync(PostgresTestHost host, Job job)
    {
        await using var scope = host.NewScope();
        Repository(scope).Add(job);
        var saved = await Repository(scope).SaveChangesAsync(CancellationToken.None);
        Assert.True(saved.IsSuccess, "The new job should have been saved.");
    }

    [RequiresDockerFact]
    public async Task SaveAndLoad_JobWithThreeTasks_ComesBackWithTheSameDataInPositionOrder()
    {
        await using var host = await RunnerHost.CreateAsync(postgres);
        var job = TestJobs.WithTasks("a", "b", "c");
        await SaveNewAsync(host, job);

        await using var scope = host.NewScope();
        var loaded = await LoadAsync(scope, job.Id);

        Assert.Equal(job.Name, loaded.Name);
        Assert.Equal(job.Description, loaded.Description);
        Assert.Equal(JobStatus.Queued, loaded.Status);
        Assert.Null(loaded.DeletedAt);
        Assert.Equal([1, 2, 3], loaded.Tasks.Select(t => t.Position));
        Assert.Equal(["a", "b", "c"], loaded.Tasks.Select(t => t.TaskType));
        Assert.Equal(job.Tasks.Select(t => t.Id), loaded.Tasks.Select(t => t.Id));
        Assert.Equal(TimeSpan.FromMinutes(5), loaded.Tasks[0].Timeout);
        Assert.Null(loaded.Tasks[1].Timeout);
        Assert.All(loaded.Tasks, task =>
        {
            Assert.Equal(JobTaskStatus.Pending, task.Status);
            Assert.Equal(1, task.Run);
            Assert.Null(task.Output);
            Assert.Null(task.ExecutionId);
            Assert.Null(task.ErrorCode);
            Assert.Null(task.ErrorMessage);
        });
    }

    [RequiresDockerFact]
    public async Task Load_TasksStoredInAnotherOrder_ComeBackInPositionOrder()
    {
        await using var host = await RunnerHost.CreateAsync(postgres);
        var job = TestJobs.WithTasks("a", "b", "c");
        await SaveNewAsync(host, job);

        // Without an explicit ORDER BY, PostgreSQL may still return the tasks in position order, because the unique
        // (JobId, Position) index is a convenient way to find them. Drop it so that only the query's own ordering counts,
        // then reverse the stored positions (1,2,3 become 3,2,1).
        await host.ExecuteAsync("""DROP INDEX "IX_job_tasks_JobId_Position" """);
        await host.ExecuteAsync("""UPDATE job_tasks SET "Position" = 4 - "Position" """);

        await using var scope = host.NewScope();
        var loaded = await LoadAsync(scope, job.Id);

        Assert.Equal([1, 2, 3], loaded.Tasks.Select(t => t.Position));
        Assert.Equal(["c", "b", "a"], loaded.Tasks.Select(t => t.TaskType));
    }

    [RequiresDockerFact]
    public async Task SaveAndLoad_ParametersWithPlaceholders_AreUnchanged()
    {
        await using var host = await RunnerHost.CreateAsync(postgres);
        const string json = """
            {"customerId":"{{tasks.1.output.items[0].id}}","note":"first of {{tasks.1.output.total}}",
             "nested":{"list":[1,2.5,{"x":null}],"text":"ünïcode \"quoted\" \\ slash"},"flag":true,"count":3}
            """;
        var job = Job.Create("Sync", null, [new TaskDefinition("a", TestJobs.Json("""{"seed":1}""")), new TaskDefinition("b", TestJobs.Json(json))]).Value;
        await SaveNewAsync(host, job);

        await using var scope = host.NewScope();
        var loaded = await LoadAsync(scope, job.Id);

        var parameters = loaded.Tasks[1].Parameters;
        Assert.True(JsonElement.DeepEquals(TestJobs.Json(json), parameters), "The JSON should be unchanged apart from formatting.");
        Assert.Equal("{{tasks.1.output.items[0].id}}", parameters.GetProperty("customerId").GetString());
        Assert.Equal("first of {{tasks.1.output.total}}", parameters.GetProperty("note").GetString());
    }

    [RequiresDockerFact]
    public async Task Get_UnknownId_ReturnsNull()
    {
        await using var host = await RunnerHost.CreateAsync(postgres);

        await using var scope = host.NewScope();

        Assert.Null(await Repository(scope).GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [RequiresDockerFact]
    public async Task Save_JobChangedByAnotherWriter_ReturnsConflictAndKeepsTheFirstChange()
    {
        await using var host = await RunnerHost.CreateAsync(postgres);
        var job = TestJobs.WithTasks("a", "b");
        await SaveNewAsync(host, job);

        await using var first = host.NewScope();
        await using var second = host.NewScope();
        var firstCopy = await LoadAsync(first, job.Id);
        var secondCopy = await LoadAsync(second, job.Id);

        Context(first).Entry(firstCopy).Property(j => j.Name).CurrentValue = "first writer";
        Assert.True((await Repository(first).SaveChangesAsync(CancellationToken.None)).IsSuccess);

        Context(second).Entry(secondCopy).Property(j => j.Name).CurrentValue = "second writer";
        var result = await Repository(second).SaveChangesAsync(CancellationToken.None);

        Assert.True(result.IsFailure, "The second save should have lost the race.");
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(IJobRepository.ConcurrencyConflictCode, result.Error.Code);

        await using var check = host.NewScope();
        Assert.Equal("first writer", (await LoadAsync(check, job.Id)).Name);
    }

    [RequiresDockerFact]
    public async Task Save_TaskChangedByAnotherWriter_ReturnsConflictAndKeepsTheFirstChange()
    {
        await using var host = await RunnerHost.CreateAsync(postgres);
        var job = TestJobs.WithTasks("a", "b");
        await SaveNewAsync(host, job);

        await using var first = host.NewScope();
        await using var second = host.NewScope();
        var firstCopy = await LoadAsync(first, job.Id);
        var secondCopy = await LoadAsync(second, job.Id);

        Context(first).Entry(firstCopy.Tasks[0]).Property(t => t.ErrorMessage).CurrentValue = "first writer";
        Assert.True((await Repository(first).SaveChangesAsync(CancellationToken.None)).IsSuccess);

        Context(second).Entry(secondCopy.Tasks[0]).Property(t => t.ErrorMessage).CurrentValue = "second writer";
        var result = await Repository(second).SaveChangesAsync(CancellationToken.None);

        Assert.True(result.IsFailure, "The second save should have lost the race.");
        Assert.Equal(IJobRepository.ConcurrencyConflictCode, result.Error.Code);

        await using var check = host.NewScope();
        Assert.Equal("first writer", (await LoadAsync(check, job.Id)).Tasks[0].ErrorMessage);
    }

    [RequiresDockerFact]
    public async Task Save_NewJob_StoresCreatedAndUpdatedTimeInUtcFromTheClock()
    {
        await using var host = await RunnerHost.CreateAsync(postgres);
        var job = TestJobs.WithTasks("a", "b");
        await SaveNewAsync(host, job);

        await using var scope = host.NewScope();
        var loaded = await LoadAsync(scope, job.Id);

        foreach (var entity in new object[] { loaded }.Concat(loaded.Tasks))
        {
            Assert.Equal(RunnerHost.StartTime, Context(scope).CreatedAt(entity));
            Assert.Equal(RunnerHost.StartTime, Context(scope).UpdatedAt(entity));
            Assert.Equal(TimeSpan.Zero, Context(scope).CreatedAt(entity).Offset);
        }
    }

    [RequiresDockerFact]
    public async Task Save_ChangedJob_MovesUpdatedTimeAndKeepsCreatedTime()
    {
        await using var host = await RunnerHost.CreateAsync(postgres);
        var job = TestJobs.WithTasks("a");
        await SaveNewAsync(host, job);

        host.Clock.Advance(TimeSpan.FromMinutes(5));
        await using (var scope = host.NewScope())
        {
            var loaded = await LoadAsync(scope, job.Id);
            Context(scope).Entry(loaded).Property(j => j.Name).CurrentValue = "Renamed";
            Assert.True((await Repository(scope).SaveChangesAsync(CancellationToken.None)).IsSuccess);
        }

        await using var check = host.NewScope();
        var reloaded = await LoadAsync(check, job.Id);
        Assert.Equal(RunnerHost.StartTime, Context(check).CreatedAt(reloaded));
        Assert.Equal(RunnerHost.StartTime.AddMinutes(5), Context(check).UpdatedAt(reloaded));
    }

    [RequiresDockerFact]
    public async Task Save_TimeWithAnOffset_IsStoredAndReadBackAsUtc()
    {
        await using var host = await RunnerHost.CreateAsync(postgres);
        var job = TestJobs.WithTasks("a");
        await SaveNewAsync(host, job);
        var deletedAt = new DateTimeOffset(2026, 11, 20, 14, 0, 0, TimeSpan.FromHours(2));

        await using (var scope = host.NewScope())
        {
            var loaded = await LoadAsync(scope, job.Id);
            Context(scope).Entry(loaded).Property(j => j.DeletedAt).CurrentValue = deletedAt;
            Assert.True((await Repository(scope).SaveChangesAsync(CancellationToken.None)).IsSuccess);
        }

        await using var check = host.NewScope();
        var reloaded = (await LoadAsync(check, job.Id)).DeletedAt;
        Assert.Equal(deletedAt, reloaded);
        Assert.Equal(TimeSpan.Zero, reloaded!.Value.Offset);
    }
}
