using MentalMetal.Domain.Initiatives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class InitiativeConfiguration : IEntityTypeConfiguration<Initiative>
{
    public void Configure(EntityTypeBuilder<Initiative> builder)
    {
        builder.ToTable("Initiatives");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(i => i.AutoSummary);

        builder.Property(i => i.LastSummaryRefreshedAt);

        builder.Property(i => i.CreatedAt);
        builder.Property(i => i.UpdatedAt);

        builder.Property(i => i.UserId)
            .IsRequired();

        builder.HasIndex(i => i.UserId);
    }
}
