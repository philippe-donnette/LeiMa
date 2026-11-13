using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LeiMa.Common.EntityFrameworkCore;

/// <summary>Model conventions shared by all LeiMa services (PostgreSQL).</summary>
public static class ModelConventions
{
    /// <summary>Name of the PostgreSQL system column used as optimistic concurrency token.</summary>
    public const string XminColumn = "xmin";

    /// <summary>Name of the creation timestamp property added by <see cref="HasAuditTimestamps{T}"/>.</summary>
    public const string CreatedAt = "CreatedAt";

    /// <summary>Name of the last-update timestamp property added by <see cref="HasAuditTimestamps{T}"/>.</summary>
    public const string UpdatedAt = "UpdatedAt";

    /// <summary>
    /// Stores every <see cref="DateTime"/> and <see cref="DateTimeOffset"/> as <c>timestamp with time zone</c> in UTC.
    /// Call it from <see cref="DbContext.ConfigureConventions"/>. Reading always yields <see cref="DateTimeKind.Utc"/>;
    /// writing an unspecified-kind <see cref="DateTime"/> is treated as UTC.
    /// </summary>
    public static ModelConfigurationBuilder UseLeiMaConventions(this ModelConfigurationBuilder configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        configuration.Properties<DateTime>()
            .HaveColumnType("timestamp with time zone")
            .HaveConversion<UtcDateTimeConverter>();
        configuration.Properties<DateTimeOffset>()
            .HaveColumnType("timestamp with time zone")
            .HaveConversion<UtcDateTimeOffsetConverter>();
        return configuration;
    }

    /// <summary>
    /// Adds the PostgreSQL <c>xmin</c> system column as a shadow concurrency token, so concurrent updates of the same
    /// row raise <see cref="DbUpdateConcurrencyException"/> instead of silently overwriting.
    /// </summary>
    public static EntityTypeBuilder<T> UseXminConcurrencyToken<T>(this EntityTypeBuilder<T> entity)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.Property<uint>(XminColumn)
            .HasColumnName(XminColumn)
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        return entity;
    }

    /// <summary>
    /// Makes the entity auditable: ensures <c>CreatedAt</c> and <c>UpdatedAt</c> (<see cref="DateTimeOffset"/>) exist,
    /// as shadow properties when the entity class does not declare them. <see cref="AuditTimestampInterceptor"/> fills them.
    /// </summary>
    public static EntityTypeBuilder<T> HasAuditTimestamps<T>(this EntityTypeBuilder<T> entity)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.Property<DateTimeOffset>(CreatedAt);
        entity.Property<DateTimeOffset>(UpdatedAt);
        return entity;
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => ToUtc(v),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
        private static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    private sealed class UtcDateTimeOffsetConverter() : ValueConverter<DateTimeOffset, DateTimeOffset>(
        v => v.ToUniversalTime(),
        v => v.ToUniversalTime());
}
