using System.Text.Json;
using LeiMa.Common;
using LeiMa.Runner.Domain;

namespace LeiMa.Runner.UnitTests;

/// <summary>Tests the rules for building a <see cref="Job"/> with its tasks.</summary>
public class JobCreationTests
{
    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

    private static TaskDefinition ValidTask(string type = "provider.sync") => new(type, Json("""{"customerId":"c-1"}"""));

    private static Result<Job> CreateJob(string? name = "Nightly sync", string? description = null, params TaskDefinition[] tasks) =>
        Job.Create(name, description, tasks.Length == 0 ? [ValidTask()] : tasks);

    private static void AssertInvalid(Result<Job> result, string code)
    {
        Assert.True(result.IsFailure, "The job should have been refused.");
        Assert.Equal(code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void Create_ValidJob_IsQueuedWithPendingTasksNumberedInOrder()
    {
        var result = Job.Create("  Nightly sync  ", "  Copies customers  ", [ValidTask("a"), ValidTask("b"), ValidTask("c")]);

        Assert.True(result.IsSuccess);
        var job = result.Value;
        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Equal("Nightly sync", job.Name);
        Assert.Equal("Copies customers", job.Description);
        Assert.Equal(JobStatus.Queued, job.Status);
        Assert.Null(job.DeletedAt);
        Assert.Equal([1, 2, 3], job.Tasks.Select(t => t.Position));
        Assert.Equal(["a", "b", "c"], job.Tasks.Select(t => t.TaskType));
        Assert.All(job.Tasks, task =>
        {
            Assert.Equal(JobTaskStatus.Pending, task.Status);
            Assert.Equal(1, task.Run);
            Assert.Null(task.Output);
            Assert.Null(task.ExecutionId);
            Assert.Null(task.ErrorCode);
            Assert.NotEqual(Guid.Empty, task.Id);
        });
        Assert.Equal(3, job.Tasks.Select(t => t.Id).Distinct().Count());
    }

    [Fact]
    public void Create_ParametersWithPlaceholders_KeepsThemAsWritten()
    {
        var parameters = Json("""{"customerId":"{{tasks.1.output.items[0].id}}","count":"{{tasks.1.output.total}}"}""");

        var job = Job.Create("Sync", null, [ValidTask(), new TaskDefinition("provider.sync", parameters)]).Value;

        Assert.Equal("{{tasks.1.output.items[0].id}}", job.Tasks[1].Parameters.GetProperty("customerId").GetString());
        Assert.Equal("{{tasks.1.output.total}}", job.Tasks[1].Parameters.GetProperty("count").GetString());
    }

    [Fact]
    public void Create_SourceDocumentDisposedAfterwards_ParametersRemainReadable()
    {
        Job job;
        using (var document = JsonDocument.Parse("""{"a":1}"""))
        {
            job = Job.Create("Sync", null, [new TaskDefinition("provider.sync", document.RootElement)]).Value;
        }

        Assert.Equal(1, job.Tasks[0].Parameters.GetProperty("a").GetInt32());
    }

    [Fact]
    public void Create_BlankDescription_BecomesNull()
    {
        Assert.Null(CreateJob(description: "   ").Value.Description);
    }

    [Fact]
    public void Create_TimeoutGiven_IsKept()
    {
        var job = Job.Create("Sync", null, [new TaskDefinition("provider.sync", Json("{}"), TimeSpan.FromMinutes(5))]).Value;

        Assert.Equal(TimeSpan.FromMinutes(5), job.Tasks[0].Timeout);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingName_IsRefused(string? name) => AssertInvalid(CreateJob(name), "job.name_required");

    [Fact]
    public void Create_NameTooLong_IsRefused() =>
        AssertInvalid(CreateJob(new string('n', Job.NameMaxLength + 1)), "job.name_too_long");

    [Fact]
    public void Create_NameAtLimit_IsAccepted() =>
        Assert.True(CreateJob(new string('n', Job.NameMaxLength)).IsSuccess);

    [Fact]
    public void Create_DescriptionTooLong_IsRefused() =>
        AssertInvalid(CreateJob(description: new string('d', Job.DescriptionMaxLength + 1)), "job.description_too_long");

    [Fact]
    public void Create_NoTasks_IsRefused() => AssertInvalid(Job.Create("Sync", null, []), "job.tasks_required");

    [Fact]
    public void Create_NullTasks_IsRefused() => AssertInvalid(Job.Create("Sync", null, null), "job.tasks_required");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_MissingTaskType_IsRefused(string? taskType) =>
        AssertInvalid(Job.Create("Sync", null, [new TaskDefinition(taskType!, Json("{}"))]), "task.type_required");

    [Fact]
    public void Create_TaskTypeTooLong_IsRefused() =>
        AssertInvalid(Job.Create("Sync", null, [ValidTask(new string('t', Job.TaskTypeMaxLength + 1))]), "task.type_too_long");

    [Theory]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    [InlineData("42")]
    [InlineData("null")]
    public void Create_ParametersNotAnObject_IsRefused(string json) =>
        AssertInvalid(Job.Create("Sync", null, [new TaskDefinition("provider.sync", Json(json))]), "task.parameters_invalid");

    [Fact]
    public void Create_DefaultParameters_IsRefused() =>
        AssertInvalid(Job.Create("Sync", null, [new TaskDefinition("provider.sync", default)]), "task.parameters_invalid");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_TimeoutNotPositive_IsRefused(int seconds) =>
        AssertInvalid(
            Job.Create("Sync", null, [new TaskDefinition("provider.sync", Json("{}"), TimeSpan.FromSeconds(seconds))]),
            "task.timeout_invalid");

    [Fact]
    public void Create_SecondTaskInvalid_NamesItsPosition()
    {
        var result = Job.Create("Sync", null, [ValidTask(), new TaskDefinition("", Json("{}"))]);

        AssertInvalid(result, "task.type_required");
        Assert.Contains("Task 2", result.Error.Message);
    }

    [Fact]
    public void Create_PlaceholderToAnEarlierTask_IsAcceptedAndStoredUnchanged()
    {
        const string parameters = """{"customerId":"{{tasks.1.output.items[0].id}}","note":"of {{tasks.1.output.total}}","nested":{"list":["{{tasks.2.output.x}}"]}}""";

        var result = Job.Create("Sync", null, [ValidTask(), ValidTask(), new TaskDefinition("provider.sync", Json(parameters))]);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.True(JsonElement.DeepEquals(Json(parameters), result.Value.Tasks[2].Parameters));
    }

    [Fact]
    public void Create_PlaceholderToALaterTask_IsRefusedNamingTheTask()
    {
        var result = Job.Create("Sync", null, [new TaskDefinition("a", Json("""{"id":"{{tasks.2.output.id}}"}""")), ValidTask()]);

        AssertInvalid(result, Job.PlaceholderReferenceInvalidCode);
        Assert.Contains("Task 1", result.Error.Message);
        Assert.Contains("task 2", result.Error.Message);
    }

    [Fact]
    public void Create_PlaceholderToItsOwnTask_IsRefused()
    {
        var result = Job.Create("Sync", null, [ValidTask(), new TaskDefinition("b", Json("""{"id":"{{tasks.2.output.id}}"}"""))]);

        AssertInvalid(result, Job.PlaceholderReferenceInvalidCode);
        Assert.Contains("Task 2", result.Error.Message);
    }

    [Fact]
    public void Create_PlaceholderToAMissingTask_IsRefused()
    {
        var result = Job.Create("Sync", null, [ValidTask(), new TaskDefinition("b", Json("""{"id":"{{tasks.9.output.id}}"}"""))]);

        AssertInvalid(result, Job.PlaceholderReferenceInvalidCode);
    }

    [Theory]
    [InlineData("""{"a":{"b":["fine",{"c":"{{tasks.5.output.id}}"}]}}""")]
    [InlineData("""{"list":["{{tasks.2.output.id}}"]}""")]
    [InlineData("""{"text":"before {{tasks.3.output.id}} after"}""")]
    public void Create_ForwardReferenceDeepInTheParameters_IsRefused(string parameters) =>
        AssertInvalid(
            Job.Create("Sync", null, [ValidTask(), new TaskDefinition("b", Json(parameters))]),
            Job.PlaceholderReferenceInvalidCode);

    [Theory]
    [InlineData("""{"id":"{{tasks.1.output.id"}""")]
    [InlineData("""{"id":"{{tasks.1.output}}"}""")]
    [InlineData("""{"id":"{{ tasks.1.output.id }}"}""")]
    [InlineData("""{"list":[{"id":"{{tasks.x.output.id}}"}]}""")]
    public void Create_MalformedPlaceholder_IsRefusedNamingTheTask(string parameters)
    {
        var result = Job.Create("Sync", null, [ValidTask(), new TaskDefinition("b", Json(parameters))]);

        AssertInvalid(result, "task.placeholder_invalid");
        Assert.Contains("Task 2", result.Error.Message);
    }

    [Fact]
    public void Create_EscapedBracesAndPropertyNames_AreNotPlaceholders()
    {
        const string parameters = """{"text":"write \\{{tasks.9.output.id}} as text","{{tasks.9.output.name}}":1,"number":5,"flag":true,"nothing":null}""";

        var result = Job.Create("Sync", null, [new TaskDefinition("a", Json(parameters))]);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
    }

    [Fact]
    public void Create_ExactlyTheMaximumNumberOfTasks_IsAccepted()
    {
        var tasks = Enumerable.Range(0, Job.MaxTasks).Select(_ => ValidTask()).ToList();

        var result = Job.Create("Sync", null, tasks);

        Assert.True(result.IsSuccess);
        Assert.Equal(Job.MaxTasks, result.Value.Tasks.Count);
    }

    [Fact]
    public void Create_MoreThanTheMaximumNumberOfTasks_IsRefused()
    {
        var tasks = Enumerable.Range(0, Job.MaxTasks + 1).Select(_ => ValidTask()).ToList();

        AssertInvalid(Job.Create("Sync", null, tasks), "job.too_many_tasks");
    }
}
