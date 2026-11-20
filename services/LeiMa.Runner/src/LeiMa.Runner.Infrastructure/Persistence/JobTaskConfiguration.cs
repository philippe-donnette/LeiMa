using LeiMa.Common.EntityFrameworkCore;
using LeiMa.Runner.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeiMa.Runner.Infrastructure.Persistence;

/// <summary>Maps <see cref="JobTask"/> to the <c>job_tasks</c> table; the domain class carries no ORM attributes.</summary>
internal sealed class JobTaskConfiguration : IEntityTypeConfiguration<JobTask>
{
    /// <summary>The name of the shadow property holding the id of the job the task belongs to.</summary>
    internal const string JobIdColumn = "JobId";

    /// <summary>The PostgreSQL column type of the JSON parameters and output, so they are stored as queryable binary JSON.</summary>
    private const string JsonColumnType = "jsonb";

    /// <summary>The longest stored status name; the status is kept as text so the table stays readable.</summary>
    private const int StatusMaxLength = 20;

    /// <summary>The longest stored execution id returned by a provider.</summary>
    private const int ExecutionIdMaxLength = 200;

    /// <summary>The longest stored error code.</summary>
    private const int ErrorCodeMaxLength = 100;

    /// <summary>Configures the table, columns, the unique position of a task in its job, concurrency token and audit timestamps.</summary>
    /// <param name="builder">The builder for the task entity.</param>
    public void Configure(EntityTypeBuilder<JobTask> builder)
    {
        builder.ToTable("job_tasks");

        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id).ValueGeneratedNever();
        builder.Property<Guid>(JobIdColumn);
        builder.Property(task => task.Position);
        builder.Property(task => task.TaskType).IsRequired().HasMaxLength(Job.TaskTypeMaxLength);
        builder.Property(task => task.Parameters).IsRequired().HasColumnType(JsonColumnType);
        builder.Property(task => task.Timeout);
        builder.Property(task => task.Status).HasConversion<string>().HasMaxLength(StatusMaxLength);
        builder.Property(task => task.Output).HasColumnType(JsonColumnType);
        builder.Property(task => task.Run);
        builder.Property(task => task.ExecutionId).HasMaxLength(ExecutionIdMaxLength);
        builder.Property(task => task.ErrorCode).HasMaxLength(ErrorCodeMaxLength);
        builder.Property(task => task.ErrorMessage);

        builder.HasIndex(JobIdColumn, nameof(JobTask.Position)).IsUnique();

        builder.UseXminConcurrencyToken();
        builder.HasAuditTimestamps();
    }
}
