using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Users;

public sealed record UserRegistered(Guid UserId, string Email) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record UserProfileUpdated(Guid UserId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record PreferencesUpdated(Guid UserId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record AiProviderConfigured(Guid UserId, AiProvider Provider) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record AiProviderRemoved(Guid UserId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record TranscriptionProviderConfigured(Guid UserId, TranscriptionProvider Provider) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record TranscriptionProviderRemoved(Guid UserId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
