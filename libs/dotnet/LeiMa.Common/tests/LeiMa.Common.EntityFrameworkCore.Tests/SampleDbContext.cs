using Microsoft.EntityFrameworkCore;

namespace LeiMa.Common.EntityFrameworkCore.Tests;

public sealed class Widget
{
    public Guid Id { get; set; } = Ids.NewId();

    public string Name { get; set; } = string.Empty;

    public DateTime ScheduledAt { get; set; }

    public DateTimeOffset? LastRunAt { get; set; }
}

public sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
{
    public DbSet<Widget> Widgets => Set<Widget>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.UseLeiMaConventions();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Widget>(widget =>
        {
            widget.HasKey(w => w.Id);
            widget.Property(w => w.Name).HasMaxLength(100);
            widget.UseXminConcurrencyToken();
            widget.HasAuditTimestamps();
        });
}
