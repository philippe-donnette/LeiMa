using LeiMa.Common.Testing;
using LeiMa.Runner.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeiMa.Runner.IntegrationTests;

/// <summary>Creates the shared test host with the Runner's own registration, migration and clock start.</summary>
internal static class RunnerHost
{
    /// <summary>The time the fake clock starts at.</summary>
    public static readonly DateTimeOffset StartTime = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Creates a new database in the shared container and, unless told not to, migrates it.</summary>
    /// <param name="postgres">The shared PostgreSQL fixture.</param>
    /// <param name="migrate">Whether to apply the migrations before returning.</param>
    /// <returns>The host; dispose it when done.</returns>
    public static Task<PostgresTestHost> CreateAsync(PostgreSqlFixture postgres, bool migrate = true) =>
        PostgresTestHost.CreateAsync(
            postgres,
            "runner",
            services => services.AddRunnerInfrastructure(),
            provider => RunnerDatabase.MigrateAsync(provider, NullLogger.Instance, CancellationToken.None),
            migrateNow: migrate,
            startTime: StartTime);
}
