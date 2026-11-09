namespace LeiMa.Common;

/// <summary>Category of an <see cref="Error"/>; maps to an HTTP status in the ASP.NET Core package.</summary>
public enum ErrorType
{
    /// <summary>An unexpected failure.</summary>
    Failure = 0,

    /// <summary>The input is invalid.</summary>
    Validation,

    /// <summary>The requested resource does not exist.</summary>
    NotFound,

    /// <summary>The operation conflicts with the current state (for example a concurrency clash).</summary>
    Conflict,

    /// <summary>The caller is not authenticated.</summary>
    Unauthorized,

    /// <summary>The caller is authenticated but not allowed.</summary>
    Forbidden,
}

/// <summary>A described failure: a stable machine-readable code, a human-readable message and a category.</summary>
/// <param name="Code">Stable code such as <c>provider.not_found</c>; safe for clients to branch on.</param>
/// <param name="Message">Human-readable description; must not contain secrets.</param>
/// <param name="Type">The failure category.</param>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>Creates a <see cref="ErrorType.Failure"/> error.</summary>
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);

    /// <summary>Creates a <see cref="ErrorType.Validation"/> error.</summary>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>Creates a <see cref="ErrorType.NotFound"/> error.</summary>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>Creates a <see cref="ErrorType.Conflict"/> error.</summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>Creates an <see cref="ErrorType.Unauthorized"/> error.</summary>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>Creates a <see cref="ErrorType.Forbidden"/> error.</summary>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
}
