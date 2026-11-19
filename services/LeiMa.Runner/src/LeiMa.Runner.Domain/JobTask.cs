using System.Text.Json;
using LeiMa.Common;

namespace LeiMa.Runner.Domain;

/// <summary>One step of a <see cref="Job"/>: a call to a provider task API with JSON parameters.</summary>
public sealed class JobTask
{
    /// <summary>Initializes a new instance of the <see cref="JobTask"/> class; used by the ORM only.</summary>
    private JobTask()
    {
    }

    /// <summary>Initializes a new, pending task at <paramref name="position"/> in its job.</summary>
    /// <param name="position">The 1-based position in the job.</param>
    /// <param name="taskType">The trimmed task type.</param>
    /// <param name="parameters">The parameters, detached from any parsed document.</param>
    /// <param name="timeout">The timeout, or <c>null</c> for the task type default.</param>
    internal JobTask(int position, string taskType, JsonElement parameters, TimeSpan? timeout)
    {
        Id = Ids.NewId();
        Position = position;
        TaskType = taskType;
        Parameters = parameters;
        Timeout = timeout;
        Status = JobTaskStatus.Pending;
        Run = 1;
    }

    /// <summary>Gets the id of the task.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the 1-based position of the task in its job; tasks run in this order. It is the number used by <c>{{tasks.&lt;n&gt;...}}</c> placeholders and in error messages.</summary>
    public int Position { get; private set; }

    /// <summary>Gets the kind of task, resolved to endpoints by the task provider.</summary>
    public string TaskType { get; private set; } = string.Empty;

    /// <summary>Gets the parameters as a JSON object. Placeholders are kept as written and resolved just before the task runs.</summary>
    public JsonElement Parameters { get; private set; }

    /// <summary>Gets the task timeout, or <c>null</c> to use the default of the task type.</summary>
    public TimeSpan? Timeout { get; private set; }

    /// <summary>Gets the status of the task.</summary>
    public JobTaskStatus Status { get; private set; }

    /// <summary>Gets the output the provider reported on success; <c>null</c> until then.</summary>
    public JsonElement? Output { get; private set; }

    /// <summary>Gets the run number, which starts at 1 and increases when a restart resets the task; part of the idempotency key.</summary>
    public int Run { get; private set; }

    /// <summary>Gets the execution id the provider returned when the task was started; <c>null</c> until then.</summary>
    public string? ExecutionId { get; private set; }

    /// <summary>Gets the code of the failure, if the task failed.</summary>
    public string? ErrorCode { get; private set; }

    /// <summary>Gets the message of the failure, if the task failed.</summary>
    public string? ErrorMessage { get; private set; }
}
