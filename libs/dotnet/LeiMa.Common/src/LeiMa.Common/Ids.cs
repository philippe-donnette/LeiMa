namespace LeiMa.Common;

/// <summary>Identifier generation. Ids are time-ordered GUIDs (version 7), which index well in PostgreSQL.</summary>
public static class Ids
{
    /// <summary>Creates a new time-ordered id using <paramref name="timeProvider"/> for the timestamp part.</summary>
    public static Guid NewId(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return Guid.CreateVersion7(timeProvider.GetUtcNow());
    }

    /// <summary>Creates a new time-ordered id using the system clock.</summary>
    public static Guid NewId() => NewId(TimeProvider.System);
}
