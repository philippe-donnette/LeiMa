using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace LeiMa.Common.Testing;

/// <summary>
/// Base <see cref="WebApplicationFactory{TEntryPoint}"/> for service integration tests: runs in the
/// <c>Testing</c> environment, replaces the <see cref="TimeProvider"/> with a controllable <see cref="Clock"/>
/// and lets tests override configuration with <see cref="WithSetting"/>.
/// </summary>
/// <typeparam name="TEntryPoint">The service's <c>Program</c> class.</typeparam>
public class LeiMaWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    private readonly Dictionary<string, string?> _settings = [];

    /// <summary>The clock the service sees. Advance it to test time-dependent behavior.</summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    /// <summary>Overrides a configuration value (for example a connection string). Call before the first request.</summary>
    public LeiMaWebApplicationFactory<TEntryPoint> WithSetting(string key, string? value)
    {
        _settings[key] = value;
        return this;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_settings));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }
}
