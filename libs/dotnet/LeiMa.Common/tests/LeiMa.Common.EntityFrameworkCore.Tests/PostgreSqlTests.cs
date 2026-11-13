using LeiMa.Common.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace LeiMa.Common.EntityFrameworkCore.Tests;

/// <summary>Integration tests against a real PostgreSQL container (needs Docker).</summary>
public class PostgreSqlTests(PostgreSqlFixture postgres) : IClassFixture<PostgreSqlFixture>
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero));

    private async Task<ServiceProvider> CreateServicesAsync()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(_clock);
        services.AddLeiMaEntityFrameworkCore();
        services.AddDbContext<SampleDbContext>((sp, options) => options.UseLeiMaNpgsql(sp, connectionString));

        var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SampleDbContext>().Database.EnsureCreatedAsync();
        return provider;
    }

    [RequiresDockerFact]
    public async Task DateTimes_RoundTripAsUtc_EvenWhenWrittenAsLocalOrUnspecified()
    {
        await using var provider = await CreateServicesAsync();
        var local = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Local);
        var unspecified = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Unspecified);
        var localWidget = new Widget { Name = "local", ScheduledAt = local };
        var unspecifiedWidget = new Widget { Name = "unspecified", ScheduledAt = unspecified };

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            db.AddRange(localWidget, unspecifiedWidget);
            await db.SaveChangesAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            var loadedLocal = await db.Widgets.SingleAsync(w => w.Id == localWidget.Id);
            var loadedUnspecified = await db.Widgets.SingleAsync(w => w.Id == unspecifiedWidget.Id);

            Assert.Equal(DateTimeKind.Utc, loadedLocal.ScheduledAt.Kind);
            Assert.Equal(local.ToUniversalTime(), loadedLocal.ScheduledAt);
            Assert.Equal(DateTimeKind.Utc, loadedUnspecified.ScheduledAt.Kind);
            Assert.Equal(new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc), loadedUnspecified.ScheduledAt);
        }
    }

    [RequiresDockerFact]
    public async Task DateTimeOffsets_WithAnOffset_AreStoredAsUtc()
    {
        await using var provider = await CreateServicesAsync();
        var widget = new Widget { Name = "offset", LastRunAt = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.FromHours(2)) };

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            db.Add(widget);
            await db.SaveChangesAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            var loaded = await db.Widgets.SingleAsync(w => w.Id == widget.Id);

            Assert.Equal(widget.LastRunAt, loaded.LastRunAt);
            Assert.Equal(TimeSpan.Zero, loaded.LastRunAt!.Value.Offset);
        }
    }

    [RequiresDockerFact]
    public async Task AuditTimestamps_AreSetOnInsert_AndUpdatedOnChange()
    {
        await using var provider = await CreateServicesAsync();
        var created = _clock.GetUtcNow();
        var widget = new Widget { Name = "audited" };

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            db.Add(widget);
            await db.SaveChangesAsync();
        }

        _clock.Advance(TimeSpan.FromHours(1));

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            var loaded = await db.Widgets.SingleAsync(w => w.Id == widget.Id);
            Assert.Equal(created, db.Entry(loaded).Property<DateTimeOffset>(ModelConventions.CreatedAt).CurrentValue);
            Assert.Equal(created, db.Entry(loaded).Property<DateTimeOffset>(ModelConventions.UpdatedAt).CurrentValue);

            loaded.Name = "renamed";
            await db.SaveChangesAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            var loaded = await db.Widgets.SingleAsync(w => w.Id == widget.Id);

            Assert.Equal(created, db.Entry(loaded).Property<DateTimeOffset>(ModelConventions.CreatedAt).CurrentValue);
            Assert.Equal(created.AddHours(1), db.Entry(loaded).Property<DateTimeOffset>(ModelConventions.UpdatedAt).CurrentValue);
        }
    }

    [RequiresDockerFact]
    public async Task ConcurrentUpdates_RaiseAConcurrencyException()
    {
        await using var provider = await CreateServicesAsync();
        var widget = new Widget { Name = "contended" };
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            db.Add(widget);
            await db.SaveChangesAsync();
        }

        await using var scopeA = provider.CreateAsyncScope();
        await using var scopeB = provider.CreateAsyncScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<SampleDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<SampleDbContext>();
        var a = await dbA.Widgets.SingleAsync(w => w.Id == widget.Id);
        var b = await dbB.Widgets.SingleAsync(w => w.Id == widget.Id);

        a.Name = "first";
        await dbA.SaveChangesAsync();
        b.Name = "second";

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());
    }
}
