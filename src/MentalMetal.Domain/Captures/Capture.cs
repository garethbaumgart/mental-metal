using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Captures;

public sealed class Capture : AggregateRoot, IUserScoped
{
    private readonly List<Guid> _linkedPersonIds = [];
    private readonly List<Guid> _linkedInitiativeIds = [];
    private readonly List<Guid> _spawnedCommitmentIds = [];
    private readonly List<Guid> _spawnedDelegationIds = [];
    private readonly List<Guid> _spawnedObservationIds = [];

    public Guid UserId { get; private set; }
    public string RawContent { get; private set; } = null!;
    public CaptureType CaptureType { get; private set; }
    public ProcessingStatus ProcessingStatus { get; private set; }
    public AiExtraction? AiExtraction { get; private set; }
    public ExtractionStatus ExtractionStatus { get; private set; }
    public string? FailureReason { get; private set; }
    public IReadOnlyList<Guid> LinkedPersonIds => _linkedPersonIds;
    public IReadOnlyList<Guid> LinkedInitiativeIds => _linkedInitiativeIds;
    public IReadOnlyList<Guid> SpawnedCommitmentIds => _spawnedCommitmentIds;
    public IReadOnlyList<Guid> SpawnedDelegationIds => _spawnedDelegationIds;
    public IReadOnlyList<Guid> SpawnedObservationIds => _spawnedObservationIds;
    public string? Title { get; private set; }
    public DateTimeOffset CapturedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? Source { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Capture() { } // EF Core

    public static Capture Create(Guid userId, string rawContent, CaptureType type, string? source = null, string? title = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawContent, nameof(rawContent));

        var now = DateTimeOffset.UtcNow;

        var capture = new Capture
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RawContent = rawContent,
            CaptureType = type,
            ProcessingStatus = ProcessingStatus.Raw,
            Title = title?.Trim(),
            Source = source?.Trim(),
            CapturedAt = now,
            UpdatedAt = now
        };

        capture.RaiseDomainEvent(new CaptureCreated(capture.Id, userId, type));

        return capture;
    }

    public void BeginProcessing()
    {
        if (ProcessingStatus != ProcessingStatus.Raw)
            throw new InvalidOperationException(
                $"Cannot begin processing from '{ProcessingStatus}' status. Must be 'Raw'.");

        ProcessingStatus = ProcessingStatus.Processing;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureProcessingStarted(Id));
    }

    public void CompleteProcessing(AiExtraction extraction)
    {
        ArgumentNullException.ThrowIfNull(extraction, nameof(extraction));

        if (ProcessingStatus != ProcessingStatus.Processing)
            throw new InvalidOperationException(
                $"Cannot complete processing from '{ProcessingStatus}' status. Must be 'Processing'.");

        ProcessingStatus = ProcessingStatus.Processed;
        AiExtraction = extraction;
        ExtractionStatus = ExtractionStatus.Pending;
        FailureReason = null;
        ProcessedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureProcessed(Id));
    }

    public void FailProcessing(string? reason = null)
    {
        if (ProcessingStatus != ProcessingStatus.Processing)
            throw new InvalidOperationException(
                $"Cannot fail processing from '{ProcessingStatus}' status. Must be 'Processing'.");

        ProcessingStatus = ProcessingStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureProcessingFailed(Id, reason));
    }

    public void RetryProcessing()
    {
        if (ProcessingStatus != ProcessingStatus.Failed)
            throw new InvalidOperationException(
                $"Cannot retry processing from '{ProcessingStatus}' status. Must be 'Failed'.");

        ProcessingStatus = ProcessingStatus.Raw;
        FailureReason = null;
        AiExtraction = null;
        ExtractionStatus = ExtractionStatus.None;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureRetryRequested(Id));
    }

    public void ConfirmExtraction()
    {
        if (ProcessingStatus != ProcessingStatus.Processed)
            throw new InvalidOperationException(
                $"Cannot confirm extraction from '{ProcessingStatus}' status. Must be 'Processed'.");

        if (AiExtraction is null)
            throw new InvalidOperationException("No extraction to confirm.");

        if (ExtractionStatus == ExtractionStatus.Confirmed)
            throw new InvalidOperationException("Extraction already confirmed.");

        ExtractionStatus = ExtractionStatus.Confirmed;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureExtractionConfirmed(Id));
    }

    public void DiscardExtraction()
    {
        if (ProcessingStatus != ProcessingStatus.Processed)
            throw new InvalidOperationException(
                $"Cannot discard extraction from '{ProcessingStatus}' status. Must be 'Processed'.");

        if (ExtractionStatus != ExtractionStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot discard extraction with status '{ExtractionStatus}'. Must be 'Pending'.");

        ExtractionStatus = ExtractionStatus.Discarded;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureExtractionDiscarded(Id));
    }

    public void LinkToPerson(Guid personId)
    {
        if (_linkedPersonIds.Contains(personId))
            return;

        _linkedPersonIds.Add(personId);
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureLinkedToPerson(Id, personId));
    }

    public void UnlinkFromPerson(Guid personId)
    {
        if (!_linkedPersonIds.Remove(personId))
            return; // idempotent

        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureUnlinkedFromPerson(Id, personId));
    }

    public void LinkToInitiative(Guid initiativeId)
    {
        if (_linkedInitiativeIds.Contains(initiativeId))
            return;

        _linkedInitiativeIds.Add(initiativeId);
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureLinkedToInitiative(Id, initiativeId));
    }

    public void UnlinkFromInitiative(Guid initiativeId)
    {
        if (!_linkedInitiativeIds.Remove(initiativeId))
            return; // idempotent

        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureUnlinkedFromInitiative(Id, initiativeId));
    }

    public void UpdateMetadata(string? title, string? source)
    {
        Title = title?.Trim();
        Source = source?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureMetadataUpdated(Id));
    }

    public void RecordSpawnedCommitment(Guid commitmentId)
    {
        if (_spawnedCommitmentIds.Contains(commitmentId))
            return;

        _spawnedCommitmentIds.Add(commitmentId);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordSpawnedDelegation(Guid delegationId)
    {
        if (_spawnedDelegationIds.Contains(delegationId))
            return;

        _spawnedDelegationIds.Add(delegationId);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordSpawnedObservation(Guid observationId)
    {
        if (_spawnedObservationIds.Contains(observationId))
            return;

        _spawnedObservationIds.Add(observationId);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
