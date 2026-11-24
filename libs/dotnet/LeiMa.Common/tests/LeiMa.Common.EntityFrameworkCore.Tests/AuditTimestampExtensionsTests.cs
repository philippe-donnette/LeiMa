using Microsoft.EntityFrameworkCore;

namespace LeiMa.Common.EntityFrameworkCore.Tests;

/// <summary>Tests of the audit timestamp readers; they need no database, only the model.</summary>
public class AuditTimestampExtensionsTests
{
    private static SampleDbContext NewContext() =>
        new(new DbContextOptionsBuilder<SampleDbContext>().UseNpgsql("Host=localhost;Database=never_connected").Options);

    [Fact]
    public void CreatedAtAndUpdatedAt_AuditableEntity_ReturnTheShadowValues()
    {
        using var db = NewContext();
        var widget = new Widget { Name = "w" };
        var entry = db.Attach(widget);
        var created = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);
        entry.Property(ModelConventions.CreatedAt).CurrentValue = created;
        entry.Property(ModelConventions.UpdatedAt).CurrentValue = created.AddHours(2);

        Assert.Equal(created, db.CreatedAt(widget));
        Assert.Equal(created.AddHours(2), db.UpdatedAt(widget));
    }

    [Fact]
    public void CreatedAt_EntityWithoutAuditTimestamps_SaysWhatToDo()
    {
        using var db = NewContext();
        var plain = new Plain();
        db.Attach(plain);

        var error = Assert.Throws<InvalidOperationException>(() => db.CreatedAt(plain));

        Assert.Contains("HasAuditTimestamps", error.Message);
        Assert.Contains("Plain", error.Message);
    }

    [Fact]
    public void UpdatedAt_NullArguments_AreRefused()
    {
        using var db = NewContext();

        Assert.Throws<ArgumentNullException>(() => _ = db.UpdatedAt(null!));
        Assert.Throws<ArgumentNullException>(() => _ = AuditTimestampExtensions.UpdatedAt(null!, new Widget()));
    }
}
