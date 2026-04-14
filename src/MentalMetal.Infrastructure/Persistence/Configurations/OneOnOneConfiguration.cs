using MentalMetal.Domain.OneOnOnes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class OneOnOneConfiguration : IEntityTypeConfiguration<OneOnOne>
{
    public void Configure(EntityTypeBuilder<OneOnOne> builder)
    {
        builder.ToTable("OneOnOnes");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId).IsRequired();
        builder.Property(o => o.PersonId).IsRequired();
        builder.Property(o => o.OccurredAt).IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(8000);
        builder.Property(o => o.MoodRating);
        builder.Property(o => o.CreatedAt);
        builder.Property(o => o.UpdatedAt);

        builder.PrimitiveCollection(o => o.Topics)
            .HasColumnName("Topics")
            .HasField("_topics")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(o => o.ActionItems)
            .HasField("_actionItems")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(o => o.ActionItems, item =>
        {
            item.ToTable("OneOnOneActionItems");
            item.WithOwner().HasForeignKey("OneOnOneId");
            item.HasKey(a => a.Id);
            item.Property(a => a.Description).IsRequired().HasMaxLength(2000);
            item.Property(a => a.Completed).HasDefaultValue(false);
        });

        builder.Navigation(o => o.FollowUps)
            .HasField("_followUps")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(o => o.FollowUps, fu =>
        {
            fu.ToTable("OneOnOneFollowUps");
            fu.WithOwner().HasForeignKey("OneOnOneId");
            fu.HasKey(f => f.Id);
            fu.Property(f => f.Description).IsRequired().HasMaxLength(2000);
            fu.Property(f => f.Resolved).HasDefaultValue(false);
        });

        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.PersonId);
    }
}
