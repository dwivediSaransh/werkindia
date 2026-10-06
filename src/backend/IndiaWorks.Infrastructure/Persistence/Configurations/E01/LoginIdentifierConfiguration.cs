using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E01;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E01;

public sealed class LoginIdentifierConfiguration : IEntityTypeConfiguration<LoginIdentifier>
{
    public void Configure(EntityTypeBuilder<LoginIdentifier> builder)
    {
        builder.ToTable("LoginIdentifiers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();
        builder.Property(x => x.AccountId)
            .IsRequired();
        builder.Property(x => x.Provider)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(x => x.Identifier)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(x => x.CreatedOn)
            .IsRequired();
        builder.Property(x => x.VerifiedOn)
            .IsRequired(false);
        builder.HasIndex(x => new { x.AccountId, x.Provider })
            .IsUnique()
            .HasDatabaseName("UX_LoginIdentifiers_AccountId_Provider");
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
