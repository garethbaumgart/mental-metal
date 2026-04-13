using MentalMetal.Domain.ChatThreads;

namespace MentalMetal.Application.Initiatives.Chat;

/// <summary>
/// Structured context payload assembled for initiative-scoped chat completions.
/// Everything here is filtered by the user that owns the initiative — the builder
/// re-filters even though EF query filters exist, as defence in depth.
/// </summary>
public sealed record InitiativeChatContextPayload(
    InitiativeMetadataContext Initiative,
    LivingBriefContext LivingBrief,
    IReadOnlyList<CommitmentContextItem> Commitments,
    IReadOnlyList<DelegationContextItem> Delegations,
    IReadOnlyList<CaptureContextItem> LinkedCaptures,
    IReadOnlyList<ChatMessage> ConversationHistory)
{
    /// <summary>
    /// Every EntityId from every item assembled into context, keyed by type — used by the
    /// completion service to drop hallucinated SourceReference citations before persisting.
    /// </summary>
    public HashSet<(SourceReferenceEntityType Type, Guid Id)> KnownCitations()
    {
        var set = new HashSet<(SourceReferenceEntityType, Guid)>
        {
            (SourceReferenceEntityType.Initiative, Initiative.Id),
        };

        foreach (var d in LivingBrief.RecentDecisionIds) set.Add((SourceReferenceEntityType.LivingBriefDecision, d));
        foreach (var r in LivingBrief.OpenRiskIds) set.Add((SourceReferenceEntityType.LivingBriefRisk, r));
        if (LivingBrief.LatestRequirementsId is { } reqId)
            set.Add((SourceReferenceEntityType.LivingBriefRequirements, reqId));
        if (LivingBrief.LatestDesignDirectionId is { } ddId)
            set.Add((SourceReferenceEntityType.LivingBriefDesignDirection, ddId));

        foreach (var c in Commitments) set.Add((SourceReferenceEntityType.Commitment, c.Id));
        foreach (var d in Delegations) set.Add((SourceReferenceEntityType.Delegation, d.Id));
        foreach (var cap in LinkedCaptures) set.Add((SourceReferenceEntityType.Capture, cap.Id));

        return set;
    }
}

public sealed record InitiativeMetadataContext(
    Guid Id,
    string Title,
    string Status,
    IReadOnlyList<InitiativeMilestoneContext> Milestones);

public sealed record InitiativeMilestoneContext(
    Guid Id,
    string Title,
    DateOnly TargetDate,
    bool IsCompleted,
    string? Description);

public sealed record LivingBriefContext(
    string Summary,
    int BriefVersion,
    DateTimeOffset? SummaryLastRefreshedAt,
    IReadOnlyList<Guid> RecentDecisionIds,
    IReadOnlyList<LivingBriefDecisionContext> RecentDecisions,
    IReadOnlyList<Guid> OpenRiskIds,
    IReadOnlyList<LivingBriefRiskContext> OpenRisks,
    Guid? LatestRequirementsId,
    string? LatestRequirementsContent,
    Guid? LatestDesignDirectionId,
    string? LatestDesignDirectionContent);

public sealed record LivingBriefDecisionContext(Guid Id, string Description, string? Rationale, DateTimeOffset LoggedAt);
public sealed record LivingBriefRiskContext(Guid Id, string Description, string Severity, DateTimeOffset RaisedAt);

public sealed record CommitmentContextItem(
    Guid Id,
    string Description,
    string Direction,
    string? PersonName,
    string Status,
    DateOnly? DueDate);

public sealed record DelegationContextItem(
    Guid Id,
    string Description,
    string? DelegateName,
    string Status,
    DateOnly? DueDate,
    string? BlockedReason);

public sealed record CaptureContextItem(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Summary);
