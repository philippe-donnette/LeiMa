using LeiMa.Common.EntityFrameworkCore;
using LeiMa.Runner.Application.Repository;
using LeiMa.Runner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeiMa.Runner.Infrastructure;

/// <summary>Registers the Infrastructure services of the Runner; called by the composition roots (the Api and the migrator).</summary>
public static class DependencyInjection
{
    /// <summary>The name of the connection string of the Runner database; the AppHost injects it under this name.</summary>
    public const string ConnectionStringName = "runner";

    /// <summary>
    /// Registers the database context (PostgreSQL, through the LeiMa conventions) and the persistence ports.
    /// The connection string is read when the database is first used, so a host that never touches the database starts without one.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddRunnerInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLeiMaEntityFrameworkCore();
        services.AddDbContext<RunnerDbContext>((provider, options) =>
        {
            var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"The connection string '{ConnectionStringName}' is not configured.");
            options.UseLeiMaNpgsql(provider, connectionString);
        });
        services.AddScoped<IJobRepository, JobRepository>();

        return services;
    }
}
