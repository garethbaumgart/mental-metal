using System.Collections.Immutable;

namespace MentalMetal.Domain.Initiatives.LivingBrief;

// Immutable collections prevent callers from retaining the original List<T>/List<Guid> and
// mutating proposal contents after creation, which would bypass the pending-update state machine.
public sealed record BriefUpdateProposal
{
    public string? ProposedSummary { get; init; }
    public ImmutableArray<ProposedDecision> NewDecisions { get; init; } = [];
    public ImmutableArray<ProposedRisk> NewRisks { get; init; } = [];
    public ImmutableArray<Guid> RisksToResolve { get; init; } = [];
    public string? ProposedRequirementsContent { get; init; }
    public string? ProposedDesignDirectionContent { get; init; }
    public ImmutableArray<Guid> SourceCaptureIds { get; init; } = [];
    public decimal? AiConfidence { get; init; }
    public string? Rationale { get; init; }
    public int SchemaVersion { get; init; } = 1;
}

public sealed record ProposedDecision
{
    public required string Description { get; init; }
    public string? Rationale { get; init; }
    public ImmutableArray<Guid> SourceCaptureIds { get; init; } = [];
}

public sealed record ProposedRisk
{
    public required string Description { get; init; }
    public required RiskSeverity Severity { get; init; }
    public ImmutableArray<Guid> SourceCaptureIds { get; init; } = [];
}
