using System.Text.Json;
using MentalMetal.Domain.Initiatives.LivingBrief;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class PendingBriefUpdateConfiguration : IEntityTypeConfiguration<PendingBriefUpdate>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<PendingBriefUpdate> builder)
    {
        builder.ToTable("PendingBriefUpdates");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.InitiativeId).IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.BriefVersionAtProposal).IsRequired();

        builder.Property(p => p.FailureReason).HasMaxLength(2000);

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        var proposalConverter = new ValueConverter<BriefUpdateProposal, string>(
            v => JsonSerializer.Serialize(v ?? new BriefUpdateProposal(), JsonOpts),
            v => string.IsNullOrEmpty(v)
                ? new BriefUpdateProposal()
                : JsonSerializer.Deserialize<BriefUpdateProposal>(v, JsonOpts) ?? new BriefUpdateProposal());

        var proposalComparer = new ValueComparer<BriefUpdateProposal>(
            (a, b) => JsonSerializer.Serialize(a, JsonOpts) == JsonSerializer.Serialize(b, JsonOpts),
            v => JsonSerializer.Serialize(v, JsonOpts).GetHashCode(),
            v => JsonSerializer.Deserialize<BriefUpdateProposal>(JsonSerializer.Serialize(v, JsonOpts), JsonOpts) ?? new BriefUpdateProposal());

        builder.Property(p => p.Proposal)
            .HasColumnName("Proposal")
            .HasColumnType("jsonb")
            .HasConversion(proposalConverter, proposalComparer)
            .IsRequired();

        builder.HasIndex(p => new { p.UserId, p.InitiativeId, p.Status, p.CreatedAt });
    }
}
