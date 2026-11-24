using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace LeiMa.Common.Testing;

/// <summary>
/// A service's own registration on one new, empty test database, with a clock the test controls. Create it from a
/// <see cref="PostgreSqlFixture"/>, passing the same registration method the service's host uses.
/// </summary>
public sealed class PostgresTestHost : IAsyncDisposable
{
    /// <summary>The time the fake clock starts at unless the test chooses another.</summary>
    public static readonly DateTimeOffset DefaultStartTime = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly Func<IServiceProvider, Task>? _migrate;

    private PostgresTestHost(ServiceProvider services, FakeTimeProvider clock, string connectionString, Func<IServiceProvider, Task>? migrate)
    {
        Services = services;
        Clock = clock;
        ConnectionString = connectionString;
        _migrate = migrate;
    }

    /// <summary>Gets the services, registered by the delegate given to <see cref="CreateAsync"/>, with a fake clock.</summary>
    public ServiceProvider Services { get; }

    /// <summary>Gets the clock registered as the <see cref="TimeProvider"/>.</summary>
    public FakeTimeProvider Clock { get; }

    /// <summary>Gets the connection string of the test database.</summary>
    public string ConnectionString { get; }

    /// <summary>Creates a new database in the shared container and, when a migration delegate is given, migrates it.</summary>
    /// <param name="postgres">The shared PostgreSQL fixture.</param>
    /// <param name="connectionStringName">The name the service reads its connection string under (<c>ConnectionStrings:&lt;name&gt;</c>).</param>
    /// <param name="register">Registers the service's own services, for example its <c>AddXxxInfrastructure()</c> method.</param>
    /// <param name="migrate">Brings the database up to date, with the same code the service uses; <see langword="null"/> if there is none.</param>
    /// <param name="migrateNow">Whether to run <paramref name="migrate"/> before returning; use <see langword="false"/> to test the migration itself.</param>
    /// <param name="startTime">The time the clock starts at; <see cref="DefaultStartTime"/> if omitted.</param>
    /// <returns>The host; dispose it when done.</returns>
    public static async Task<PostgresTestHost> CreateAsync(
        PostgreSqlFixture postgres,
        string connectionStringName,
        Action<IServiceCollection> register,
        Func<IServiceProvider, Task>? migrate = null,
        bool migrateNow = true,
        DateTimeOffset? startTime = null)
    {
        ArgumentNullException.ThrowIfNull(postgres);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);
        ArgumentNullException.ThrowIfNull(register);

        var connectionString = await postgres.CreateDatabaseAsync();
        var clock = new FakeTimeProvider(startTime ?? DefaultStartTime);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationManager { [$"ConnectionStrings:{connectionStringName}"] = connectionString });
        services.AddSingleton<TimeProvider>(clock);
        register(services);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        var host = new PostgresTestHost(provider, clock, connectionString, migrate);
        if (migrateNow && migrate is not null)
        {
            await host.MigrateAsync();
        }

        return host;
    }

    /// <summary>Applies the pending migrations with the delegate given to <see cref="CreateAsync"/>.</summary>
    /// <returns>A task that completes when the database is up to date.</returns>
    /// <exception cref="InvalidOperationException">No migration delegate was given.</exception>
    public Task MigrateAsync() =>
        (_migrate ?? throw new InvalidOperationException("No migration delegate was given to CreateAsync."))(Services);

    /// <summary>Starts a new scope, like one request: its own database context and repositories.</summary>
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
