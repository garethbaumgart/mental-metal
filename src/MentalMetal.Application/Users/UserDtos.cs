namespace MentalMetal.Application.Users;

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string Name,
    string? AvatarUrl,
    string Timezone,
    UserPreferencesDto Preferences,
    bool HasAiProvider,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastLoginAt);

public sealed record UserPreferencesDto(
    string Theme,
    bool NotificationsEnabled,
    TimeOnly BriefingTime);

public sealed record UpdateProfileRequest(
    string Name,
    string? AvatarUrl,
    string Timezone);

public sealed record UpdatePreferencesRequest(
    string Theme,
    bool NotificationsEnabled,
    TimeOnly BriefingTime);

public sealed record AuthTokenResponse(string AccessToken, string? RefreshToken = null);
