using MentalMetal.Domain.Initiatives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class InitiativeConfiguration : IEntityTypeConfiguration<Initiative>
{
    public void Configure(EntityTypeBuilder<Initiative> builder)
    {
        builder.ToTable("Initiatives");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Navigation(i => i.Milestones)
            .HasField("_milestones")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(i => i.Milestones, milestone =>
        {
            milestone.ToTable("InitiativeMilestones");

            milestone.WithOwner().HasForeignKey("InitiativeId");

            milestone.HasKey(m => m.Id);

            milestone.Property(m => m.Title)
                .IsRequired()
                .HasMaxLength(300);

            milestone.Property(m => m.TargetDate);

            milestone.Property(m => m.Description)
                .HasMaxLength(2000);

            milestone.Property(m => m.IsCompleted)
                .HasDefaultValue(false);
        });

        builder.PrimitiveCollection(i => i.LinkedPersonIds)
            .HasColumnName("LinkedPersonIds")
            .HasField("_linkedPersonIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(i => i.CreatedAt);
        builder.Property(i => i.UpdatedAt);

        builder.Property(i => i.UserId)
            .IsRequired();

        builder.HasIndex(i => i.UserId);
    }
}
