using MentalMetal.Domain.Nudges;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class NudgeConfiguration : IEntityTypeConfiguration<Nudge>
{
    public void Configure(EntityTypeBuilder<Nudge> builder)
    {
        builder.ToTable("Nudges");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId).IsRequired();

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(Nudge.MaxTitleLength);

        builder.Property(n => n.Notes)
            .HasMaxLength(Nudge.MaxNotesLength);

        builder.Property(n => n.StartDate).IsRequired();
        builder.Property(n => n.NextDueDate);
        builder.Property(n => n.LastNudgedAt);
        builder.Property(n => n.PersonId);
        builder.Property(n => n.InitiativeId);
        builder.Property(n => n.IsActive).IsRequired();
        builder.Property(n => n.CreatedAtUtc).IsRequired();
        builder.Property(n => n.UpdatedAtUtc).IsRequired();

        builder.OwnsOne(n => n.Cadence, cadence =>
        {
            cadence.Property(c => c.Type)
                .HasColumnName("CadenceType")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            cadence.Property(c => c.CustomIntervalDays)
                .HasColumnName("CadenceCustomIntervalDays");

            cadence.Property(c => c.DayOfWeek)
                .HasColumnName("CadenceDayOfWeek")
                .HasConversion<string>()
                .HasMaxLength(20);

            cadence.Property(c => c.DayOfMonth)
                .HasColumnName("CadenceDayOfMonth");
        });

        builder.Navigation(n => n.Cadence).IsRequired();

        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.NextDueDate);
        builder.HasIndex(n => n.IsActive);
        builder.HasIndex(n => n.PersonId);
        builder.HasIndex(n => n.InitiativeId);
    }
}
