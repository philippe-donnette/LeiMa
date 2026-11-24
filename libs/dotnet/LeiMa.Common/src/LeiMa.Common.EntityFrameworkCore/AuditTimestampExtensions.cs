using Microsoft.EntityFrameworkCore;

namespace LeiMa.Common.EntityFrameworkCore;

/// <summary>
/// Reads the audit timestamps added by <see cref="ModelConventions.HasAuditTimestamps{T}"/>. They are often shadow
/// properties, so the entity class has nothing to read; use these methods in tests and diagnostics.
/// </summary>
public static class AuditTimestampExtensions
{
    /// <summary>Gets the creation time the context holds for the entity.</summary>
    /// <param name="context">The context that tracks the entity.</param>
    /// <param name="entity">The tracked entity.</param>
    /// <returns>The creation time, in UTC.</returns>
    /// <exception cref="InvalidOperationException">The entity type is not auditable.</exception>
    public static DateTimeOffset CreatedAt(this DbContext context, object entity) =>
        Read(context, entity, ModelConventions.CreatedAt);

    /// <summary>Gets the time of the last update the context holds for the entity.</summary>
    /// <param name="context">The context that tracks the entity.</param>
    /// <param name="entity">The tracked entity.</param>
    /// <returns>The time of the last update, in UTC.</returns>
    /// <exception cref="InvalidOperationException">The entity type is not auditable.</exception>
    public static DateTimeOffset UpdatedAt(this DbContext context, object entity) =>
        Read(context, entity, ModelConventions.UpdatedAt);

    private static DateTimeOffset Read(DbContext context, object entity, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entity);

        var entry = context.Entry(entity);
        if (entry.Metadata.FindProperty(propertyName) is null)
        {
            throw new InvalidOperationException(
                $"{entry.Metadata.DisplayName()} has no {propertyName} property; call HasAuditTimestamps() in its configuration.");
        }

        return (DateTimeOffset)entry.Property(propertyName).CurrentValue!;
    }
}
