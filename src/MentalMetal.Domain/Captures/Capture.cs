using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Captures;

public sealed class Capture : AggregateRoot, IUserScoped
{
    private readonly List<Guid> _linkedPersonIds = [];
    private readonly List<Guid> _linkedInitiativeIds = [];
    private readonly List<Guid> _spawnedCommitmentIds = [];
    private readonly List<TranscriptSegment> _transcriptSegments = [];

    public Guid UserId { get; private set; }
    public string RawContent { get; private set; } = null!;
    public CaptureType CaptureType { get; private set; }
    public CaptureSource? CaptureSource { get; private set; }
    public ProcessingStatus ProcessingStatus { get; private set; }
    public AiExtraction? AiExtraction { get; private set; }
    public string? FailureReason { get; private set; }

    // --- Audio / transcription fields (null / NotApplicable for non-audio captures) ---
    public string? AudioBlobRef { get; private set; }
    public string? AudioMimeType { get; private set; }
    public double? AudioDurationSeconds { get; private set; }
    public DateTimeOffset? AudioDiscardedAt { get; private set; }
    public TranscriptionStatus TranscriptionStatus { get; private set; } = TranscriptionStatus.NotApplicable;
    public string? TranscriptionFailureReason { get; private set; }
    public IReadOnlyList<TranscriptSegment> TranscriptSegments => _transcriptSegments;
    public IReadOnlyList<Guid> LinkedPersonIds => _linkedPersonIds;
    public IReadOnlyList<Guid> LinkedInitiativeIds => _linkedInitiativeIds;
    public IReadOnlyList<Guid> SpawnedCommitmentIds => _spawnedCommitmentIds;
    public string? Title { get; private set; }
    public DateTimeOffset CapturedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Capture() { } // EF Core

    public static Capture Create(Guid userId, string rawContent, CaptureType type, CaptureSource? source = null, string? title = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawContent, nameof(rawContent));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        var now = DateTimeOffset.UtcNow;

        var capture = new Capture
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RawContent = rawContent,
            CaptureType = type,
            CaptureSource = source,
            ProcessingStatus = ProcessingStatus.Raw,
            Title = title?.Trim(),
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
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureRetryRequested(Id));
    }

    public void LinkToPerson(Guid personId)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        if (_linkedPersonIds.Contains(personId))
            return;

        _linkedPersonIds.Add(personId);
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureLinkedToPerson(Id, personId));
    }

    public void LinkToInitiative(Guid initiativeId)
    {
        if (initiativeId == Guid.Empty)
            throw new ArgumentException("InitiativeId is required.", nameof(initiativeId));

        if (_linkedInitiativeIds.Contains(initiativeId))
            return;

        _linkedInitiativeIds.Add(initiativeId);
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureLinkedToInitiative(Id, initiativeId));
    }

    public void UpdateMetadata(string? title)
    {
        Title = title?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CaptureMetadataUpdated(Id));
    }

    public void RecordSpawnedCommitment(Guid commitmentId)
    {
        if (commitmentId == Guid.Empty)
            throw new ArgumentException("CommitmentId is required.", nameof(commitmentId));

        if (_spawnedCommitmentIds.Contains(commitmentId))
            return;

        _spawnedCommitmentIds.Add(commitmentId);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // -----------------------------------------------------------------------
    // Audio capture / transcription lifecycle
    // -----------------------------------------------------------------------

    public static Capture CreateAudio(
        Guid userId,
        string audioBlobRef,
        string audioMimeType,
        double audioDurationSeconds,
        DateTimeOffset now,
        CaptureSource? source = null,
        string? title = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audioBlobRef, nameof(audioBlobRef));
        ArgumentException.ThrowIfNullOrWhiteSpace(audioMimeType, nameof(audioMimeType));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (audioDurationSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(audioDurationSeconds), "Duration must be non-negative.");

        var capture = new Capture
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RawContent = string.Empty, // populated after transcription
            CaptureType = CaptureType.AudioRecording,
            CaptureSource = source,
            ProcessingStatus = ProcessingStatus.Raw,
            TranscriptionStatus = TranscriptionStatus.Pending,
            AudioBlobRef = audioBlobRef,
            AudioMimeType = audioMimeType,
            AudioDurationSeconds = audioDurationSeconds,
            Title = title?.Trim(),
            CapturedAt = now,
            UpdatedAt = now
        };

        capture.RaiseDomainEvent(new CaptureCreated(capture.Id, userId, CaptureType.AudioRecording));
        capture.RaiseDomainEvent(new CaptureAudioUploaded(capture.Id, audioBlobRef, audioMimeType, audioDurationSeconds));

        return capture;
    }

    public void BeginTranscription(DateTimeOffset now)
    {
        if (TranscriptionStatus != TranscriptionStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot begin transcription from '{TranscriptionStatus}'. Must be 'Pending'.");

        TranscriptionStatus = TranscriptionStatus.InProgress;
        UpdatedAt = now;
    }

    public void AttachTranscript(
        string fullTranscriptText,
        IEnumerable<TranscriptSegment> segments,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(fullTranscriptText, nameof(fullTranscriptText));
        ArgumentNullException.ThrowIfNull(segments, nameof(segments));

        if (TranscriptionStatus != TranscriptionStatus.InProgress)
            throw new InvalidOperationException(
                $"Cannot attach transcript from '{TranscriptionStatus}'. Must be 'InProgress'.");

        _transcriptSegments.Clear();
        foreach (var segment in segments)
            _transcriptSegments.Add(segment);

        RawContent = fullTranscriptText;
        TranscriptionStatus = TranscriptionStatus.Transcribed;
        TranscriptionFailureReason = null;
        UpdatedAt = now;

        RaiseDomainEvent(new CaptureTranscribed(Id, _transcriptSegments.Count));
    }

    public void MarkTranscriptionFailed(string? reason, DateTimeOffset now)
    {
        if (TranscriptionStatus is not (TranscriptionStatus.InProgress or TranscriptionStatus.Failed))
            throw new InvalidOperationException(
                $"Cannot mark transcription failed from '{TranscriptionStatus}'. Must be 'InProgress' or 'Failed'.");

        TranscriptionStatus = TranscriptionStatus.Failed;
        TranscriptionFailureReason = reason;
        UpdatedAt = now;

        RaiseDomainEvent(new CaptureTranscriptionFailed(Id, reason));
    }

    public void RequeueTranscription(DateTimeOffset now)
    {
        if (TranscriptionStatus != TranscriptionStatus.Failed)
            throw new InvalidOperationException(
                $"Cannot requeue transcription from '{TranscriptionStatus}'. Must be 'Failed'.");
        if (string.IsNullOrWhiteSpace(AudioBlobRef))
            throw new InvalidOperationException(
                "Cannot retry transcription: audio blob has been discarded.");

        TranscriptionStatus = TranscriptionStatus.Pending;
        TranscriptionFailureReason = null;
        UpdatedAt = now;
    }

    public void MarkAudioDiscarded(DateTimeOffset now)
    {
        if (AudioDiscardedAt is not null)
            return; // idempotent

        AudioBlobRef = null;
        AudioDiscardedAt = now;
        UpdatedAt = now;

        RaiseDomainEvent(new CaptureAudioDiscarded(Id));
    }

    public void IdentifySpeakers(IReadOnlyDictionary<string, Guid> mapping, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(mapping, nameof(mapping));

        if (mapping.Count == 0)
            return;

        var presentLabels = _transcriptSegments.Select(s => s.SpeakerLabel).ToHashSet(StringComparer.Ordinal);
        foreach (var label in mapping.Keys)
        {
            if (!presentLabels.Contains(label))
                throw new KeyNotFoundException($"Speaker label not found on transcript: {label}");
        }

        foreach (var segment in _transcriptSegments)
        {
            if (mapping.TryGetValue(segment.SpeakerLabel, out var personId))
                segment.LinkToPerson(personId);
        }

        UpdatedAt = now;

        foreach (var kvp in mapping)
            RaiseDomainEvent(new CaptureSpeakerIdentified(Id, kvp.Key, kvp.Value));
    }
}
