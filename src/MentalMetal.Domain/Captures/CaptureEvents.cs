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

public sealed record CaptureMetadataUpdated(Guid CaptureId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureAudioUploaded(
    Guid CaptureId,
    string AudioBlobRef,
    string AudioMimeType,
    double AudioDurationSeconds) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureTranscribed(Guid CaptureId, int SegmentCount) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureTranscriptionFailed(Guid CaptureId, string? Reason) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureAudioDiscarded(Guid CaptureId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record CaptureSpeakerIdentified(Guid CaptureId, string SpeakerLabel, Guid PersonId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
