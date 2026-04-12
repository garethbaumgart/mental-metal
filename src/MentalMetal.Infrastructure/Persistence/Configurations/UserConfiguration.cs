using MentalMetal.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.ExternalAuthId)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.ExternalAuthId)
            .IsUnique();

        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .IsRequired()
                .HasMaxLength(320);

            email.HasIndex(e => e.Value)
                .IsUnique();
        });

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(2048);

        builder.OwnsOne(u => u.Preferences, prefs =>
        {
            prefs.Property(p => p.Theme)
                .HasColumnName("PreferencesTheme")
                .HasConversion<string>()
                .HasMaxLength(10);

            prefs.Property(p => p.NotificationsEnabled)
                .HasColumnName("PreferencesNotificationsEnabled");

            prefs.Property(p => p.BriefingTime)
                .HasColumnName("PreferencesBriefingTime");
        });

        builder.OwnsOne(u => u.AiProviderConfig, ai =>
        {
            ai.Property(a => a.Provider)
                .HasColumnName("AiProviderProvider")
                .HasConversion<string>()
                .HasMaxLength(20);

            ai.Property(a => a.EncryptedApiKey)
                .HasColumnName("AiProviderEncryptedApiKey")
                .IsRequired()
                .HasMaxLength(1024);

            ai.Property(a => a.Model)
                .HasColumnName("AiProviderModel")
                .IsRequired()
                .HasMaxLength(100);

            ai.Property(a => a.MaxTokens)
                .HasColumnName("AiProviderMaxTokens");
        });

        builder.Property(u => u.Timezone)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.CreatedAt);
        builder.Property(u => u.LastLoginAt);
    }
}
