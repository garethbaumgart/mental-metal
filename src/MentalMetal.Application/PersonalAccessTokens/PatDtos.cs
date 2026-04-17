namespace MentalMetal.Application.PersonalAccessTokens;

public sealed record CreatePatRequest(string Name, HashSet<string> Scopes);

public sealed record PatCreatedResponse(
    Guid Id,
    string Name,
    IReadOnlySet<string> Scopes,
    DateTimeOffset CreatedAt,
    string Token);

public sealed record PatSummaryResponse(
    Guid Id,
    string Name,
    IReadOnlySet<string> Scopes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt);
