using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.OneOnOnes;

public sealed record OneOnOneCreated(Guid OneOnOneId, Guid UserId, Guid PersonId, DateOnly OccurredOn) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record OneOnOneUpdated(Guid OneOnOneId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record ActionItemAdded(Guid OneOnOneId, Guid ActionItemId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record ActionItemCompleted(Guid OneOnOneId, Guid ActionItemId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record ActionItemRemoved(Guid OneOnOneId, Guid ActionItemId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record FollowUpAdded(Guid OneOnOneId, Guid FollowUpId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record FollowUpResolved(Guid OneOnOneId, Guid FollowUpId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
