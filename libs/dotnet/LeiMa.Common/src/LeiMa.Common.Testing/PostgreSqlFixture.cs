using Npgsql;
using Testcontainers.PostgreSql;

namespace LeiMa.Common.Testing;

/// <summary>
/// xUnit fixture that starts one PostgreSQL container for a test class (or collection) and hands out isolated databases.
/// Use with <c>IClassFixture&lt;PostgreSqlFixture&gt;</c> and mark tests with <see cref="RequiresDockerFactAttribute"/>.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer? _container;

    /// <summary>Creates the fixture; the container starts in <see cref="InitializeAsync"/>.</summary>
    public PostgreSqlFixture()
    {
        if (DockerAvailability.IsAvailable)
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        }
    }

    /// <summary>Starts the container (does nothing when Docker is unavailable and tests are skipped).</summary>
    public async Task InitializeAsync()
    {
        if (_container is not null)
        {
            await _container.StartAsync();
        }
    }

    /// <summary>Stops and removes the container.</summary>
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>Creates a new empty database and returns its connection string, so tests do not share data.</summary>
    public async Task<string> CreateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var container = _container
            ?? throw new InvalidOperationException("Docker is not available. Mark the test with [RequiresDockerFact].");

        var name = $"test_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(container.GetConnectionString()))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = name, Pooling = false }
            .ConnectionString;
    }
}
