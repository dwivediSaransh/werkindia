using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E00;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E00;

public class ExampleEntityConfiguration : IEntityTypeConfiguration<ExampleEntity>
{
    public void Configure(EntityTypeBuilder<ExampleEntity> builder)
    {
        builder.ToTable("ExampleEntities");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);
    }
}

public sealed class Person
{
    public int Id { get; set; }
    public string GivenName { get; set; } = default!;
    public string FamilyName { get; set; } = default!;
}