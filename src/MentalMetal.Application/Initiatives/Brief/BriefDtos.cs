using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;

namespace MentalMetal.Application.Initiatives.Brief;

public sealed record KeyDecisionDto(
    Guid Id,
    string Description,
    string? Rationale,
    string Source,
    IReadOnlyList<Guid> SourceCaptureIds,
    DateTimeOffset LoggedAt);

public sealed record RiskDto(
    Guid Id,
    string Description,
    string Severity,
    string Status,
    string Source,
    IReadOnlyList<Guid> SourceCaptureIds,
    DateTimeOffset RaisedAt,
    DateTimeOffset? ResolvedAt,
    string? ResolutionNote);

public sealed record SnapshotDto(
    Guid Id,
    string Content,
    string Source,
    IReadOnlyList<Guid> SourceCaptureIds,
    DateTimeOffset CapturedAt);

public sealed record LivingBriefDto(
    Guid InitiativeId,
    string Summary,
    DateTimeOffset? SummaryLastRefreshedAt,
    int BriefVersion,
    string SummarySource,
    IReadOnlyList<Guid> SummarySourceCaptureIds,
    IReadOnlyList<KeyDecisionDto> KeyDecisions,
    IReadOnlyList<RiskDto> Risks,
    IReadOnlyList<SnapshotDto> RequirementsHistory,
    IReadOnlyList<SnapshotDto> DesignDirectionHistory)
{
    public static LivingBriefDto From(Initiative initiative)
    {
        var b = initiative.Brief ?? Domain.Initiatives.LivingBrief.LivingBrief.Empty();
        return new LivingBriefDto(
            initiative.Id,
            b.Summary,
            b.SummaryLastRefreshedAt,
            b.BriefVersion,
            b.SummarySource.ToString(),
            b.SummarySourceCaptureIds.ToList(),
            b.KeyDecisions.Select(d => new KeyDecisionDto(d.Id, d.Description, d.Rationale, d.Source.ToString(), d.SourceCaptureIds, d.LoggedAt)).ToList(),
            b.Risks.Select(r => new RiskDto(r.Id, r.Description, r.Severity.ToString(), r.Status.ToString(), r.Source.ToString(), r.SourceCaptureIds, r.RaisedAt, r.ResolvedAt, r.ResolutionNote)).ToList(),
            b.RequirementsHistory.Select(s => new SnapshotDto(s.Id, s.Content, s.Source.ToString(), s.SourceCaptureIds, s.CapturedAt)).ToList(),
            b.DesignDirectionHistory.Select(s => new SnapshotDto(s.Id, s.Content, s.Source.ToString(), s.SourceCaptureIds, s.CapturedAt)).ToList());
    }
}

public sealed record UpdateSummaryRequest(string Summary);
public sealed record LogDecisionRequest(string Description, string? Rationale);
public sealed record RaiseRiskRequest(string Description, string Severity);
public sealed record ResolveRiskRequest(string? ResolutionNote);
public sealed record SnapshotRequest(string Content);

public sealed record ProposedDecisionDto(string Description, string? Rationale, IReadOnlyList<Guid> SourceCaptureIds);
public sealed record ProposedRiskDto(string Description, string Severity, IReadOnlyList<Guid> SourceCaptureIds);

public sealed record BriefUpdateProposalDto(
    string? ProposedSummary,
    IReadOnlyList<ProposedDecisionDto> NewDecisions,
    IReadOnlyList<ProposedRiskDto> NewRisks,
    IReadOnlyList<Guid> RisksToResolve,
    string? ProposedRequirementsContent,
    string? ProposedDesignDirectionContent,
    IReadOnlyList<Guid> SourceCaptureIds,
    decimal? AiConfidence,
    string? Rationale);

public sealed record PendingBriefUpdateDto(
    Guid Id,
    Guid InitiativeId,
    string Status,
    int BriefVersionAtProposal,
    int CurrentInitiativeBriefVersion,
    bool IsStale,
    string? FailureReason,
    BriefUpdateProposalDto Proposal,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static PendingBriefUpdateDto From(PendingBriefUpdate p, int currentBriefVersion) => new(
        p.Id, p.InitiativeId, p.Status.ToString(), p.BriefVersionAtProposal,
        currentBriefVersion, currentBriefVersion > p.BriefVersionAtProposal,
        p.FailureReason,
        new BriefUpdateProposalDto(
            p.Proposal.ProposedSummary,
            p.Proposal.NewDecisions.Select(d => new ProposedDecisionDto(d.Description, d.Rationale, d.SourceCaptureIds)).ToList(),
            p.Proposal.NewRisks.Select(r => new ProposedRiskDto(r.Description, r.Severity.ToString(), r.SourceCaptureIds)).ToList(),
            p.Proposal.RisksToResolve.ToList(),
            p.Proposal.ProposedRequirementsContent,
            p.Proposal.ProposedDesignDirectionContent,
            p.Proposal.SourceCaptureIds.ToList(),
            p.Proposal.AiConfidence,
            p.Proposal.Rationale),
        p.CreatedAt, p.UpdatedAt);
}

public sealed record EditPendingUpdateRequest(
    string? ProposedSummary,
    List<ProposedDecisionDto>? NewDecisions,
    List<ProposedRiskDto>? NewRisks,
    List<Guid>? RisksToResolve,
    string? ProposedRequirementsContent,
    string? ProposedDesignDirectionContent,
    string? Rationale);

public sealed record RejectPendingUpdateRequest(string? Reason);
