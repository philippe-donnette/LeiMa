namespace LeiMa.Runner.Domain;

/// <summary>Where a <see cref="JobTask"/> is in its life (ADR 0004).</summary>
public enum JobTaskStatus
{
    /// <summary>The task has not started.</summary>
    Pending = 0,

    /// <summary>The task was started on the provider and is being polled.</summary>
    Running,

    /// <summary>The provider reported success and the output is stored.</summary>
    Succeeded,

    /// <summary>The provider reported failure, the task timed out or its parameters could not be resolved.</summary>
    Failed,

    /// <summary>The task did not run because its job was cancelled.</summary>
    Cancelled,
}
