using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Goals;

public sealed record GoalCreated(Guid GoalId, Guid UserId, Guid PersonId, GoalType Type) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record GoalUpdated(Guid GoalId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record GoalAchieved(Guid GoalId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record GoalMissed(Guid GoalId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record GoalDeferred(Guid GoalId, string? Reason) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record GoalReactivated(Guid GoalId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record GoalCheckInRecorded(Guid GoalId, Guid CheckInId, int? Progress) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
