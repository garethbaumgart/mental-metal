using MentalMetal.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(r => r.Token)
            .IsUnique();

        builder.HasIndex(r => r.UserId);

        builder.Property(r => r.ExpiresAt);
        builder.Property(r => r.IsRevoked);
        builder.Property(r => r.CreatedAt);
    }
}
