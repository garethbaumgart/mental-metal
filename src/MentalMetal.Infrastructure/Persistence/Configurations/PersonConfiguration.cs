using MentalMetal.Domain.People;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("People");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Type)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.Email)
            .HasMaxLength(320);

        builder.Property(p => p.Role)
            .HasMaxLength(200);

        builder.Property(p => p.Team)
            .HasMaxLength(200);

        builder.Property(p => p.Notes)
            .HasMaxLength(4000);

        builder.PrimitiveCollection(p => p.Aliases)
            .HasColumnName("Aliases")
            .HasField("_aliases")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.IsArchived)
            .HasDefaultValue(false);

        builder.Property(p => p.ArchivedAt);

        builder.Property(p => p.CreatedAt);
        builder.Property(p => p.UpdatedAt);

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.HasIndex(p => p.UserId);
    }
}
