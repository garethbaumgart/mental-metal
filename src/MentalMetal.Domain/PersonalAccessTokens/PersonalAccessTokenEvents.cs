using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.PersonalAccessTokens;

public sealed record PersonalAccessTokenCreated(Guid TokenId, Guid UserId, string Name) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record PersonalAccessTokenRevoked(Guid TokenId, Guid UserId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
