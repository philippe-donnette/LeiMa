using LeiMa.Common.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeiMa.Common.EntityFrameworkCore.Tests;

/// <summary>Tests of <see cref="PostgresTestHost"/> with a sample service registration (needs Docker).</summary>
public class PostgresTestHostTests(PostgreSqlFixture postgres) : IClassFixture<PostgreSqlFixture>
{
    private static void Register(IServiceCollection services) =>
        services.AddLeiMaEntityFrameworkCore()
            .AddDbContext<SampleDbContext>((sp, options) =>
                options.UseLeiMaNpgsql(sp, sp.GetRequiredService<IConfiguration>().GetConnectionString("sample")!));

    private static Task MigrateAsync(IServiceProvider services) =>
        services.CreateAsyncScope().ServiceProvider.GetRequiredService<SampleDbContext>().Database.EnsureCreatedAsync();

    [RequiresDockerFact]
    public async Task Create_WithMigration_ServesAMigratedDatabaseAndTheFakeClock()
    {
        await using var host = await PostgresTestHost.CreateAsync(postgres, "sample", Register, MigrateAsync);
        var widget = new Widget { Name = "hosted" };

        await using (var scope = host.NewScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            db.Add(widget);
            await db.SaveChangesAsync();
        }

        await using var check = host.NewScope();
        var checkDb = check.ServiceProvider.GetRequiredService<SampleDbContext>();
        var loaded = await checkDb.Widgets.SingleAsync(w => w.Id == widget.Id);
        Assert.Equal(PostgresTestHost.DefaultStartTime, checkDb.CreatedAt(loaded));
        Assert.Equal(PostgresTestHost.DefaultStartTime, host.Clock.GetUtcNow());
    }

    [RequiresDockerFact]
    public async Task Create_WithoutMigratingNow_LeavesTheDatabaseEmptyUntilTold()
    {
        await using var host = await PostgresTestHost.CreateAsync(postgres, "sample", Register, MigrateAsync, migrateNow: false);
        Assert.Empty(await host.QueryAsync("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'"));

        await host.MigrateAsync();

        Assert.Contains("Widgets", await host.QueryAsync("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'"));
    }

    [RequiresDockerFact]
    public async Task ExecuteAndQuery_RunSqlOnTheTestDatabase()
    {
        await using var host = await PostgresTestHost.CreateAsync(postgres, "sample", Register);
        await host.ExecuteAsync("CREATE TABLE t (n int)");
        await host.ExecuteAsync("INSERT INTO t VALUES (@a), (@b)", ("a", 1), ("b", 2));

        Assert.Equal(["1", "2"], await host.QueryAsync("SELECT n FROM t ORDER BY n"));
    }

    [RequiresDockerFact]
    public async Task Migrate_WithoutADelegate_Throws()
    {
        await using var host = await PostgresTestHost.CreateAsync(postgres, "sample", Register);

        await Assert.ThrowsAsync<InvalidOperationException>(() => host.MigrateAsync());
    }

    [RequiresDockerFact]
    public async Task Create_WithAStartTime_StartsTheClockThere()
    {
        var start = new DateTimeOffset(2030, 5, 6, 7, 8, 9, TimeSpan.Zero);

        await using var host = await PostgresTestHost.CreateAsync(postgres, "sample", Register, startTime: start);

        Assert.Equal(start, host.Clock.GetUtcNow());
    }
}
