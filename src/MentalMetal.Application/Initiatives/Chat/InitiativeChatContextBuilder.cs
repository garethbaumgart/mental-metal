using MentalMetal.Domain.Captures;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.People;

namespace MentalMetal.Application.Initiatives.Chat;

public sealed class InitiativeChatContextBuilder(
    IInitiativeRepository initiatives,
    ICaptureRepository captures,
    ICommitmentRepository commitments,
    IDelegationRepository delegations,
    IPersonRepository people) : IInitiativeChatContextBuilder
{
    public const int DecisionCap = 20;
    public const int CommitmentCap = 50;
    public const int DelegationCap = 50;
    public const int CaptureCap = 30;
    public const int RecentlyCompletedWindowDays = 30;

    public async Task<InitiativeChatContextPayload?> BuildAsync(
        Guid userId,
        Guid initiativeId,
        string userQuestion,
        IReadOnlyList<ChatMessage> recentMessages,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        var initiative = await initiatives.GetByIdAsync(initiativeId, cancellationToken);
        if (initiative is null || initiative.UserId != userId)
            return null;

        var brief = initiative.Brief ?? Domain.Initiatives.LivingBrief.LivingBrief.Empty();

        var commitmentsList = await commitments.GetAllAsync(
            userId, directionFilter: null, statusFilter: null,
            personIdFilter: null, initiativeIdFilter: initiativeId,
            overdueFilter: null, cancellationToken);
        var delegationsList = await delegations.GetAllAsync(
            userId, statusFilter: null, priorityFilter: null,
            delegatePersonIdFilter: null, initiativeIdFilter: initiativeId,
            cancellationToken);
        var capturesList = await captures.GetConfirmedForInitiativeAsync(userId, initiativeId, CaptureCap, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var recentWindow = now.AddDays(-RecentlyCompletedWindowDays);

        var relevantCommitments = commitmentsList
            .Where(c => c.UserId == userId)
            .Where(c => c.Status == CommitmentStatus.Open
                || (c.CompletedAt is not null && c.CompletedAt >= recentWindow))
            .OrderByDescending(c => c.UpdatedAt)
            .Take(CommitmentCap)
            .ToList();

        var relevantDelegations = delegationsList
            .Where(d => d.UserId == userId)
            .Where(d => d.Status is DelegationStatus.Assigned or DelegationStatus.InProgress or DelegationStatus.Blocked
                || (d.CompletedAt is not null && d.CompletedAt >= recentWindow))
            .OrderByDescending(d => d.UpdatedAt)
            .Take(DelegationCap)
            .ToList();

        // Resolve person names for commitments + delegations in one pass.
        var personIds = relevantCommitments.Select(c => c.PersonId)
            .Concat(relevantDelegations.Select(d => d.DelegatePersonId))
            .Distinct()
            .ToList();

        // Batch person lookup to avoid N+1 queries. Repository already filters to userId.
        var personLookup = personIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await people.GetByIdsAsync(userId, personIds, cancellationToken))
                .ToDictionary(p => p.Id, p => p.Name);

        var recentDecisions = brief.KeyDecisions
            .OrderByDescending(d => d.LoggedAt)
            .Take(DecisionCap)
            .ToList();

        var openRisks = brief.Risks
            .Where(r => r.Status == RiskStatus.Open)
            .OrderByDescending(r => r.RaisedAt)
            .ToList();

        var latestReq = brief.RequirementsHistory
            .OrderByDescending(r => r.CapturedAt)
            .FirstOrDefault();
        var latestDesign = brief.DesignDirectionHistory
            .OrderByDescending(d => d.CapturedAt)
            .FirstOrDefault();

        var captureItems = capturesList
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CapturedAt)
            .Take(CaptureCap)
            .Select(c => new CaptureContextItem(c.Id, c.CapturedAt, c.AiExtraction?.Summary ?? string.Empty))
            .ToList();

        var payload = new InitiativeChatContextPayload(
            Initiative: new InitiativeMetadataContext(
                initiative.Id,
                initiative.Title,
                initiative.Status.ToString(),
                initiative.Milestones
                    .Select(m => new InitiativeMilestoneContext(m.Id, m.Title, m.TargetDate, m.IsCompleted, m.Description))
                    .ToList()),
            LivingBrief: new LivingBriefContext(
                brief.Summary,
                brief.BriefVersion,
                brief.SummaryLastRefreshedAt,
                recentDecisions.Select(d => d.Id).ToList(),
                recentDecisions
                    .Select(d => new LivingBriefDecisionContext(d.Id, d.Description, d.Rationale, d.LoggedAt))
                    .ToList(),
                openRisks.Select(r => r.Id).ToList(),
                openRisks
                    .Select(r => new LivingBriefRiskContext(r.Id, r.Description, r.Severity.ToString(), r.RaisedAt))
                    .ToList(),
                latestReq?.Id,
                latestReq?.Content,
                latestDesign?.Id,
                latestDesign?.Content),
            Commitments: relevantCommitments
                .Select(c => new CommitmentContextItem(
                    c.Id,
                    c.Description,
                    c.Direction.ToString(),
                    personLookup.TryGetValue(c.PersonId, out var n) ? n : null,
                    c.Status.ToString(),
                    c.DueDate))
                .ToList(),
            Delegations: relevantDelegations
                .Select(d => new DelegationContextItem(
                    d.Id,
                    d.Description,
                    personLookup.TryGetValue(d.DelegatePersonId, out var n) ? n : null,
                    d.Status.ToString(),
                    d.DueDate,
                    d.Status == DelegationStatus.Blocked ? d.Notes : null))
                .ToList(),
            LinkedCaptures: captureItems,
            ConversationHistory: recentMessages);

        return payload;
    }
}
