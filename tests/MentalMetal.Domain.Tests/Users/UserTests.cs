using MentalMetal.Domain.Users;

namespace MentalMetal.Domain.Tests.Users;

public class UserTests
{
    [Fact]
    public void Register_ValidInput_CreatesUserWithCorrectState()
    {
        var user = User.Register("auth-123", "test@example.com", "Test User", "https://avatar.url");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("auth-123", user.ExternalAuthId);
        Assert.Equal("test@example.com", user.Email.Value);
        Assert.Equal("Test User", user.Name);
        Assert.Equal("https://avatar.url", user.AvatarUrl);
        Assert.Equal("UTC", user.Timezone);
        Assert.NotNull(user.Preferences);
        Assert.Equal(Theme.Light, user.Preferences.Theme);
    }

    [Fact]
    public void Register_RaisesUserRegisteredEvent()
    {
        var user = User.Register("auth-123", "test@example.com", "Test User", null);

        var domainEvent = Assert.Single(user.DomainEvents);
        var registered = Assert.IsType<UserRegistered>(domainEvent);
        Assert.Equal(user.Id, registered.UserId);
        Assert.Equal("test@example.com", registered.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Register_EmptyAuthId_Throws(string? authId)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            User.Register(authId!, "test@example.com", "Name", null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Register_EmptyName_Throws(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            User.Register("auth-123", "test@example.com", name!, null));
    }

    [Fact]
    public void UpdateProfile_ValidInput_UpdatesFields()
    {
        var user = User.Register("auth-123", "test@example.com", "Original", null);
        user.ClearDomainEvents();

        user.UpdateProfile("Updated Name", "https://new-avatar.url", "Australia/Sydney");

        Assert.Equal("Updated Name", user.Name);
        Assert.Equal("https://new-avatar.url", user.AvatarUrl);
        Assert.Equal("Australia/Sydney", user.Timezone);
    }

    [Fact]
    public void UpdateProfile_RaisesUserProfileUpdatedEvent()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);
        user.ClearDomainEvents();

        user.UpdateProfile("New Name", null, "UTC");

        var domainEvent = Assert.Single(user.DomainEvents);
        var updated = Assert.IsType<UserProfileUpdated>(domainEvent);
        Assert.Equal(user.Id, updated.UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void UpdateProfile_EmptyName_Throws(string? name)
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);

        Assert.ThrowsAny<ArgumentException>(() =>
            user.UpdateProfile(name!, null, "UTC"));
    }

    [Fact]
    public void UpdateProfile_InvalidTimezone_Throws()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);

        Assert.Throws<ArgumentException>(() =>
            user.UpdateProfile("Name", null, "Invalid/Timezone"));
    }

    [Fact]
    public void UpdatePreferences_UpdatesAndRaisesEvent()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);
        user.ClearDomainEvents();

        var newPrefs = UserPreferences.Create(Theme.Dark, false, new TimeOnly(9, 30));
        user.UpdatePreferences(newPrefs);

        Assert.Equal(Theme.Dark, user.Preferences.Theme);
        Assert.False(user.Preferences.NotificationsEnabled);
        Assert.Equal(new TimeOnly(9, 30), user.Preferences.BriefingTime);

        var domainEvent = Assert.Single(user.DomainEvents);
        Assert.IsType<PreferencesUpdated>(domainEvent);
    }

    [Fact]
    public void UpdatePreferences_Null_Throws()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);

        Assert.Throws<ArgumentNullException>(() => user.UpdatePreferences(null!));
    }

    [Fact]
    public void Register_HasNullAiProviderConfig()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);

        Assert.Null(user.AiProviderConfig);
    }

    [Fact]
    public void ConfigureAiProvider_SetsConfigAndRaisesEvent()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);
        user.ClearDomainEvents();

        user.ConfigureAiProvider(AiProvider.Anthropic, "enc_key", "claude-sonnet-4-20250514", 4096);

        Assert.NotNull(user.AiProviderConfig);
        Assert.Equal(AiProvider.Anthropic, user.AiProviderConfig.Provider);
        Assert.Equal("claude-sonnet-4-20250514", user.AiProviderConfig.Model);

        var domainEvent = Assert.Single(user.DomainEvents);
        var configured = Assert.IsType<AiProviderConfigured>(domainEvent);
        Assert.Equal(user.Id, configured.UserId);
        Assert.Equal(AiProvider.Anthropic, configured.Provider);
    }

    [Fact]
    public void ConfigureAiProvider_ReplacesExistingConfig()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);
        user.ConfigureAiProvider(AiProvider.Anthropic, "enc_key_1", "claude-sonnet-4-20250514", null);
        user.ClearDomainEvents();

        user.ConfigureAiProvider(AiProvider.OpenAI, "enc_key_2", "gpt-4o", null);

        Assert.Equal(AiProvider.OpenAI, user.AiProviderConfig!.Provider);
        Assert.Equal("gpt-4o", user.AiProviderConfig.Model);

        var domainEvent = Assert.Single(user.DomainEvents);
        var configured = Assert.IsType<AiProviderConfigured>(domainEvent);
        Assert.Equal(AiProvider.OpenAI, configured.Provider);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ConfigureAiProvider_EmptyKey_Throws(string? key)
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);

        Assert.ThrowsAny<ArgumentException>(() =>
            user.ConfigureAiProvider(AiProvider.Anthropic, key!, "model", null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ConfigureAiProvider_EmptyModel_Throws(string? model)
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);

        Assert.ThrowsAny<ArgumentException>(() =>
            user.ConfigureAiProvider(AiProvider.Anthropic, "enc_key", model!, null));
    }

    [Fact]
    public void RemoveAiProvider_ClearsConfigAndRaisesEvent()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);
        user.ConfigureAiProvider(AiProvider.Anthropic, "enc_key", "model", null);
        user.ClearDomainEvents();

        user.RemoveAiProvider();

        Assert.Null(user.AiProviderConfig);
        var domainEvent = Assert.Single(user.DomainEvents);
        Assert.IsType<AiProviderRemoved>(domainEvent);
    }

    [Fact]
    public void RemoveAiProvider_WhenNull_IsIdempotent()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);
        user.ClearDomainEvents();

        user.RemoveAiProvider();

        Assert.Null(user.AiProviderConfig);
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void RecordLogin_UpdatesLastLoginAt()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);
        var originalLogin = user.LastLoginAt;

        // Ensure time passes
        Thread.Sleep(1);

        user.RecordLogin();

        Assert.True(user.LastLoginAt >= originalLogin);
    }
}
