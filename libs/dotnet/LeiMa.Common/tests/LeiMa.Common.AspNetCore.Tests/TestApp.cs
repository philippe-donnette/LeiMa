using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LeiMa.Common.AspNetCore.Tests;

/// <summary>Starts an in-memory web app that uses the LeiMa defaults.</summary>
internal sealed class TestApp : IAsyncDisposable
{
    private readonly WebApplication _app;

    private TestApp(WebApplication app, HttpClient client)
    {
        _app = app;
        Client = client;
    }

    public HttpClient Client { get; }

    public static async Task<TestApp> StartAsync(
        Action<WebApplicationBuilder>? configureBuilder = null,
        Action<WebApplication>? mapEndpoints = null,
        IDictionary<string, string?>? settings = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
        });
        builder.WebHost.UseTestServer();
        if (settings is not null)
        {
            builder.Configuration.AddInMemoryCollection(settings);
        }

        builder.AddLeiMaDefaults();
        configureBuilder?.Invoke(builder);

        var app = builder.Build();
        app.UseLeiMaDefaults();
        mapEndpoints?.Invoke(app);
        await app.StartAsync();

        return new TestApp(app, app.GetTestClient());
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
    }
}
