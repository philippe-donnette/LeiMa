using LeiMa.Common;
using LeiMa.Runner.Domain;

namespace LeiMa.Runner.Application.Repository;

/// <summary>
/// Loads and saves the <see cref="Job"/> aggregate: a job together with all of its tasks.
/// Changes made to a job returned by <see cref="GetAsync"/> are written by <see cref="SaveChangesAsync"/>,
/// so a use case commits everything it did in one atomic step. Implementations hide the data-access technology.
/// </summary>
public interface IJobRepository
{
    /// <summary>The code of the <see cref="ErrorType.Conflict"/> error returned when a save loses against another writer.</summary>
    public const string ConcurrencyConflictCode = "job.concurrency_conflict";

    /// <summary>Registers a new job, with its tasks, to be stored by the next <see cref="SaveChangesAsync"/>.</summary>
    /// <param name="job">The job to store.</param>
    void Add(Job job);

    /// <summary>Loads a job with its tasks in position order. Soft deleted jobs are returned too; filtering them is the caller's decision.</summary>
    /// <param name="id">The id of the job.</param>
    /// <param name="cancellationToken">Cancels the load.</param>
    /// <returns>The job, or <c>null</c> if no job has that id.</returns>
    Task<Job?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Atomically writes every new job and every change made to a loaded job since it was loaded.</summary>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>
    /// Success, or an <see cref="ErrorType.Conflict"/> error with code <see cref="ConcurrencyConflictCode"/>
    /// when another writer changed a job after it was loaded; nothing was saved in that case.
    /// </returns>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
