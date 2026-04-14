using MentalMetal.Application.MyQueue.Contracts;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Options;

namespace MentalMetal.Application.MyQueue;

public sealed record GetMyQueueQuery(
    QueueScope Scope,
    IReadOnlyList<QueueItemType> ItemTypes,
    Guid? PersonId,
    Guid? InitiativeId);

public sealed class GetMyQueueHandler(
    ICommitmentRepository commitmentRepository,
    IDelegationRepository delegationRepository,
    ICaptureRepository captureRepository,
    IPersonRepository personRepository,
    IInitiativeRepository initiativeRepository,
    ICurrentUserService currentUserService,
    QueuePrioritizationService scoring,
    IOptions<MyQueueOptions> options,
    TimeProvider timeProvider)
{
    public async Task<MyQueueResponse> HandleAsync(
        GetMyQueueQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var userId = currentUserService.UserId;
        var opts = options.Value;
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        // Normalise the item-type filter: absent / empty means "all three".
        var itemTypes = query.ItemTypes is { Count: > 0 }
            ? query.ItemTypes.Distinct().ToList()
            : new List<QueueItemType> { QueueItemType.Commitment, QueueItemType.Delegation, QueueItemType.Capture };

        // Preload, per-type, candidate rows that qualify for the base queue. Each call is
        // user-scoped at the DB level via its repository.
        var commitments = itemTypes.Contains(QueueItemType.Commitment)
            ? await LoadQualifyingCommitmentsAsync(userId, opts, today, query, cancellationToken)
            : new List<Commitment>();

        var delegations = itemTypes.Contains(QueueItemType.Delegation)
            ? await LoadQualifyingDelegationsAsync(userId, opts, today, now, query, cancellationToken)
            : new List<Delegation>();

        var captures = itemTypes.Contains(QueueItemType.Capture)
            ? await LoadQualifyingCapturesAsync(userId, opts, now, query, cancellationToken)
            : new List<Capture>();

        // Load the set of PersonIds the user has a non-cancelled delegation with,
        // used for the transient "suggestDelegate" hint. Compute in-memory so we avoid
        // the EF HashSet.Contains translation pitfall.
        var activeDelegationPersonIds = await LoadActiveDelegationPersonIdsAsync(
            userId, cancellationToken);

        // Batch-resolve display names: one round-trip for people, one for initiatives.
        var personIdsToResolve = new List<Guid>();
        var initiativeIdsToResolve = new List<Guid>();
        foreach (var c in commitments)
        {
            personIdsToResolve.Add(c.PersonId);
            if (c.InitiativeId is { } iid) initiativeIdsToResolve.Add(iid);
        }
        foreach (var d in delegations)
        {
            personIdsToResolve.Add(d.DelegatePersonId);
            if (d.InitiativeId is { } iid) initiativeIdsToResolve.Add(iid);
        }
        foreach (var c in captures)
        {
            if (c.LinkedPersonIds.Count > 0) personIdsToResolve.Add(c.LinkedPersonIds[0]);
            if (c.LinkedInitiativeIds.Count > 0) initiativeIdsToResolve.Add(c.LinkedInitiativeIds[0]);
        }

        var people = await personRepository.GetByIdsAsync(userId, personIdsToResolve, cancellationToken);
        var personNames = people.ToDictionary(p => p.Id, p => p.Name);
        var initiatives = await initiativeRepository.GetByIdsAsync(userId, initiativeIdsToResolve, cancellationToken);
        var initiativeNames = initiatives.ToDictionary(i => i.Id, i => i.Title);

        // Project each source type to the queue DTO, computing score + suggestDelegate inline.
        var items = new List<QueueItemResponse>(commitments.Count + delegations.Count + captures.Count);

        foreach (var c in commitments)
        {
            var score = scoring.ScoreCommitment(c, now, opts);
            var suggest =
                c.Status == CommitmentStatus.Open
                && c.Direction == CommitmentDirection.MineToThem
                && activeDelegationPersonIds.Contains(c.PersonId);

            items.Add(new QueueItemResponse(
                ItemType: QueueItemType.Commitment,
                Id: c.Id,
                Title: c.Description,
                Status: c.Status.ToString(),
                DueDate: c.DueDate,
                IsOverdue: c.IsOverdue,
                PersonId: c.PersonId,
                PersonName: personNames.TryGetValue(c.PersonId, out var pn) ? pn : null,
                InitiativeId: c.InitiativeId,
                InitiativeName: c.InitiativeId is { } iid && initiativeNames.TryGetValue(iid, out var iName) ? iName : null,
                DaysSinceCaptured: null,
                LastFollowedUpAt: null,
                PriorityScore: score,
                SuggestDelegate: suggest));
        }

        foreach (var d in delegations)
        {
            var score = scoring.ScoreDelegation(d, now, opts);
            var isOverdue = QueuePrioritizationService.IsDelegationOverdue(d, today);

            items.Add(new QueueItemResponse(
                ItemType: QueueItemType.Delegation,
                Id: d.Id,
                Title: d.Description,
                Status: d.Status.ToString(),
                DueDate: d.DueDate,
                IsOverdue: isOverdue,
                PersonId: d.DelegatePersonId,
                PersonName: personNames.TryGetValue(d.DelegatePersonId, out var pn) ? pn : null,
                InitiativeId: d.InitiativeId,
                InitiativeName: d.InitiativeId is { } iid && initiativeNames.TryGetValue(iid, out var iName) ? iName : null,
                DaysSinceCaptured: null,
                LastFollowedUpAt: d.LastFollowedUpAt,
                PriorityScore: score,
                SuggestDelegate: false));
        }

        foreach (var c in captures)
        {
            var score = scoring.ScoreCapture(c, now, opts);
            var daysSince = (int)Math.Floor((now - c.CapturedAt).TotalDays);
            var firstPerson = c.LinkedPersonIds.Count > 0 ? c.LinkedPersonIds[0] : (Guid?)null;
            var firstInitiative = c.LinkedInitiativeIds.Count > 0 ? c.LinkedInitiativeIds[0] : (Guid?)null;

            items.Add(new QueueItemResponse(
                ItemType: QueueItemType.Capture,
                Id: c.Id,
                Title: string.IsNullOrWhiteSpace(c.Title) ? c.RawContent : c.Title,
                Status: c.ProcessingStatus.ToString(),
                DueDate: null,
                IsOverdue: false,
                PersonId: firstPerson,
                PersonName: firstPerson is { } pid && personNames.TryGetValue(pid, out var pn) ? pn : null,
                InitiativeId: firstInitiative,
                InitiativeName: firstInitiative is { } iid && initiativeNames.TryGetValue(iid, out var iName) ? iName : null,
                DaysSinceCaptured: daysSince,
                LastFollowedUpAt: null,
                PriorityScore: score,
                SuggestDelegate: false));
        }

        // Apply scope filter after projection (so we have IsOverdue/DueDate in hand).
        items = ApplyScopeFilter(items, query.Scope, today, opts);

        // Order deterministically.
        items = items
            .OrderByDescending(i => i.PriorityScore)
            .ThenBy(i => i.DueDate ?? DateOnly.MaxValue)
            .ThenByDescending(i => i.DaysSinceCaptured ?? int.MinValue)
            .ThenBy(i => i.Id)
            .ToList();

        // Compute counts. staleDelegations needs access to the raw delegation timestamps,
        // so it's computed here against the filtered delegation-id set.
        var retainedDelegationIds = items
            .Where(i => i.ItemType == QueueItemType.Delegation)
            .Select(i => i.Id)
            .ToHashSet();
        var staleDelegations = delegations
            .Where(d => retainedDelegationIds.Contains(d.Id))
            .Count(d =>
            {
                var lastTouch = d.LastFollowedUpAt ?? d.CreatedAt;
                var days = (int)Math.Floor((now - lastTouch).TotalDays);
                return days >= opts.DelegationStalenessDays;
            });

        var counts = ComputeCounts(items, opts, today, staleDelegations);

        var filters = new QueueFiltersResponse(
            Scope: query.Scope,
            ItemType: itemTypes,
            PersonId: query.PersonId,
            InitiativeId: query.InitiativeId);

        return new MyQueueResponse(items, counts, filters);
    }

    private async Task<List<Commitment>> LoadQualifyingCommitmentsAsync(
        Guid userId,
        MyQueueOptions opts,
        DateOnly today,
        GetMyQueueQuery query,
        CancellationToken cancellationToken)
    {
        // Repo supports status + person + initiative filters at the DB level.
        var rows = await commitmentRepository.GetAllAsync(
            userId,
            directionFilter: null,
            statusFilter: CommitmentStatus.Open,
            personIdFilter: query.PersonId,
            initiativeIdFilter: query.InitiativeId,
            overdueFilter: null,
            cancellationToken);

        // Qualification per spec: Open AND (Overdue OR DueDate null OR DueDate within window).
        var windowEnd = today.AddDays(opts.CommitmentDueSoonDays);
        return rows
            .Where(c =>
                c.Status == CommitmentStatus.Open
                && (c.IsOverdue || c.DueDate is null || c.DueDate <= windowEnd))
            .Take(opts.CandidateFetchLimit)
            .ToList();
    }

    private async Task<List<Delegation>> LoadQualifyingDelegationsAsync(
        Guid userId,
        MyQueueOptions opts,
        DateOnly today,
        DateTimeOffset now,
        GetMyQueueQuery query,
        CancellationToken cancellationToken)
    {
        var rows = await delegationRepository.GetAllAsync(
            userId,
            statusFilter: null,
            priorityFilter: null,
            delegatePersonIdFilter: query.PersonId,
            initiativeIdFilter: query.InitiativeId,
            cancellationToken);

        // Qualification: status in {Assigned, InProgress, Blocked} AND
        //   (overdue OR stale by DelegationStalenessDays OR priority in {High, Urgent}).
        var qualifying = new List<Delegation>();
        foreach (var d in rows)
        {
            if (d.Status == DelegationStatus.Completed) continue;

            var isOverdue = QueuePrioritizationService.IsDelegationOverdue(d, today);
            var lastTouch = d.LastFollowedUpAt ?? d.CreatedAt;
            var daysSinceTouch = (int)Math.Floor((now - lastTouch).TotalDays);
            var isStale = daysSinceTouch >= opts.DelegationStalenessDays;
            var isHighPriority = d.Priority == Priority.High || d.Priority == Priority.Urgent;

            if (isOverdue || isStale || isHighPriority)
            {
                qualifying.Add(d);
                if (qualifying.Count >= opts.CandidateFetchLimit) break;
            }
        }

        return qualifying;
    }

    private async Task<List<Capture>> LoadQualifyingCapturesAsync(
        Guid userId,
        MyQueueOptions opts,
        DateTimeOffset now,
        GetMyQueueQuery query,
        CancellationToken cancellationToken)
    {
        // Use the close-out queue projection: not triaged + unresolved processing.
        var rows = await captureRepository.GetCloseOutQueueAsync(userId, cancellationToken);

        var qualifying = new List<Capture>();
        foreach (var c in rows)
        {
            var daysSince = (int)Math.Floor((now - c.CapturedAt).TotalDays);
            if (daysSince < opts.CaptureStalenessDays) continue;

            if (query.PersonId is { } pid && !c.LinkedPersonIds.Contains(pid)) continue;
            if (query.InitiativeId is { } iid && !c.LinkedInitiativeIds.Contains(iid)) continue;

            qualifying.Add(c);
            if (qualifying.Count >= opts.CandidateFetchLimit) break;
        }

        return qualifying;
    }

    private async Task<HashSet<Guid>> LoadActiveDelegationPersonIdsAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        // We want "any non-cancelled delegation ever" per design/D3. Our status enum has no
        // Cancelled value — the only non-active status is Completed. Interpret per spec
        // scenario text: Completed delegations still count as an "established relationship".
        var rows = await delegationRepository.GetAllAsync(
            userId,
            statusFilter: null,
            priorityFilter: null,
            delegatePersonIdFilter: null,
            initiativeIdFilter: null,
            cancellationToken);

        // Purely in-memory HashSet — never used in an EF predicate, so no translation concern.
        return rows.Select(d => d.DelegatePersonId).ToHashSet();
    }

    private static List<QueueItemResponse> ApplyScopeFilter(
        List<QueueItemResponse> items, QueueScope scope, DateOnly today, MyQueueOptions opts)
    {
        return scope switch
        {
            QueueScope.All => items,
            QueueScope.Overdue => items
                .Where(i => i.ItemType == QueueItemType.Capture || i.IsOverdue)
                .ToList(),
            QueueScope.Today => items
                .Where(i => i.ItemType == QueueItemType.Capture
                    || i.IsOverdue
                    || (i.DueDate is { } due && due == today))
                .ToList(),
            QueueScope.ThisWeek => items
                .Where(i => i.ItemType == QueueItemType.Capture
                    || i.IsOverdue
                    || (i.DueDate is { } due && due <= today.AddDays(7)))
                .ToList(),
            _ => items,
        };
    }

    private static QueueCountsResponse ComputeCounts(
        IReadOnlyList<QueueItemResponse> items,
        MyQueueOptions opts,
        DateOnly today,
        int staleDelegations)
    {
        var overdue = items.Count(i => i.IsOverdue);
        var dueSoon = items.Count(i =>
            !i.IsOverdue
            && i.ItemType != QueueItemType.Capture
            && i.DueDate is { } due
            && due.DayNumber - today.DayNumber >= 0
            && due.DayNumber - today.DayNumber <= opts.CommitmentDueSoonDays);
        var staleCaptures = items.Count(i => i.ItemType == QueueItemType.Capture);

        return new QueueCountsResponse(
            Overdue: overdue,
            DueSoon: dueSoon,
            StaleCaptures: staleCaptures,
            StaleDelegations: staleDelegations,
            Total: items.Count);
    }
}
