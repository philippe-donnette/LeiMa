using LeiMa.Common.Testing;
using LeiMa.Runner.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace LeiMa.Runner.IntegrationTests;

/// <summary>The Runner infrastructure on one new, empty test database, with a clock the test controls.</summary>
internal sealed class RunnerTestHost : IAsyncDisposable
{
    /// <summary>The time the fake clock starts at.</summary>
    public static readonly DateTimeOffset StartTime = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private RunnerTestHost(ServiceProvider services, FakeTimeProvider clock, string connectionString)
    {
        Services = services;
        Clock = clock;
        ConnectionString = connectionString;
    }

    /// <summary>Gets the services, registered like the Api and the migrator do, with a fake clock.</summary>
    public ServiceProvider Services { get; }

    /// <summary>Gets the clock the audit timestamps are read from.</summary>
    public FakeTimeProvider Clock { get; }

    /// <summary>Gets the connection string of the test database.</summary>
    public string ConnectionString { get; }

    /// <summary>Creates a new database in the shared container and, unless told not to, migrates it.</summary>
    /// <param name="postgres">The shared PostgreSQL fixture.</param>
    /// <param name="migrate">Whether to apply the migrations before returning.</param>
    /// <returns>The host.</returns>
    public static async Task<RunnerTestHost> CreateAsync(PostgreSqlFixture postgres, bool migrate = true)
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        var clock = new FakeTimeProvider(StartTime);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationManager { ["ConnectionStrings:runner"] = connectionString });
        services.AddSingleton<TimeProvider>(clock);
        services.AddRunnerInfrastructure();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        var host = new RunnerTestHost(provider, clock, connectionString);
        if (migrate)
        {
            await host.MigrateAsync();
        }

        return host;
    }

    /// <summary>Applies the pending migrations with the same method the migrator uses.</summary>
    /// <returns>A task that completes when the database is up to date.</returns>
    public Task MigrateAsync() => RunnerDatabase.MigrateAsync(Services, NullLogger.Instance, CancellationToken.None);

    /// <summary>Starts a new scope, like one request: its own database context and repository.</summary>
    /// <returns>The scope; dispose it when done.</returns>
    public AsyncServiceScope NewScope() => Services.CreateAsyncScope();

    /// <summary>Runs one SQL command that returns no rows, straight on the database.</summary>
    /// <param name="sql">The command text.</param>
    /// <param name="parameters">Named parameters, if any.</param>
    /// <returns>A task that completes when the command has run.</returns>
    public async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Runs a SQL query straight on the database and reads the first column of every row as text.</summary>
    /// <param name="sql">The query text.</param>
    /// <returns>The values of the first column.</returns>
    public async Task<List<string>> QueryAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetValue(0)?.ToString() ?? string.Empty);
        }

        return values;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Services.DisposeAsync();
}
