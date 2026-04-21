using MentalMetal.Domain.Commitments;

namespace MentalMetal.Domain.Captures;

/// <summary>
/// V2 extraction value object — auto-applied, no confirmation step.
/// </summary>
public sealed record AiExtraction
{
    public required string Summary { get; init; }
    public IReadOnlyList<PersonMention> PeopleMentioned { get; init; } = new List<PersonMention>();
    public IReadOnlyList<ExtractedCommitment> Commitments { get; init; } = new List<ExtractedCommitment>();
    public IReadOnlyList<string> Decisions { get; init; } = new List<string>();
    public IReadOnlyList<string> Risks { get; init; } = new List<string>();
    public IReadOnlyList<InitiativeTag> InitiativeTags { get; init; } = new List<InitiativeTag>();
    public required DateTimeOffset ExtractedAt { get; init; }
    public CaptureType? DetectedCaptureType { get; init; }
}

public sealed record PersonMention
{
    public required string RawName { get; init; }
    public Guid? PersonId { get; init; }
    public string? Context { get; init; }
}

public sealed record ExtractedCommitment
{
    public required string Description { get; init; }
    public required CommitmentDirection Direction { get; init; }
    public Guid? PersonId { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public required CommitmentConfidence Confidence { get; init; }
    public int? SourceStartOffset { get; init; }
    public int? SourceEndOffset { get; init; }
    public Guid? SpawnedCommitmentId { get; init; }
}

public sealed record InitiativeTag
{
    public required string RawName { get; init; }
    public Guid? InitiativeId { get; init; }
    public string? Context { get; init; }
}
