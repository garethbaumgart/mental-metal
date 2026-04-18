using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Users;

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string Name,
    string? AvatarUrl,
    string Timezone,
    UserPreferencesDto Preferences,
    bool HasAiProvider,
    bool HasPassword,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastLoginAt)
{
    public static UserProfileResponse FromDomain(User user) =>
        new(
            user.Id,
            user.Email.Value,
            user.Name,
            user.AvatarUrl,
            user.Timezone,
            new UserPreferencesDto(
                user.Preferences.Theme.ToString(),
                user.Preferences.NotificationsEnabled,
                user.Preferences.BriefingTime,
                user.Preferences.LivingBriefAutoApply),
            user.AiProviderConfig is not null,
            user.PasswordHash is not null,
            user.CreatedAt,
            user.LastLoginAt);
}

public sealed record UserPreferencesDto(
    string Theme,
    bool NotificationsEnabled,
    TimeOnly BriefingTime,
    bool LivingBriefAutoApply = false);

public sealed record UpdateProfileRequest(
    string Name,
    string? AvatarUrl,
    string Timezone);

public sealed record UpdatePreferencesRequest(
    string Theme,
    bool NotificationsEnabled,
    TimeOnly BriefingTime,
    bool LivingBriefAutoApply = false);

public sealed record AuthTokenResponse(string AccessToken, string? RefreshToken = null);

public sealed record RegisterWithPasswordRequest(string Email, string Password, string Name);

public sealed record LoginWithPasswordRequest(string Email, string Password);

public sealed record SetPasswordRequest(string NewPassword);
