using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E01;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E01;

public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();
        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(x => x.CreatedOn)
            .IsRequired();
        builder.HasIndex(x => x.DisplayName)
            .IsUnique()
            .HasDatabaseName("UX_UserAccounts_DisplayName");
    }
}
