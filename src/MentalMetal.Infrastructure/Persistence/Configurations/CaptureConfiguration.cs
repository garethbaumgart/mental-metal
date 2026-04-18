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
            .HasMaxLength(30);

        builder.Property(c => c.CaptureSource)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.ProcessingStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.OwnsOne(c => c.AiExtraction, extraction =>
        {
            extraction.ToJson();
            extraction.OwnsMany(e => e.PeopleMentioned);
            extraction.OwnsMany(e => e.Commitments);
            extraction.OwnsMany(e => e.InitiativeTags);
        });

        builder.Property(c => c.FailureReason)
            .HasMaxLength(2000);

        builder.Property(c => c.Title)
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

        builder.Property(c => c.CapturedAt);
        builder.Property(c => c.ProcessedAt);
        builder.Property(c => c.UpdatedAt);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.HasIndex(c => c.UserId);

        // --- Audio / transcription fields ---
        builder.Property(c => c.AudioBlobRef).HasMaxLength(500);
        builder.Property(c => c.AudioMimeType).HasMaxLength(100);
        builder.Property(c => c.AudioDurationSeconds);
        builder.Property(c => c.AudioDiscardedAt);
        builder.Property(c => c.TranscriptionStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(TranscriptionStatus.NotApplicable);
        builder.Property(c => c.TranscriptionFailureReason).HasMaxLength(2000);

        builder.OwnsMany(c => c.TranscriptSegments, segment =>
        {
            segment.ToTable("CaptureTranscriptSegments");
            segment.WithOwner().HasForeignKey("CaptureId");
            segment.Property<Guid>("CaptureId");
            segment.HasKey(s => s.Id);
            segment.Property(s => s.Id).ValueGeneratedNever();
            segment.Property(s => s.StartSeconds).IsRequired();
            segment.Property(s => s.EndSeconds).IsRequired();
            segment.Property(s => s.SpeakerLabel)
                .IsRequired()
                .HasMaxLength(TranscriptSegment.MaxSpeakerLabelLength);
            segment.Property(s => s.Text)
                .IsRequired()
                .HasMaxLength(TranscriptSegment.MaxTextLength);
            segment.Property(s => s.LinkedPersonId);
            segment.HasIndex("CaptureId");
        });
        builder.Navigation(c => c.TranscriptSegments)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_transcriptSegments");
    }
}
