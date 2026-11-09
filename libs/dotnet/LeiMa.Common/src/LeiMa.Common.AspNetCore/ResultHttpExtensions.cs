namespace LeiMa.Common.AspNetCore;

/// <summary>Maps <see cref="Result"/> and <see cref="Error"/> to HTTP responses (ProblemDetails for failures).</summary>
public static class ResultHttpExtensions
{
    /// <summary>The HTTP status code used for an <see cref="ErrorType"/>.</summary>
    public static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError,
    };

    /// <summary>Converts an error into a ProblemDetails response. The error code is returned in the <c>code</c> extension.</summary>
    public static IResult ToProblemResult(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return Results.Problem(
            statusCode: StatusCodeFor(error.Type),
            detail: error.Message,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    /// <summary>204 No Content on success, ProblemDetails on failure.</summary>
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : result.Error.ToProblemResult();

    /// <summary>200 OK with the value on success (or the result of <paramref name="onSuccess"/>), ProblemDetails on failure.</summary>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null) =>
        result.IsSuccess
            ? (onSuccess is null ? Results.Ok(result.Value) : onSuccess(result.Value))
            : result.Error.ToProblemResult();
}
