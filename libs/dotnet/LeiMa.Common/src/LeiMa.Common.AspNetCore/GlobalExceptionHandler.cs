using Microsoft.AspNetCore.Diagnostics;

namespace LeiMa.Common.AspNetCore;

/// <summary>
/// Turns unhandled exceptions into an RFC 9457 ProblemDetails response with status 500. The exception
/// details are logged but never returned to the caller.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int status;
        string title;

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request was cancelled by the client.");
            status = ClientClosedRequest;
            title = "The request was cancelled.";
        }
        else
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
            status = StatusCodes.Status500InternalServerError;
            title = "An unexpected error occurred.";
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title },
        });
    }
}
