using LeiMa.Common.Testing;
using LeiMa.Runner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LeiMa.Runner.IntegrationTests;

/// <summary>Applies the migrations of the Runner database to an empty PostgreSQL database and checks the result.</summary>
[Collection(PostgresCollection.Name)]
public class MigrationTests(PostgreSqlFixture postgres)
{
    private const string HistoryQuery = """SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" """;

    private const string SchemaQuery = """
        SELECT table_name || '.' || column_name || ':' || data_type || ':' || is_nullable
        FROM information_schema.columns WHERE table_schema = 'public' ORDER BY 1
        """;

    private const string IndexQuery = "SELECT indexname || ' ' || indexdef FROM pg_indexes WHERE schemaname = 'public' ORDER BY 1";

    /// <summary>Checks that the history table lists every migration of the assembly exactly once, however many there are.</summary>
    private static async Task AssertEveryMigrationRecordedOnceAsync(RunnerTestHost host)
    {
        await using var scope = host.NewScope();
        var known = scope.ServiceProvider.GetRequiredService<RunnerDbContext>().Database.GetMigrations();
        var recorded = await host.QueryAsync(HistoryQuery);

        Assert.NotEmpty(known);
        Assert.Equal(known.Order(StringComparer.Ordinal), recorded.Order(StringComparer.Ordinal));
    }

    [RequiresDockerFact]
    public async Task Migrate_EmptyDatabase_CreatesTheTablesAndRecordsTheMigrations()
    {
        await using var host = await RunnerTestHost.CreateAsync(postgres, migrate: false);

        await host.MigrateAsync();

        var tables = await host.QueryAsync("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY 1");
        Assert.Equal(["__EFMigrationsHistory", "job_tasks", "jobs"], tables);
        await AssertEveryMigrationRecordedOnceAsync(host);
    }

    [RequiresDockerFact]
    public async Task Migrate_RunTwice_ChangesNothingTheSecondTime()
    {
        await using var host = await RunnerTestHost.CreateAsync(postgres, migrate: false);
        await host.MigrateAsync();
        var historyBefore = await host.QueryAsync(HistoryQuery);
        var schemaBefore = await host.QueryAsync(SchemaQuery);
        var indexesBefore = await host.QueryAsync(IndexQuery);

        await host.MigrateAsync();

        Assert.Equal(historyBefore, await host.QueryAsync(HistoryQuery));
        Assert.Equal(schemaBefore, await host.QueryAsync(SchemaQuery));
        Assert.Equal(indexesBefore, await host.QueryAsync(IndexQuery));
        await using var scope = host.NewScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<RunnerDbContext>().Database.GetPendingMigrationsAsync());
    }

    [RequiresDockerFact]
    public async Task Migrate_ThreeAtTheSameTime_AllSucceedAndApplyEachMigrationOnce()
    {
        await using var host = await RunnerTestHost.CreateAsync(postgres, migrate: false);

        await Task.WhenAll(host.MigrateAsync(), host.MigrateAsync(), host.MigrateAsync());

        await AssertEveryMigrationRecordedOnceAsync(host);
    }

    [RequiresDockerFact]
    public async Task Model_AfterMigrating_HasNoChangesMissingFromTheMigrations()
    {
        await using var host = await RunnerTestHost.CreateAsync(postgres);

        await using var scope = host.NewScope();

        Assert.False(
            scope.ServiceProvider.GetRequiredService<RunnerDbContext>().Database.HasPendingModelChanges(),
            "The model differs from the last migration; add a migration for the change.");
    }

    [RequiresDockerFact]
    public async Task Schema_JsonAndTimeColumns_HaveTheExpectedTypes()
    {
        await using var host = await RunnerTestHost.CreateAsync(postgres);

        var json = await host.QueryAsync(
            """SELECT column_name || ':' || data_type FROM information_schema.columns WHERE table_name = 'job_tasks' AND column_name IN ('Parameters', 'Output') ORDER BY 1""");
        var notUtc = await host.QueryAsync(
            """SELECT table_name || '.' || column_name FROM information_schema.columns WHERE table_schema = 'public' AND column_name LIKE '%At' AND data_type <> 'timestamp with time zone'""");

        Assert.Equal(["Output:jsonb", "Parameters:jsonb"], json);
        Assert.Empty(notUtc);
    }

    [RequiresDockerFact]
    public async Task Schema_TwoTasksAtTheSamePositionOfOneJob_AreRejected()
    {
        await using var host = await RunnerTestHost.CreateAsync(postgres);
        var jobId = Guid.NewGuid();
        await host.ExecuteAsync(
            """INSERT INTO jobs ("Id", "Name", "Status", "CreatedAt", "UpdatedAt") VALUES (@id, 'Sync', 'Queued', now(), now())""",
            ("id", jobId));
        const string insertTask =
            """INSERT INTO job_tasks ("Id", "JobId", "Position", "TaskType", "Parameters", "Status", "Run", "CreatedAt", "UpdatedAt") VALUES (@id, @jobId, 1, 'a', '{}'::jsonb, 'Pending', 1, now(), now())""";
        await host.ExecuteAsync(insertTask, ("id", Guid.NewGuid()), ("jobId", jobId));

        var error = await Assert.ThrowsAsync<PostgresException>(
            () => host.ExecuteAsync(insertTask, ("id", Guid.NewGuid()), ("jobId", jobId)));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
    }
}
