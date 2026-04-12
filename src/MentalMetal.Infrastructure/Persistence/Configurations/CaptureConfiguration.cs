using MentalMetal.Domain.Captures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class CaptureConfiguration : IEntityTypeConfiguration<Capture>
{
    public void Configure(EntityTypeBuilder<Capture> builder)
    {
        builder.ToTable("Captures");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.RawContent)
            .IsRequired();

        builder.Property(c => c.CaptureType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.ProcessingStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.AiExtraction);

        builder.Property(c => c.Title)
            .HasMaxLength(500);

        builder.Property(c => c.Source)
            .HasMaxLength(500);

        builder.PrimitiveCollection(c => c.LinkedPersonIds)
            .HasColumnName("LinkedPersonIds")
            .HasField("_linkedPersonIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.PrimitiveCollection(c => c.LinkedInitiativeIds)
            .HasColumnName("LinkedInitiativeIds")
            .HasField("_linkedInitiativeIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.PrimitiveCollection(c => c.SpawnedCommitmentIds)
            .HasColumnName("SpawnedCommitmentIds")
            .HasField("_spawnedCommitmentIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.PrimitiveCollection(c => c.SpawnedDelegationIds)
            .HasColumnName("SpawnedDelegationIds")
            .HasField("_spawnedDelegationIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.PrimitiveCollection(c => c.SpawnedObservationIds)
            .HasColumnName("SpawnedObservationIds")
            .HasField("_spawnedObservationIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(c => c.CapturedAt);
        builder.Property(c => c.ProcessedAt);
        builder.Property(c => c.UpdatedAt);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.HasIndex(c => c.UserId);
    }
}
