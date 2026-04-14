using MentalMetal.Domain.Briefings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class BriefingConfiguration : IEntityTypeConfiguration<Briefing>
{
    public void Configure(EntityTypeBuilder<Briefing> builder)
    {
        builder.ToTable("Briefings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.UserId).IsRequired();

        builder.Property(b => b.Type)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.ScopeKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(b => b.GeneratedAtUtc).IsRequired();

        // MarkdownBody can be large; map to text rather than the default varchar(n).
        builder.Property(b => b.MarkdownBody)
            .HasColumnType("text")
            .IsRequired();

        // Facts payload as jsonb for future querying / debugging.
        builder.Property(b => b.PromptFactsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(b => b.Model)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(b => b.InputTokens).IsRequired();
        builder.Property(b => b.OutputTokens).IsRequired();

        // Composite unique index supporting GetLatestAsync(userId, type, scopeKey)
        // and defending against duplicate-row races on identical timestamps.
        builder.HasIndex(b => new { b.UserId, b.Type, b.ScopeKey, b.GeneratedAtUtc })
            .IsUnique()
            .IsDescending(false, false, false, true)
            .HasDatabaseName("IX_Briefings_User_Type_Scope_GeneratedAt");

        builder.HasIndex(b => b.UserId);
    }
}
