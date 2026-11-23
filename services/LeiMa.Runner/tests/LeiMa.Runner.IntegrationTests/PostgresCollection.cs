using LeiMa.Common.Testing;

namespace LeiMa.Runner.IntegrationTests;

/// <summary>Shares one PostgreSQL container between all integration test classes; every test still gets its own database.</summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgreSqlFixture>
{
    /// <summary>The name of the collection.</summary>
    public const string Name = "postgres";
}
