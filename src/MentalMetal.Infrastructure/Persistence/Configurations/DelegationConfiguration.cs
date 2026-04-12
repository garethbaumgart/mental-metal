using MentalMetal.Domain.Delegations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class DelegationConfiguration : IEntityTypeConfiguration<Delegation>
{
    public void Configure(EntityTypeBuilder<Delegation> builder)
    {
        builder.ToTable("Delegations");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(d => d.Priority)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(d => d.DelegatePersonId)
            .IsRequired();

        builder.Property(d => d.Notes)
            .HasMaxLength(4000);

        builder.Property(d => d.UserId)
            .IsRequired();

        builder.HasIndex(d => d.UserId);
        builder.HasIndex(d => d.DelegatePersonId);
        builder.HasIndex(d => d.InitiativeId);
    }
}
