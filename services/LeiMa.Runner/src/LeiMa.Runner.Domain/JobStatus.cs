namespace LeiMa.Runner.Domain;

/// <summary>Where a <see cref="Job"/> is in its life (ADR 0004).</summary>
public enum JobStatus
{
    /// <summary>The job is waiting to be claimed by a worker.</summary>
    Queued = 0,

    /// <summary>A worker is executing the job's tasks.</summary>
    Running,

    /// <summary>The job was stopped after its running task finished; it can be restarted.</summary>
    Stopped,

    /// <summary>All tasks succeeded.</summary>
    Succeeded,

    /// <summary>A task failed or timed out; the job can be restarted.</summary>
    Failed,

    /// <summary>The job was cancelled before it ran or while stopped; this is final.</summary>
    Cancelled,
}
