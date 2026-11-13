using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LeiMa.Common.EntityFrameworkCore;

/// <summary>
/// Sets <c>CreatedAt</c> and <c>UpdatedAt</c> on entities that have those properties (see
/// <see cref="ModelConventions.HasAuditTimestamps{T}"/>) when changes are saved, using the injected
/// <see cref="TimeProvider"/> so tests can control time.
/// </summary>
/// <param name="timeProvider">The clock.</param>
public sealed class AuditTimestampInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        foreach (var entry in context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    Set(entry, ModelConventions.CreatedAt, now);
                    Set(entry, ModelConventions.UpdatedAt, now);
                    break;
                case EntityState.Modified:
                    Set(entry, ModelConventions.UpdatedAt, now);
                    if (entry.Metadata.FindProperty(ModelConventions.CreatedAt) is not null)
                    {
                        entry.Property(ModelConventions.CreatedAt).IsModified = false;
                    }

                    break;
            }
        }
    }

    private static void Set(EntityEntry entry, string name, DateTimeOffset value)
    {
        var property = entry.Metadata.FindProperty(name);
        if (property is null)
        {
            return;
        }

        var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        if (clrType == typeof(DateTimeOffset))
        {
            entry.Property(name).CurrentValue = value;
        }
        else if (clrType == typeof(DateTime))
        {
            entry.Property(name).CurrentValue = value.UtcDateTime;
        }
    }
}
