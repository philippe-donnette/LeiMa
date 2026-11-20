using LeiMa.Common;
using LeiMa.Runner.Application.Repository;
using LeiMa.Runner.Domain;
using Microsoft.EntityFrameworkCore;

namespace LeiMa.Runner.Infrastructure.Persistence;

/// <summary>The EF Core implementation of <see cref="IJobRepository"/>; the <see cref="DbContext"/> tracks the changes until they are saved.</summary>
internal sealed class JobRepository : IJobRepository
{
    /// <summary>The model and change tracker of the current scope.</summary>
    private readonly RunnerDbContext _db;

    /// <summary>Initializes a new instance of the <see cref="JobRepository"/> class.</summary>
    /// <param name="db">The database context of the current scope.</param>
    public JobRepository(RunnerDbContext db) => _db = db;

    /// <inheritdoc />
    public void Add(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);
        _db.Jobs.Add(job);
    }

    /// <inheritdoc />
    public Task<Job?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Jobs
            .Include(job => job.Tasks.OrderBy(task => task.Position))
            .FirstOrDefaultAsync(job => job.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Error.Conflict(
                IJobRepository.ConcurrencyConflictCode,
                "The job was changed by someone else after it was loaded. Nothing was saved; load it again and retry.");
        }
    }
}
