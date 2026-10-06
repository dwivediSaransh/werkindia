using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E04;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E04;

public sealed class CandidatePaywallEventConfiguration : IEntityTypeConfiguration<CandidatePaywallEvent>
{
    public void Configure(EntityTypeBuilder<CandidatePaywallEvent> builder)
    {
        builder.ToTable("CandidatePaywallEvents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.OrganisationId)
            .IsRequired();

        builder.Property(x => x.CandidateProfileId)
            .IsRequired();

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.JobContext)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.FreeLimit)
            .IsRequired();

        builder.Property(x => x.FreeUsed)
            .IsRequired();

        builder.Property(x => x.FreeRemaining)
            .IsRequired();

        builder.Property(x => x.AccessSource)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.OrganisationId, x.CandidateProfileId, x.CreatedAt })
            .HasDatabaseName("IX_CandidatePaywallEvents_OrgId_CandidateProfileId_CreatedAt");

        builder.HasIndex(x => x.CorrelationId)
            .IsUnique()
            .HasDatabaseName("IX_CandidatePaywallEvents_CorrelationId");
    }
}