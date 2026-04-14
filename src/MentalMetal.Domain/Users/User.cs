using MentalMetal.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace MentalMetal.Domain.Users;

public sealed class User : AggregateRoot
{
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
}
