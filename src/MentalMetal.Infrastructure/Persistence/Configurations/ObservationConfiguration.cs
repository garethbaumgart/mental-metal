using MentalMetal.Domain.Observations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
    public void Configure(EntityTypeBuilder<Observation> builder)
    {
        builder.ToTable("Observations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId).IsRequired();
        builder.Property(o => o.PersonId).IsRequired();

        builder.Property(o => o.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(o => o.Tag)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.OccurredAt).IsRequired();
        builder.Property(o => o.SourceCaptureId);
        builder.Property(o => o.CreatedAt);
        builder.Property(o => o.UpdatedAt);

        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.PersonId);
        builder.HasIndex(o => o.Tag);
    }
}
