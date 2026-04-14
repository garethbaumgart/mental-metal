using MentalMetal.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace MentalMetal.Domain.Users;

public sealed class User : AggregateRoot
{
    private readonly List<DailyCloseOutLog> _dailyCloseOutLogs = [];

    public string? ExternalAuthId { get; private set; }
    public Email Email { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }
    public Password? PasswordHash { get; private set; }
    public UserPreferences Preferences { get; private set; } = null!;
    public AiProviderConfig? AiProviderConfig { get; private set; }
    public string Timezone { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastLoginAt { get; private set; }

    public IReadOnlyList<DailyCloseOutLog> DailyCloseOutLogs => _dailyCloseOutLogs;

    private User() { } // EF Core

    public static User Register(string externalAuthId, string email, string name, string? avatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalAuthId, nameof(externalAuthId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        var now = DateTimeOffset.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalAuthId = externalAuthId,
            Email = Email.Create(email),
            Name = name.Trim(),
            AvatarUrl = avatarUrl,
            Preferences = UserPreferences.Default(),
            Timezone = "UTC",
            CreatedAt = now,
            LastLoginAt = now
        };

        user.RaiseDomainEvent(new UserRegistered(user.Id, user.Email.Value));

        return user;
    }

    public static User RegisterWithPassword(
        string email,
        string name,
        Password password,
        string? timezone = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(password);

        var now = DateTimeOffset.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalAuthId = null,
            Email = Email.Create(email),
            Name = name.Trim(),
            AvatarUrl = null,
            PasswordHash = password,
            Preferences = UserPreferences.Default(),
            Timezone = string.IsNullOrWhiteSpace(timezone) ? "UTC" : timezone,
            CreatedAt = now,
            LastLoginAt = now
        };

        user.RaiseDomainEvent(new UserRegistered(user.Id, user.Email.Value));

        return user;
    }

    public void SetPassword(string plaintext, IPasswordHasher<User> hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        PasswordHash = Password.Create(plaintext, hasher);
    }

    public bool VerifyPassword(string plaintext, IPasswordHasher<User> hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        return PasswordHash is not null && PasswordHash.Verify(plaintext, hasher);
    }

    public void UpdateProfile(string name, string? avatarUrl, string timezone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(timezone, nameof(timezone));

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timezone, out _))
            throw new ArgumentException($"'{timezone}' is not a valid time zone.", nameof(timezone));

        Name = name.Trim();
        AvatarUrl = avatarUrl;
        Timezone = timezone;

        RaiseDomainEvent(new UserProfileUpdated(Id));
    }

    public void UpdatePreferences(UserPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        Preferences = preferences;

        RaiseDomainEvent(new PreferencesUpdated(Id));
    }

    public void ConfigureAiProvider(AiProvider provider, string encryptedApiKey, string model, int? maxTokens = null)
    {
        AiProviderConfig = new AiProviderConfig(provider, encryptedApiKey, model, maxTokens);
        RaiseDomainEvent(new AiProviderConfigured(Id, provider));
    }

    public void RemoveAiProvider()
    {
        if (AiProviderConfig is null)
            return;

        AiProviderConfig = null;
        RaiseDomainEvent(new AiProviderRemoved(Id));
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
    }

    public DailyCloseOutLog? GetCloseOutLog(DateOnly date) =>
        _dailyCloseOutLogs.FirstOrDefault(l => l.Date == date);

    /// <summary>
    /// Records (or overwrites) the user's close-out summary for the given date.
    /// Returns the log entry together with a flag indicating whether a new entry was appended
    /// (handlers must call <c>IUserRepository.MarkOwnedAdded</c> for new entries because EF Core's
    /// snapshot change detection does not always recognise additions to field-backed owned collections).
    /// </summary>
    public RecordDailyCloseOutResult RecordDailyCloseOut(
        DateOnly date,
        int confirmedCount,
        int discardedCount,
        int remainingCount)
    {
        if (confirmedCount < 0)
            throw new ArgumentOutOfRangeException(nameof(confirmedCount), "Count cannot be negative.");
        if (discardedCount < 0)
            throw new ArgumentOutOfRangeException(nameof(discardedCount), "Count cannot be negative.");
        if (remainingCount < 0)
            throw new ArgumentOutOfRangeException(nameof(remainingCount), "Count cannot be negative.");

        var now = DateTimeOffset.UtcNow;
        var existing = GetCloseOutLog(date);

        if (existing is not null)
        {
            existing.Overwrite(now, confirmedCount, discardedCount, remainingCount);
            RaiseDomainEvent(new DailyCloseOutRecorded(Id, date));
            return new RecordDailyCloseOutResult(existing, IsNew: false);
        }

        var entry = new DailyCloseOutLog(date, now, confirmedCount, discardedCount, remainingCount);
        _dailyCloseOutLogs.Add(entry);
        RaiseDomainEvent(new DailyCloseOutRecorded(Id, date));
        return new RecordDailyCloseOutResult(entry, IsNew: true);
    }
}

public sealed record RecordDailyCloseOutResult(DailyCloseOutLog Log, bool IsNew);
