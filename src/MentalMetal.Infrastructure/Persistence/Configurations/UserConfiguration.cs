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

        builder.OwnsOne(u => u.PasswordHash, pw =>
        {
            pw.Property(p => p.HashValue)
                .HasColumnName("PasswordHash")
                .HasMaxLength(1024);
        });

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

            prefs.Property(p => p.LivingBriefAutoApply)
                .HasColumnName("PreferencesLivingBriefAutoApply")
                .HasDefaultValue(false);
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

        builder.Navigation(u => u.DailyCloseOutLogs)
            .HasField("_dailyCloseOutLogs")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(u => u.DailyCloseOutLogs, log =>
        {
            log.ToTable("DailyCloseOutLogs");
            log.WithOwner().HasForeignKey("UserId");
            log.HasKey(l => l.Id);
            log.Property(l => l.Date).IsRequired();
            log.Property(l => l.ClosedAtUtc).IsRequired();
            log.Property(l => l.ConfirmedCount).IsRequired();
            log.Property(l => l.DiscardedCount).IsRequired();
            log.Property(l => l.RemainingCount).IsRequired();
            log.HasIndex("UserId", nameof(DailyCloseOutLog.Date)).IsUnique();
        });
    }
}
