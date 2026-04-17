using MentalMetal.Domain.PersonalAccessTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class PersonalAccessTokenConfiguration : IEntityTypeConfiguration<PersonalAccessToken>
{
    public void Configure(EntityTypeBuilder<PersonalAccessToken> builder)
    {
        builder.ToTable("PersonalAccessTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Scopes)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .IsRequired();

        builder.Property(t => t.TokenLookupPrefix)
            .IsRequired();

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.TokenLookupPrefix);
    }
}
