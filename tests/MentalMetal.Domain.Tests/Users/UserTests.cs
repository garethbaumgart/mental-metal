using MentalMetal.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace MentalMetal.Domain.Tests.Users;

public class UserTests
{
    private static readonly IPasswordHasher<User> Hasher = new PasswordHasher<User>();

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
    public void RegisterWithPassword_SetsStateCorrectly()
    {
        var password = Password.Create("secret-pw", Hasher);

        var user = User.RegisterWithPassword("new@example.com", "New User", password, null);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Null(user.ExternalAuthId);
        Assert.Equal("new@example.com", user.Email.Value);
        Assert.Equal("New User", user.Name);
        Assert.Null(user.AvatarUrl);
        Assert.NotNull(user.PasswordHash);
        Assert.Equal(password, user.PasswordHash);
        Assert.Equal("UTC", user.Timezone);
        Assert.NotNull(user.Preferences);
    }

    [Fact]
    public void RegisterWithPassword_UsesProvidedTimezone()
    {
        var password = Password.Create("secret-pw", Hasher);

        var user = User.RegisterWithPassword("x@y.com", "N", password, "Australia/Sydney");

        Assert.Equal("Australia/Sydney", user.Timezone);
    }

    [Fact]
    public void RegisterWithPassword_RaisesUserRegisteredEvent()
    {
        var password = Password.Create("secret-pw", Hasher);

        var user = User.RegisterWithPassword("n@example.com", "N", password, null);

        var ev = Assert.Single(user.DomainEvents);
        var registered = Assert.IsType<UserRegistered>(ev);
        Assert.Equal(user.Id, registered.UserId);
        Assert.Equal("n@example.com", registered.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void RegisterWithPassword_EmptyName_Throws(string? name)
    {
        var password = Password.Create("secret-pw", Hasher);

        Assert.ThrowsAny<ArgumentException>(() =>
            User.RegisterWithPassword("a@b.com", name!, password, null));
    }

    [Fact]
    public void SetPassword_OnGoogleUser_SetsHash()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);

        user.SetPassword("new-password", Hasher);

        Assert.NotNull(user.PasswordHash);
        Assert.True(user.VerifyPassword("new-password", Hasher));
    }

    [Fact]
    public void SetPassword_ReplacesExistingHash()
    {
        var password = Password.Create("first-pw", Hasher);
        var user = User.RegisterWithPassword("a@b.com", "N", password, null);

        user.SetPassword("second-pw", Hasher);

        Assert.True(user.VerifyPassword("second-pw", Hasher));
        Assert.False(user.VerifyPassword("first-pw", Hasher));
    }

    [Fact]
    public void SetPassword_ShortPassword_Throws()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);

        Assert.ThrowsAny<ArgumentException>(() => user.SetPassword("short", Hasher));
    }

    [Fact]
    public void VerifyPassword_WhenHashIsNull_ReturnsFalse()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);

        Assert.False(user.VerifyPassword("anything", Hasher));
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var password = Password.Create("correct-pw", Hasher);
        var user = User.RegisterWithPassword("a@b.com", "N", password, null);

        Assert.False(user.VerifyPassword("wrong-pw", Hasher));
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

    // --- Daily close-out ---

    [Fact]
    public void RecordDailyCloseOut_NewDate_AppendsLogAndRaisesEvent()
    {
        var user = User.Register("auth-1", "a@b.com", "A", null);
        user.ClearDomainEvents();

        var result = user.RecordDailyCloseOut(new DateOnly(2026, 4, 14), 3, 1, 2);

        Assert.True(result.IsNew);
        var log = Assert.Single(user.DailyCloseOutLogs);
        Assert.Equal(new DateOnly(2026, 4, 14), log.Date);
        Assert.Equal(3, log.ConfirmedCount);
        Assert.Equal(1, log.DiscardedCount);
        Assert.Equal(2, log.RemainingCount);
        Assert.Same(log, result.Log);
        var evt = Assert.Single(user.DomainEvents);
        Assert.IsType<DailyCloseOutRecorded>(evt);
    }

    [Fact]
    public void RecordDailyCloseOut_SameDate_OverwritesExistingLog()
    {
        var user = User.Register("auth-1", "a@b.com", "A", null);
        var date = new DateOnly(2026, 4, 14);
        user.RecordDailyCloseOut(date, 1, 0, 5);
        var originalId = user.DailyCloseOutLogs[0].Id;
        user.ClearDomainEvents();

        var result = user.RecordDailyCloseOut(date, 4, 2, 0);

        Assert.False(result.IsNew);
        var log = Assert.Single(user.DailyCloseOutLogs);
        Assert.Equal(originalId, log.Id);
        Assert.Equal(4, log.ConfirmedCount);
        Assert.Equal(2, log.DiscardedCount);
        Assert.Equal(0, log.RemainingCount);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void RecordDailyCloseOut_NegativeCount_Throws(int confirmed, int discarded, int remaining)
    {
        var user = User.Register("auth-1", "a@b.com", "A", null);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.RecordDailyCloseOut(new DateOnly(2026, 4, 14), confirmed, discarded, remaining));
    }

    [Fact]
    public void RecordDailyCloseOut_MultiUser_IsolatedPerUser()
    {
        var userA = User.Register("auth-a", "a@b.com", "A", null);
        var userB = User.Register("auth-b", "b@b.com", "B", null);

        userA.RecordDailyCloseOut(new DateOnly(2026, 4, 14), 1, 2, 3);
        userB.RecordDailyCloseOut(new DateOnly(2026, 4, 14), 9, 8, 7);

        Assert.Single(userA.DailyCloseOutLogs);
        Assert.Single(userB.DailyCloseOutLogs);
        Assert.Equal(1, userA.DailyCloseOutLogs[0].ConfirmedCount);
        Assert.Equal(9, userB.DailyCloseOutLogs[0].ConfirmedCount);
    }

    [Fact]
    public void GetCloseOutLog_ReturnsMatchingOrNull()
    {
        var user = User.Register("auth-1", "a@b.com", "A", null);
        user.RecordDailyCloseOut(new DateOnly(2026, 4, 14), 1, 0, 0);

        Assert.NotNull(user.GetCloseOutLog(new DateOnly(2026, 4, 14)));
        Assert.Null(user.GetCloseOutLog(new DateOnly(2026, 4, 15)));
    }
}
