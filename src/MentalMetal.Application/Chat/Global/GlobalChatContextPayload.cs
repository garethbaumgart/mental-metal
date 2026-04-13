using MentalMetal.Domain.ChatThreads;

namespace MentalMetal.Application.Chat.Global;

/// <summary>
/// Bounded context payload assembled by <see cref="GlobalChatContextBuilder"/>. Items
/// are the union of the slices selected by the IntentSet, with hard caps applied per slice
/// before assembly. Always user-scoped.
/// </summary>
public sealed record GlobalChatContextPayload(
    IntentSet Intents,
    GlobalCounters Counters,
    IReadOnlyList<PersonContextItem> Persons,
    IReadOnlyList<InitiativeContextItem> Initiatives,
    IReadOnlyList<CommitmentContextItem> Commitments,
    IReadOnlyList<DelegationContextItem> Delegations,
    IReadOnlyList<CaptureContextItem> Captures,
    IReadOnlyList<string> TruncationNotes,
    IReadOnlyList<ChatMessage> ConversationHistory)
{
    /// <summary>
    /// Set of (EntityType, EntityId) pairs the assistant may legitimately cite. The
    /// completion service drops any SourceReference whose pair is not in this set.
    /// </summary>
    public HashSet<(SourceReferenceEntityType Type, Guid Id)> KnownCitations()
    {
        var set = new HashSet<(SourceReferenceEntityType, Guid)>();
        foreach (var p in Persons) set.Add((SourceReferenceEntityType.Person, p.Id));
        foreach (var i in Initiatives)
        {
            set.Add((SourceReferenceEntityType.Initiative, i.Id));
            foreach (var d in i.RecentDecisionIds) set.Add((SourceReferenceEntityType.LivingBriefDecision, d));
            foreach (var r in i.OpenRiskIds) set.Add((SourceReferenceEntityType.LivingBriefRisk, r));
        }
        foreach (var c in Commitments) set.Add((SourceReferenceEntityType.Commitment, c.Id));
        foreach (var d in Delegations) set.Add((SourceReferenceEntityType.Delegation, d.Id));
        foreach (var cap in Captures) set.Add((SourceReferenceEntityType.Capture, cap.Id));
        return set;
    }
}

public sealed record GlobalCounters(int OpenCommitments, int OpenDelegations, int ActiveInitiatives);

public sealed record PersonContextItem(
    Guid Id,
    string Name,
    string Type,
    string? Role,
    string? Team);

public sealed record InitiativeContextItem(
    Guid Id,
    string Title,
    string Status,
    string? BriefSummary,
    IReadOnlyList<Guid> RecentDecisionIds,
    IReadOnlyList<Guid> OpenRiskIds);

public sealed record CommitmentContextItem(
    Guid Id,
    string Description,
    string Direction,
    string? PersonName,
    string Status,
    DateOnly? DueDate,
    bool IsOverdue);

public sealed record DelegationContextItem(
    Guid Id,
    string Description,
    string? DelegateName,
    string Status,
    DateOnly? DueDate,
    bool IsOverdue,
    string? BlockedReason);

public sealed record CaptureContextItem(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Summary);
