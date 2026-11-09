namespace LeiMa.Common;

/// <summary>Names used to carry a correlation id across HTTP calls, logs and traces.</summary>
public static class CorrelationConstants
{
    /// <summary>HTTP header carrying the correlation id.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>Log property and trace tag name for the correlation id.</summary>
    public const string PropertyName = "CorrelationId";

    /// <summary>Maximum accepted length of an incoming correlation id; longer values are replaced.</summary>
    public const int MaxLength = 64;
}
