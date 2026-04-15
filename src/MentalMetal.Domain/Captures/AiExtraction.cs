namespace MentalMetal.Domain.Captures;

public sealed record AiExtraction
{
    public required string Summary { get; init; }
    public IReadOnlyList<ExtractedCommitment> Commitments { get; init; } = new List<ExtractedCommitment>();
    public IReadOnlyList<ExtractedDelegation> Delegations { get; init; } = new List<ExtractedDelegation>();
    public IReadOnlyList<ExtractedObservation> Observations { get; init; } = new List<ExtractedObservation>();
    public IReadOnlyList<string> Decisions { get; init; } = new List<string>();
    public IReadOnlyList<string> RisksIdentified { get; init; } = new List<string>();
    public IReadOnlyList<string> SuggestedPersonLinks { get; init; } = new List<string>();
    public IReadOnlyList<string> SuggestedInitiativeLinks { get; init; } = new List<string>();
    public decimal ConfidenceScore { get; init; }
}

public sealed record ExtractedCommitment
{
    public required string Description { get; init; }
    public required ExtractionDirection Direction { get; init; }
    public string? PersonHint { get; init; }
    public string? DueDate { get; init; }
}

public sealed record ExtractedDelegation
{
    public required string Description { get; init; }
    public string? PersonHint { get; init; }
    public string? DueDate { get; init; }
}

public sealed record ExtractedObservation
{
    public required string Description { get; init; }
    public string? PersonHint { get; init; }
    public string? Tag { get; init; }
}

public enum ExtractionDirection
{
    MineToThem,
    TheirsToMe
}
