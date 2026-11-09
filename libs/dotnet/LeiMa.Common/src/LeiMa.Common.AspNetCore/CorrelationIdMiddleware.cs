using System.Diagnostics;
using Serilog.Context;

namespace LeiMa.Common.AspNetCore;

/// <summary>
/// Reads the correlation id from the request header (or creates one), exposes it on the response, in
/// <see cref="HttpContext.Items"/>, in log events and on the current trace span.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>Key of the correlation id in <see cref="HttpContext.Items"/>.</summary>
    public const string ItemKey = CorrelationConstants.PropertyName;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(static state =>
        {
            var (ctx, id) = ((HttpContext, string))state;
            ctx.Response.Headers[CorrelationConstants.HeaderName] = id;
            return Task.CompletedTask;
        }, (context, correlationId));

        Activity.Current?.SetTag(CorrelationConstants.PropertyName, correlationId);

        using (LogContext.PushProperty(CorrelationConstants.PropertyName, correlationId))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var incoming = context.Request.Headers[CorrelationConstants.HeaderName].ToString();
        return IsValid(incoming)
            ? incoming
            : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    }

    private static bool IsValid(string value) =>
        value.Length is > 0 and <= CorrelationConstants.MaxLength
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
