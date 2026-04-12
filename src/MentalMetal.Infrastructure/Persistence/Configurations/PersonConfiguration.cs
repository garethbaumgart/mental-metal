using MentalMetal.Domain.People;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("People");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Type)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.Email)
            .HasMaxLength(320);

        builder.Property(p => p.Role)
            .HasMaxLength(200);

        builder.Property(p => p.Team)
            .HasMaxLength(200);

        builder.Property(p => p.Notes)
            .HasMaxLength(4000);

        builder.OwnsOne(p => p.CareerDetails, career =>
        {
            career.Property(c => c.Level)
                .HasColumnName("CareerDetails_Level")
                .HasMaxLength(100);

            career.Property(c => c.Aspirations)
                .HasColumnName("CareerDetails_Aspirations")
                .HasMaxLength(2000);

            career.Property(c => c.GrowthAreas)
                .HasColumnName("CareerDetails_GrowthAreas")
                .HasMaxLength(2000);
        });

        builder.OwnsOne(p => p.CandidateDetails, candidate =>
        {
            candidate.Property(c => c.PipelineStatus)
                .HasColumnName("CandidateDetails_PipelineStatus")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            candidate.Property(c => c.CvNotes)
                .HasColumnName("CandidateDetails_CvNotes")
                .HasMaxLength(4000);

            candidate.Property(c => c.SourceChannel)
                .HasColumnName("CandidateDetails_SourceChannel")
                .HasMaxLength(200);
        });

        builder.Property(p => p.IsArchived)
            .HasDefaultValue(false);

        builder.Property(p => p.ArchivedAt);

        builder.Property(p => p.CreatedAt);
        builder.Property(p => p.UpdatedAt);

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.HasIndex(p => p.UserId);

        builder.HasIndex(p => new { p.UserId, p.Name })
            .IsUnique()
            .HasFilter("\"IsArchived\" = false");
    }
}
