namespace MentalMetal.Domain.Captures;

public sealed record AiExtraction
{
    public required string Summary { get; init; }
    public IReadOnlyList<ExtractedCommitment> Commitments { get; init; } = [];
    public IReadOnlyList<ExtractedDelegation> Delegations { get; init; } = [];
    public IReadOnlyList<ExtractedObservation> Observations { get; init; } = [];
    public IReadOnlyList<string> Decisions { get; init; } = [];
    public IReadOnlyList<string> RisksIdentified { get; init; } = [];
    public IReadOnlyList<string> SuggestedPersonLinks { get; init; } = [];
    public IReadOnlyList<string> SuggestedInitiativeLinks { get; init; } = [];
    public decimal ConfidenceScore { get; init; }
}

public sealed record ExtractedCommitment
{
    public required string Description { get; init; }
    public required string Direction { get; init; } // "MineToThem" or "TheirsToMe"
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
