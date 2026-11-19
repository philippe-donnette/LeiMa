using System.Text.Json;

namespace LeiMa.Runner.Domain;

/// <summary>What the caller asks for when it adds a task to a new job; the position comes from the order in the list.</summary>
/// <param name="TaskType">The kind of task, resolved to endpoints by the task provider.</param>
/// <param name="Parameters">The task parameters: a JSON object whose string values may contain placeholders such as <c>{{tasks.1.output.id}}</c>.</param>
/// <param name="Timeout">The task timeout, or <c>null</c> to use the default of the task type.</param>
public sealed record TaskDefinition(string TaskType, JsonElement Parameters, TimeSpan? Timeout = null);
