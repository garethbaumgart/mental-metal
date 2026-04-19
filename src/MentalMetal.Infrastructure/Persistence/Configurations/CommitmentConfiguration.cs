using MentalMetal.Domain.Commitments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class CommitmentConfiguration : IEntityTypeConfiguration<Commitment>
{
    public void Configure(EntityTypeBuilder<Commitment> builder)
    {
        builder.ToTable("Commitments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.Direction)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Confidence)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue(CommitmentConfidence.High);

        builder.Property(c => c.PersonId)
            .IsRequired();

        builder.Property(c => c.DismissedAt);

        builder.Property(c => c.Notes)
            .HasMaxLength(4000);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.PersonId);
        builder.HasIndex(c => c.InitiativeId);
        builder.HasIndex(c => c.SourceCaptureId);

        builder.Property(c => c.SourceStartOffset);
        builder.Property(c => c.SourceEndOffset);

        builder.Ignore(c => c.IsOverdue);
    }
}
