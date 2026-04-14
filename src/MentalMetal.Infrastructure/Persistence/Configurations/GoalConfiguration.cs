using MentalMetal.Domain.Goals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("Goals");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.UserId).IsRequired();
        builder.Property(g => g.PersonId).IsRequired();

        builder.Property(g => g.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(g => g.Description).HasMaxLength(4000);

        builder.Property(g => g.Type)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(g => g.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(g => g.TargetDate);
        builder.Property(g => g.DeferralReason).HasMaxLength(2000);
        builder.Property(g => g.AchievedAt);
        builder.Property(g => g.CreatedAt);
        builder.Property(g => g.UpdatedAt);

        builder.Navigation(g => g.CheckIns)
            .HasField("_checkIns")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(g => g.CheckIns, checkIn =>
        {
            checkIn.ToTable("GoalCheckIns");
            checkIn.WithOwner().HasForeignKey("GoalId");
            checkIn.HasKey(c => c.Id);
            checkIn.Property(c => c.Note).IsRequired().HasMaxLength(4000);
            checkIn.Property(c => c.Progress);
            checkIn.Property(c => c.RecordedAt).IsRequired();
        });

        builder.HasIndex(g => g.UserId);
        builder.HasIndex(g => g.PersonId);
        builder.HasIndex(g => g.Status);
    }
}
