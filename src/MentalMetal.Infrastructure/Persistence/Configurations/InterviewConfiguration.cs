using System.Text.Json;
using MentalMetal.Domain.Interviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class InterviewConfiguration : IEntityTypeConfiguration<Interview>
{
    public void Configure(EntityTypeBuilder<Interview> builder)
    {
        builder.ToTable("Interviews");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.UserId).IsRequired();
        builder.Property(i => i.CandidatePersonId).IsRequired();
        builder.Property(i => i.RoleTitle).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Stage).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(i => i.ScheduledAtUtc);
        builder.Property(i => i.CompletedAtUtc);
        builder.Property(i => i.Decision).HasConversion<string?>().HasMaxLength(40);
        builder.Property(i => i.CreatedAtUtc).IsRequired();
        builder.Property(i => i.UpdatedAtUtc).IsRequired();

        builder.HasIndex(i => i.UserId);
        builder.HasIndex(i => i.CandidatePersonId);

        // Owned collection: scorecards
        builder.Navigation(i => i.Scorecards)
            .HasField("_scorecards")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(i => i.Scorecards, s =>
        {
            s.ToTable("InterviewScorecards");
            s.WithOwner().HasForeignKey("InterviewId");
            s.HasKey(x => x.Id);
            s.Property(x => x.Competency).IsRequired().HasMaxLength(200);
            s.Property(x => x.Rating).IsRequired();
            s.Property(x => x.Notes).HasMaxLength(4000);
            s.Property(x => x.RecordedAtUtc).IsRequired();
            s.HasIndex("InterviewId");
        });

        // Owned value object: transcript (single, nullable)
        builder.OwnsOne(i => i.Transcript, t =>
        {
            t.Property(x => x.RawText)
                .HasColumnName("Transcript_RawText")
                .HasColumnType("text");
            t.Property(x => x.Summary)
                .HasColumnName("Transcript_Summary")
                .HasColumnType("text");
            t.Property(x => x.RecommendedDecision)
                .HasColumnName("Transcript_RecommendedDecision")
                .HasConversion<string?>()
                .HasMaxLength(40);
            t.Property(x => x.AnalyzedAtUtc)
                .HasColumnName("Transcript_AnalyzedAtUtc");
            t.Property(x => x.Model)
                .HasColumnName("Transcript_Model")
                .HasMaxLength(200);

            // Serialise the RiskSignals list (backed by `_riskSignals`) into a single jsonb
            // column. Using a property-level value converter keeps the domain API a
            // read-only IReadOnlyList while EF round-trips the backing field to Postgres.
            t.Property(x => x.RiskSignals)
                .HasField("_riskSignals")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasColumnName("Transcript_RiskSignals")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v ?? (IReadOnlyList<string>)Array.Empty<string>(), (JsonSerializerOptions?)null),
                    v => string.IsNullOrWhiteSpace(v)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>(),
                    new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<string>>(
                        (a, b) => (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>()),
                        v => v == null ? 0 : v.Aggregate(0, (h, s) => HashCode.Combine(h, s)),
                        v => (IReadOnlyList<string>)((v ?? Array.Empty<string>()).ToList())));
        });
    }
}
