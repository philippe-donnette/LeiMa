using LeiMa.Runner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LeiMa.Runner.Infrastructure;

/// <summary>Operations on the Runner database as a whole, used by the migrator and by the integration tests.</summary>
public static class RunnerDatabase
{
    /// <summary>
    /// Applies every pending EF Core migration. It never opens a transaction of its own, because EF Core manages one and
    /// takes a database-wide migration lock; running it again with nothing pending changes nothing (ADR 0005).
    /// </summary>
    /// <param name="services">The service provider built from <see cref="DependencyInjection.AddRunnerInfrastructure"/>.</param>
    /// <param name="logger">Receives what was applied.</param>
    /// <param name="cancellationToken">Cancels the migration.</param>
    /// <returns>A task that completes when the database is up to date.</returns>
    public static async Task MigrateAsync(IServiceProvider services, ILogger logger, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<RunnerDbContext>().Database;

        var pending = (await database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("The Runner database is up to date; nothing to apply.");
            return;
        }

        logger.LogInformation("Applying {Count} pending migration(s): {Migrations}", pending.Count, pending);
        await database.MigrateAsync(cancellationToken);
        logger.LogInformation("The Runner database is up to date.");
    }
}
