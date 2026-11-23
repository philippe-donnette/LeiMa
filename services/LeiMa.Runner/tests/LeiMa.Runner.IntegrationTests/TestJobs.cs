using System.Text.Json;
using LeiMa.Runner.Domain;

namespace LeiMa.Runner.IntegrationTests;

/// <summary>Builds valid jobs for the tests.</summary>
internal static class TestJobs
{
    /// <summary>Parses JSON text into an element that stays valid after the parser is gone.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The element.</returns>
    public static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    /// <summary>Creates a queued job with one task per given type, each with its own parameters.</summary>
    /// <param name="taskTypes">The task types, in the order they run.</param>
    /// <returns>The job.</returns>
    public static Job WithTasks(params string[] taskTypes)
    {
        var tasks = taskTypes
            .Select((type, index) => new TaskDefinition(type, Json($$"""{"step":{{index + 1}}}"""), index == 0 ? TimeSpan.FromMinutes(5) : null))
            .ToList();
        return Job.Create("Nightly sync", "Copies customers", tasks).Value;
    }
}
