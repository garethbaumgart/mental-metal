using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Nudges;

public sealed record NudgeCreated(Guid NudgeId, Guid UserId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record NudgeUpdated(Guid NudgeId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record NudgeCadenceChanged(Guid NudgeId, CadenceType NewType) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record NudgeNudged(Guid NudgeId, DateOnly NextDueDate) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record NudgePaused(Guid NudgeId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record NudgeResumed(Guid NudgeId, DateOnly NextDueDate) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record NudgeDeleted(Guid NudgeId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
