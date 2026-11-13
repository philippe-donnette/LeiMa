using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LeiMa.Common.EntityFrameworkCore.Tests;

/// <summary>Model-level checks that need no database.</summary>
public class ConventionTests
{
    private static IEntityType WidgetType()
    {
        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .Options;
        using var context = new SampleDbContext(options);
        return context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Widget))!;
    }

    [Theory]
    [InlineData(nameof(Widget.ScheduledAt))]
    [InlineData(nameof(Widget.LastRunAt))]
    [InlineData(ModelConventions.CreatedAt)]
    [InlineData(ModelConventions.UpdatedAt)]
    public void TimestampColumns_AreTimestamptz(string property)
    {
        Assert.Equal("timestamp with time zone", WidgetType().FindProperty(property)!.GetColumnType());
    }

    [Fact]
    public void Xmin_IsAConcurrencyToken_OnTheSystemColumn()
    {
        var xmin = WidgetType().FindProperty(ModelConventions.XminColumn)!;

        Assert.True(xmin.IsConcurrencyToken);
        Assert.True(xmin.IsShadowProperty());
        Assert.Equal("xmin", xmin.GetColumnName());
        Assert.Equal("xid", xmin.GetColumnType());
    }

    [Fact]
    public void AuditProperties_AreShadowProperties()
    {
        Assert.True(WidgetType().FindProperty(ModelConventions.CreatedAt)!.IsShadowProperty());
        Assert.True(WidgetType().FindProperty(ModelConventions.UpdatedAt)!.IsShadowProperty());
    }
}
