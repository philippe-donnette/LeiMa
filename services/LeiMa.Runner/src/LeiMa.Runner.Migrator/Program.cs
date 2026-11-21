using LeiMa.Runner.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Applies the pending migrations of the Runner database and exits: 0 on success, 1 on failure (ADR 0005).
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRunnerInfrastructure();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("LeiMa.Runner.Migrator");

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

try
{
    await RunnerDatabase.MigrateAsync(host.Services, logger, cancellation.Token);
    return 0;
}
catch (Exception exception)
{
    logger.LogCritical(exception, "Migrating the Runner database failed.");
    return 1;
}
