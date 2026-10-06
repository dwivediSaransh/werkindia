using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E02;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E02;

public sealed class OrganisationConfiguration : IEntityTypeConfiguration<Organisation>
{
    public void Configure(EntityTypeBuilder<Organisation> builder)
    {
        builder.ToTable("Organisations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.LegalName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.TradeName)
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(x => x.OrganisationType)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.Sector)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(x => x.EmployeeBand)
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(x => x.Nic2025Code)
            .HasMaxLength(10)
            .IsRequired(false);

        builder.Property(x => x.VerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CommercialIdentityFingerprint)
            .IsRequired(false)
            .HasMaxLength(32);

        builder.HasMany(x => x.Identifiers)
            .WithOne()
            .HasForeignKey(x => x.OrganisationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Identifiers)
            .AutoInclude(false);

        builder.HasIndex(x => x.LegalName)
            .HasDatabaseName("IX_Organisations_LegalName");

        builder.HasIndex(x => x.VerificationStatus)
            .HasDatabaseName("IX_Organisations_VerificationStatus");
    }
}
