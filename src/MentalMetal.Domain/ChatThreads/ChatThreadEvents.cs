using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.ChatThreads;

public sealed record ChatThreadStarted(Guid ThreadId, Guid UserId, ContextScopeType ScopeType, Guid? InitiativeId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record ChatMessageSent(Guid ThreadId, Guid UserId, int MessageOrdinal) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record ChatMessageReceived(Guid ThreadId, Guid UserId, int MessageOrdinal) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record ChatThreadRenamed(Guid ThreadId, Guid UserId, string Title, string Source) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record ChatThreadArchived(Guid ThreadId, Guid UserId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record ChatThreadUnarchived(Guid ThreadId, Guid UserId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
