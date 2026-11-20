using LeiMa.Common.EntityFrameworkCore;
using LeiMa.Runner.Domain;
using Microsoft.EntityFrameworkCore;

namespace LeiMa.Runner.Infrastructure.Persistence;

/// <summary>The EF Core model of the Runner database: the <see cref="Job"/> aggregate and its tasks.</summary>
public sealed class RunnerDbContext : DbContext
{
    /// <summary>Initializes a new instance of the <see cref="RunnerDbContext"/> class.</summary>
    /// <param name="options">The options, including the PostgreSQL connection, from the composition root.</param>
    public RunnerDbContext(DbContextOptions<RunnerDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets the jobs. Tasks are reached through their job, so they have no set of their own.</summary>
    public DbSet<Job> Jobs => Set<Job>();

    /// <summary>Applies the LeiMa conventions: every date and time is stored as UTC <c>timestamptz</c>.</summary>
    /// <param name="configurationBuilder">The builder for the conventions of the model.</param>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.UseLeiMaConventions();

    /// <summary>Applies every entity configuration of this assembly.</summary>
    /// <param name="modelBuilder">The builder for the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RunnerDbContext).Assembly);
}
