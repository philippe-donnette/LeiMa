namespace LeiMa.Common;

/// <summary>Helpers for <see cref="TimeProvider"/>. Always inject a <see cref="TimeProvider"/> instead of using <c>DateTime.UtcNow</c>.</summary>
public static class TimeProviderExtensions
{
    /// <summary>The current UTC time as a <see cref="DateTime"/> with <see cref="DateTimeKind.Utc"/>.</summary>
    public static DateTime GetUtcDateTime(this TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return timeProvider.GetUtcNow().UtcDateTime;
    }

    /// <summary>True when <paramref name="deadline"/> is at or before the current time.</summary>
    public static bool HasPassed(this TimeProvider timeProvider, DateTimeOffset deadline)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return timeProvider.GetUtcNow() >= deadline;
    }
}
