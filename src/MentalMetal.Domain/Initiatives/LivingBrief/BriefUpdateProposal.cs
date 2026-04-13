namespace MentalMetal.Domain.Initiatives.LivingBrief;

public sealed record BriefUpdateProposal
{
    public string? ProposedSummary { get; init; }
    public IReadOnlyList<ProposedDecision> NewDecisions { get; init; } = [];
    public IReadOnlyList<ProposedRisk> NewRisks { get; init; } = [];
    public IReadOnlyList<Guid> RisksToResolve { get; init; } = [];
    public string? ProposedRequirementsContent { get; init; }
    public string? ProposedDesignDirectionContent { get; init; }
    public IReadOnlyList<Guid> SourceCaptureIds { get; init; } = [];
    public decimal? AiConfidence { get; init; }
    public string? Rationale { get; init; }
    public int SchemaVersion { get; init; } = 1;
}

public sealed record ProposedDecision
{
    public required string Description { get; init; }
    public string? Rationale { get; init; }
    public IReadOnlyList<Guid> SourceCaptureIds { get; init; } = [];
}

public sealed record ProposedRisk
{
    public required string Description { get; init; }
    public required RiskSeverity Severity { get; init; }
    public IReadOnlyList<Guid> SourceCaptureIds { get; init; } = [];
}
