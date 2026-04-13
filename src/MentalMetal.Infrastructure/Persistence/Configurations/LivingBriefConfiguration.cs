using System.Text.Json;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class LivingBriefConfiguration : IEntityTypeConfiguration<Initiative>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<Initiative> builder)
    {
        builder.OwnsOne(i => i.Brief, brief =>
        {
            brief.ToTable("Initiatives");

            // The IReadOnlyList projection properties are not EF navigations —
            // backing fields configured below are the source of truth.
            brief.Ignore(b => b.KeyDecisions);
            brief.Ignore(b => b.Risks);
            brief.Ignore(b => b.RequirementsHistory);
            brief.Ignore(b => b.DesignDirectionHistory);

            brief.Property(b => b.Summary)
                .HasColumnName("BriefSummary")
                .HasColumnType("text")
                .HasDefaultValue(string.Empty);

            brief.Property(b => b.SummaryLastRefreshedAt)
                .HasColumnName("BriefSummaryLastRefreshedAt");

            brief.Property(b => b.BriefVersion)
                .HasColumnName("BriefVersion")
                .HasDefaultValue(0);

            brief.Property(b => b.SummarySource)
                .HasColumnName("BriefSummarySource")
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(BriefSource.Manual);

            brief.PrimitiveCollection(b => b.SummarySourceCaptureIds)
                .HasColumnName("BriefSummarySourceCaptureIds");

            ConfigureJsonListBackingField<KeyDecision>(brief, "_keyDecisions", "BriefKeyDecisions");
            ConfigureJsonListBackingField<Risk>(brief, "_risks", "BriefRisks");
            ConfigureJsonListBackingField<RequirementsSnapshot>(brief, "_requirementsHistory", "BriefRequirementsHistory");
            ConfigureJsonListBackingField<DesignDirectionSnapshot>(brief, "_designDirectionHistory", "BriefDesignDirectionHistory");
        });

        builder.Navigation(i => i.Brief).IsRequired();
    }

    private static void ConfigureJsonListBackingField<T>(
        OwnedNavigationBuilder<Initiative, Domain.Initiatives.LivingBrief.LivingBrief> brief,
        string fieldName,
        string columnName)
    {
        var converter = new ValueConverter<List<T>, string>(
            list => JsonSerializer.Serialize(list ?? new(), JsonOpts),
            json => string.IsNullOrEmpty(json)
                ? new List<T>()
                : JsonSerializer.Deserialize<List<T>>(json, JsonOpts) ?? new List<T>());

        var comparer = new ValueComparer<List<T>>(
            (a, b) => JsonSerializer.Serialize(a, JsonOpts) == JsonSerializer.Serialize(b, JsonOpts),
            v => JsonSerializer.Serialize(v, JsonOpts).GetHashCode(),
            v => JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(v, JsonOpts), JsonOpts) ?? new List<T>());

        brief.Property<List<T>>(fieldName)
            .HasField(fieldName)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName(columnName)
            .HasColumnType("jsonb")
            .HasConversion(converter, comparer)
            .HasDefaultValueSql("'[]'::jsonb");
    }
}
