using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E01;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E01;

public sealed class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.ToTable("OtpChallenges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();
        builder.Property(x => x.AccountId)
            .IsRequired();
        builder.Property(x => x.Purpose)
            .IsRequired();
        builder.Property(x => x.HashedOtp)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(x => x.CreatedOn)
            .IsRequired();
        builder.Property(x => x.ExpiresOn)
            .IsRequired();
        builder.Property(x => x.Attempts)
            .IsRequired();
        builder.Property(x => x.MaxAttempts)
            .IsRequired();
        builder.Property(x => x.ResendWindowStartsOn)
            .IsRequired(false);
        builder.Property(x => x.ConsumedOn)
            .IsRequired(false);

        // FK to UserAccount with cascade delete
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for looking up challenges by account + purpose
        builder.HasIndex(x => new { x.AccountId, x.Purpose })
            .HasDatabaseName("IX_OtpChallenges_AccountId_Purpose");

        // Unique filtered index: one active (non-consumed) challenge per account per purpose
        builder.HasIndex(x => new { x.AccountId, x.Purpose })
            .IsUnique()
            .HasFilter("([ConsumedOn] IS NULL)")
            .HasDatabaseName("UX_OtpChallenges_AccountId_Purpose_Active");

        // Index for expired/consumed cleanup queries
        builder.HasIndex(x => x.ExpiresOn)
            .HasDatabaseName("IX_OtpChallenges_ExpiresOn");
    }
}
