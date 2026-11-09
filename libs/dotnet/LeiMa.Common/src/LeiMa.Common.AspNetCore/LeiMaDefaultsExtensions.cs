using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

namespace LeiMa.Common.AspNetCore;

/// <summary>Entry points that apply the standard LeiMa service setup.</summary>
public static class LeiMaDefaultsExtensions
{
    /// <summary>Tag that marks a health check as part of readiness (<c>/health/ready</c>).</summary>
    public const string ReadyTag = "ready";

    /// <summary>Liveness endpoint: the process is up. Runs no checks.</summary>
    public const string LivePath = "/health/live";

    /// <summary>Readiness endpoint: runs the health checks tagged <see cref="ReadyTag"/>.</summary>
    public const string ReadyPath = "/health/ready";

    /// <summary>
    /// Registers logging (Serilog), OpenTelemetry traces and metrics, ProblemDetails with a global exception
    /// handler, health checks, and the authentication and authorization services.
    /// Traces and metrics are exported over OTLP when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is configured.
    /// </summary>
    public static WebApplicationBuilder AddLeiMaDefaults(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var serviceName = builder.Environment.ApplicationName;
        var isDevelopment = builder.Environment.IsDevelopment();

        builder.Services.AddSerilog((services, logger) =>
        {
            logger
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName);

            if (isDevelopment)
            {
                logger.WriteTo.Console();
            }
            else
            {
                logger.WriteTo.Console(new RenderedCompactJsonFormatter());
            }
        });

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            telemetry.UseOtlpExporter();
        }

        builder.Services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                var http = context.HttpContext;
                context.ProblemDetails.Extensions["traceId"] = System.Diagnostics.Activity.Current?.TraceId.ToString()
                    ?? http.TraceIdentifier;
                if (http.Items[CorrelationIdMiddleware.ItemKey] is string correlationId)
                {
                    context.ProblemDetails.Extensions["correlationId"] = correlationId;
                }
            });
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.AddHealthChecks();

        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();

        return builder;
    }

    /// <summary>
    /// Adds the standard middleware in the correct order (correlation id, exception handling, request logging,
    /// status-code ProblemDetails, authentication, authorization) and maps <see cref="LivePath"/> and
    /// <see cref="ReadyPath"/>. Call it before mapping the service's own endpoints.
    /// </summary>
    public static WebApplication UseLeiMaDefaults(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseSerilogRequestLogging();
        app.UseStatusCodePages();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks(LivePath, new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks(ReadyPath, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
        }).AllowAnonymous();

        return app;
    }
}
