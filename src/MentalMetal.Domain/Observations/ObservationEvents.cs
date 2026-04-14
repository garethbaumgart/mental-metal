using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Observations;

public sealed record ObservationCreated(Guid ObservationId, Guid UserId, Guid PersonId, ObservationTag Tag) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record ObservationUpdated(Guid ObservationId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record ObservationDeleted(Guid ObservationId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
