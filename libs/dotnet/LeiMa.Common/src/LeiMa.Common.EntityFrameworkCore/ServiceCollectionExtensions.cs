using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LeiMa.Common.EntityFrameworkCore;

/// <summary>Registration helpers for the LeiMa EF Core conventions.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="AuditTimestampInterceptor"/> (and <see cref="TimeProvider.System"/> when none is registered).</summary>
    public static IServiceCollection AddLeiMaEntityFrameworkCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<AuditTimestampInterceptor>();
        return services;
    }

    /// <summary>
    /// Configures Npgsql with transient-failure retries and adds the LeiMa interceptors.
    /// Use inside <c>AddDbContext</c>: <c>(sp, options) =&gt; options.UseLeiMaNpgsql(sp, connectionString)</c>.
    /// </summary>
    public static DbContextOptionsBuilder UseLeiMaNpgsql(
        this DbContextOptionsBuilder options, IServiceProvider services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return options
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
            .AddInterceptors(services.GetRequiredService<AuditTimestampInterceptor>());
    }
}
