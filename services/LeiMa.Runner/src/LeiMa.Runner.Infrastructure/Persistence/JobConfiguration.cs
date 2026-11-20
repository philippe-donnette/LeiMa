using LeiMa.Common.EntityFrameworkCore;
using LeiMa.Runner.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeiMa.Runner.Infrastructure.Persistence;

/// <summary>Maps <see cref="Job"/> to the <c>jobs</c> table; the domain class carries no ORM attributes.</summary>
internal sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    /// <summary>The longest stored status name; the status is kept as text so the table stays readable.</summary>
    private const int StatusMaxLength = 20;

    /// <summary>Configures the table, columns, concurrency token, audit timestamps and the link to the tasks.</summary>
    /// <param name="builder">The builder for the job entity.</param>
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs");

        builder.HasKey(job => job.Id);
        builder.Property(job => job.Id).ValueGeneratedNever();
        builder.Property(job => job.Name).IsRequired().HasMaxLength(Job.NameMaxLength);
        builder.Property(job => job.Description).HasMaxLength(Job.DescriptionMaxLength);
        builder.Property(job => job.Status).HasConversion<string>().HasMaxLength(StatusMaxLength);

        builder.UseXminConcurrencyToken();
        builder.HasAuditTimestamps();

        builder.HasMany(job => job.Tasks)
            .WithOne()
            .HasForeignKey(JobTaskConfiguration.JobIdColumn)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(job => job.Tasks).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
