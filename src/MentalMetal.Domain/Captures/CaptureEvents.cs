using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Captures;

public sealed record CaptureCreated(Guid CaptureId, Guid UserId, CaptureType Type) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureProcessingStarted(Guid CaptureId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureProcessed(Guid CaptureId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureProcessingFailed(Guid CaptureId, string? Reason) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureRetryRequested(Guid CaptureId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureLinkedToPerson(Guid CaptureId, Guid PersonId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureLinkedToInitiative(Guid CaptureId, Guid InitiativeId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureUnlinkedFromPerson(Guid CaptureId, Guid PersonId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureUnlinkedFromInitiative(Guid CaptureId, Guid InitiativeId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureMetadataUpdated(Guid CaptureId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureExtractionConfirmed(Guid CaptureId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureExtractionDiscarded(Guid CaptureId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
