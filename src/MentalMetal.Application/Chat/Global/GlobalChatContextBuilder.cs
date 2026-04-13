using MentalMetal.Domain.Captures;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.People;

namespace MentalMetal.Application.Chat.Global;

public sealed class GlobalChatContextBuilder(
    IPersonRepository people,
    IInitiativeRepository initiatives,
    ICommitmentRepository commitments,
    IDelegationRepository delegations,
    ICaptureRepository captures) : IGlobalChatContextBuilder
{
    // Per-intent caps mirror the spec.
    public const int CommitmentTimeWindowCap = 30;
    public const int DelegationTimeWindowCap = 30;
    public const int OverdueCommitmentCap = 30;
    public const int OverdueDelegationCap = 30;
    public const int PersonsPerLensCap = 5;
    public const int InitiativesPerStatusCap = 5;
    public const int CaptureSearchCap = 30;
    public const int GenericTopOverdueCap = 5;
    public const int GenericTopRecentCaptureCap = 5;

    // Token budget — char/4 estimate. Leaves room for response in a 100k window.
    public const int DefaultTokenBudget = 16000;

    public async Task<GlobalChatContextPayload> BuildAsync(
        Guid userId,
        IntentSet intents,
        IReadOnlyList<ChatMessage> conversationHistory,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        ArgumentNullException.ThrowIfNull(intents);

        // Pull the data slices needed; each slice is independently capped.
        var personItems = new List<PersonContextItem>();
        var initiativeItems = new List<InitiativeContextItem>();
        var commitmentItems = new List<CommitmentContextItem>();
        var delegationItems = new List<DelegationContextItem>();
        var captureItems = new List<CaptureContextItem>();
        var truncationNotes = new List<string>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
        var endOfWeek = today.AddDays(daysUntilSaturday);

        // ---- Persons (PersonLens) ------------------------------------------------
        if (intents.Intents.Contains(ChatIntent.PersonLens) && intents.Hints.PersonIds.Count > 0)
        {
            var personIds = intents.Hints.PersonIds.Take(PersonsPerLensCap).ToList();
            var fetched = await people.GetByIdsAsync(userId, personIds, cancellationToken);
            foreach (var person in fetched.Where(p => p.UserId == userId))
            {
                personItems.Add(new PersonContextItem(person.Id, person.Name, person.Type.ToString(), person.Role, person.Team));
            }

            if (intents.Hints.PersonIds.Count > PersonsPerLensCap)
                truncationNotes.Add($"Persons capped at {PersonsPerLensCap}");
        }

        // ---- Initiatives (InitiativeStatus) -------------------------------------
        if (intents.Intents.Contains(ChatIntent.InitiativeStatus) && intents.Hints.InitiativeIds.Count > 0)
        {
            var initiativeIds = intents.Hints.InitiativeIds.Take(InitiativesPerStatusCap).ToList();
            foreach (var initiativeId in initiativeIds)
            {
                var initiative = await initiatives.GetByIdAsync(initiativeId, cancellationToken);
                if (initiative is null || initiative.UserId != userId) continue;

                var brief = initiative.Brief ?? Domain.Initiatives.LivingBrief.LivingBrief.Empty();
                var recentDecisions = brief.KeyDecisions
                    .OrderByDescending(d => d.LoggedAt)
                    .Take(10)
                    .Select(d => d.Id)
                    .ToList();
                var openRisks = brief.Risks
                    .Where(r => r.Status == Domain.Initiatives.LivingBrief.RiskStatus.Open)
                    .Select(r => r.Id)
                    .ToList();

                initiativeItems.Add(new InitiativeContextItem(
                    initiative.Id, initiative.Title, initiative.Status.ToString(),
                    string.IsNullOrWhiteSpace(brief.Summary) ? null : brief.Summary,
                    recentDecisions, openRisks));
            }

            if (intents.Hints.InitiativeIds.Count > InitiativesPerStatusCap)
                truncationNotes.Add($"Initiatives capped at {InitiativesPerStatusCap}");
        }

        // ---- Commitments / Delegations slices -----------------------------------
        // Only load when an active intent actually needs them. Filtering happens in-memory off
        // the user's own (already user-filtered) lists — defence in depth re-filters by UserId.
        var isGeneric = intents.IsGenericOnly || intents.Intents.Contains(ChatIntent.Generic);
        var needsCommitments =
            intents.Intents.Contains(ChatIntent.MyDay)
            || intents.Intents.Contains(ChatIntent.MyWeek)
            || intents.Intents.Contains(ChatIntent.OverdueWork)
            || (intents.Intents.Contains(ChatIntent.PersonLens) && intents.Hints.PersonIds.Count > 0)
            || (intents.Intents.Contains(ChatIntent.InitiativeStatus) && intents.Hints.InitiativeIds.Count > 0)
            || isGeneric;
        var needsDelegations =
            intents.Intents.Contains(ChatIntent.MyDay)
            || intents.Intents.Contains(ChatIntent.MyWeek)
            || intents.Intents.Contains(ChatIntent.OverdueWork)
            || (intents.Intents.Contains(ChatIntent.PersonLens) && intents.Hints.PersonIds.Count > 0)
            || (intents.Intents.Contains(ChatIntent.InitiativeStatus) && intents.Hints.InitiativeIds.Count > 0)
            || isGeneric;

        var userCommitments = new List<Commitment>();
        var userDelegations = new List<Delegation>();

        if (needsCommitments)
        {
            var allCommitments = await commitments.GetAllAsync(
                userId, directionFilter: null, statusFilter: null,
                personIdFilter: null, initiativeIdFilter: null,
                overdueFilter: null, cancellationToken);
            userCommitments = allCommitments.Where(c => c.UserId == userId).ToList();
        }

        if (needsDelegations)
        {
            var allDelegations = await delegations.GetAllAsync(
                userId, statusFilter: null, priorityFilter: null,
                delegatePersonIdFilter: null, initiativeIdFilter: null,
                cancellationToken);
            userDelegations = allDelegations.Where(d => d.UserId == userId).ToList();
        }

        var addedCommitmentIds = new HashSet<Guid>();
        var addedDelegationIds = new HashSet<Guid>();

        if (intents.Intents.Contains(ChatIntent.MyDay))
        {
            AddCommitments(userCommitments
                .Where(c => c.Status == CommitmentStatus.Open && c.DueDate == today)
                .Take(CommitmentTimeWindowCap), commitmentItems, addedCommitmentIds);
            AddDelegations(userDelegations
                .Where(d => d.Status is DelegationStatus.Assigned or DelegationStatus.InProgress or DelegationStatus.Blocked
                    && d.DueDate == today)
                .Take(DelegationTimeWindowCap), delegationItems, addedDelegationIds);
        }

        if (intents.Intents.Contains(ChatIntent.MyWeek))
        {
            AddCommitments(userCommitments
                .Where(c => c.Status == CommitmentStatus.Open && c.DueDate is not null && c.DueDate >= today && c.DueDate <= endOfWeek)
                .Take(CommitmentTimeWindowCap), commitmentItems, addedCommitmentIds);
            AddDelegations(userDelegations
                .Where(d => d.Status is DelegationStatus.Assigned or DelegationStatus.InProgress or DelegationStatus.Blocked
                    && d.DueDate is not null && d.DueDate >= today && d.DueDate <= endOfWeek)
                .Take(DelegationTimeWindowCap), delegationItems, addedDelegationIds);
        }

        if (intents.Intents.Contains(ChatIntent.OverdueWork))
        {
            AddCommitments(userCommitments
                .Where(c => c.IsOverdue)
                .OrderBy(c => c.DueDate)
                .Take(OverdueCommitmentCap), commitmentItems, addedCommitmentIds);
            AddDelegations(userDelegations
                .Where(d => d.Status is DelegationStatus.Assigned or DelegationStatus.InProgress or DelegationStatus.Blocked
                    && d.DueDate is not null
                    && d.DueDate < today)
                .OrderBy(d => d.DueDate)
                .Take(OverdueDelegationCap), delegationItems, addedDelegationIds);
        }

        // For PersonLens: pull open commitments / delegations linked to each person (cap 10 each).
        if (intents.Intents.Contains(ChatIntent.PersonLens) && intents.Hints.PersonIds.Count > 0)
        {
            foreach (var personId in intents.Hints.PersonIds.Take(PersonsPerLensCap))
            {
                AddCommitments(userCommitments
                    .Where(c => c.PersonId == personId && c.Status == CommitmentStatus.Open)
                    .OrderByDescending(c => c.UpdatedAt)
                    .Take(10), commitmentItems, addedCommitmentIds);
                AddDelegations(userDelegations
                    .Where(d => d.DelegatePersonId == personId
                        && d.Status is DelegationStatus.Assigned or DelegationStatus.InProgress or DelegationStatus.Blocked)
                    .OrderByDescending(d => d.UpdatedAt)
                    .Take(10), delegationItems, addedDelegationIds);
            }
        }

        if (intents.Intents.Contains(ChatIntent.InitiativeStatus) && intents.Hints.InitiativeIds.Count > 0)
        {
            foreach (var initiativeId in intents.Hints.InitiativeIds.Take(InitiativesPerStatusCap))
            {
                AddCommitments(userCommitments
                    .Where(c => c.InitiativeId == initiativeId && c.Status == CommitmentStatus.Open)
                    .OrderByDescending(c => c.UpdatedAt)
                    .Take(20), commitmentItems, addedCommitmentIds);
                AddDelegations(userDelegations
                    .Where(d => d.InitiativeId == initiativeId
                        && d.Status is DelegationStatus.Assigned or DelegationStatus.InProgress or DelegationStatus.Blocked)
                    .OrderByDescending(d => d.UpdatedAt)
                    .Take(20), delegationItems, addedDelegationIds);
            }
        }

        // ---- Captures (CaptureSearch) -------------------------------------------
        if (intents.Intents.Contains(ChatIntent.CaptureSearch))
        {
            var capList = await captures.GetAllAsync(userId, typeFilter: null, statusFilter: ProcessingStatus.Processed, cancellationToken);
            var filtered = capList
                .Where(c => c.UserId == userId)
                .Where(c => intents.Hints.DateRange is null
                    || (DateOnly.FromDateTime(c.CapturedAt.UtcDateTime) >= intents.Hints.DateRange.Start
                        && DateOnly.FromDateTime(c.CapturedAt.UtcDateTime) <= intents.Hints.DateRange.End))
                .OrderByDescending(c => c.CapturedAt)
                .Take(CaptureSearchCap);

            foreach (var capture in filtered)
                captureItems.Add(new CaptureContextItem(capture.Id, capture.CapturedAt, capture.AiExtraction?.Summary ?? string.Empty));
        }

        // ---- Generic additions (must happen before name resolution so ordering and
        // names survive for these items too) --------------------------------------
        if (intents.IsGenericOnly || intents.Intents.Contains(ChatIntent.Generic))
        {
            // Top-5 most overdue — AddCommitments preserves OrderBy(DueDate) ranking.
            AddCommitments(userCommitments
                .Where(c => c.IsOverdue)
                .OrderBy(c => c.DueDate)
                .Take(GenericTopOverdueCap), commitmentItems, addedCommitmentIds);

            // Top-5 most-recent confirmed captures
            var recentCaptures = (await captures.GetAllAsync(userId, typeFilter: null, statusFilter: ProcessingStatus.Processed, cancellationToken))
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CapturedAt)
                .Take(GenericTopRecentCaptureCap);
            foreach (var capture in recentCaptures)
            {
                if (captureItems.All(c => c.Id != capture.Id))
                    captureItems.Add(new CaptureContextItem(capture.Id, capture.CapturedAt, capture.AiExtraction?.Summary ?? string.Empty));
            }
        }

        // ---- Person-name resolution for already-collected commitments/delegations ----
        // Build a lookup dictionary, then rehydrate the existing placeholder items in-place
        // (preserves ordering from the earlier OrderBy(...).Take(...) selection passes).
        var commitmentPersonIds = userCommitments.Where(c => addedCommitmentIds.Contains(c.Id)).Select(c => c.PersonId);
        var delegationPersonIds = userDelegations.Where(d => addedDelegationIds.Contains(d.Id)).Select(d => d.DelegatePersonId);
        var personIdsToLookup = commitmentPersonIds.Concat(delegationPersonIds).Distinct().ToList();
        var personLookup = personIdsToLookup.Count == 0
            ? new Dictionary<Guid, string>()
            : (await people.GetByIdsAsync(userId, personIdsToLookup, cancellationToken))
                .ToDictionary(p => p.Id, p => p.Name);

        var commitmentById = userCommitments.Where(c => addedCommitmentIds.Contains(c.Id)).ToDictionary(c => c.Id);
        var delegationById = userDelegations.Where(d => addedDelegationIds.Contains(d.Id)).ToDictionary(d => d.Id);

        commitmentItems = commitmentItems.Select(item =>
            commitmentById.TryGetValue(item.Id, out var c)
                ? new CommitmentContextItem(
                    c.Id, c.Description, c.Direction.ToString(),
                    personLookup.TryGetValue(c.PersonId, out var pn) ? pn : null,
                    c.Status.ToString(), c.DueDate, c.IsOverdue)
                : item).ToList();

        delegationItems = delegationItems.Select(item =>
            delegationById.TryGetValue(item.Id, out var d)
                ? new DelegationContextItem(
                    d.Id, d.Description,
                    personLookup.TryGetValue(d.DelegatePersonId, out var dn) ? dn : null,
                    d.Status.ToString(), d.DueDate,
                    d.DueDate is not null && d.DueDate < today
                        && d.Status is DelegationStatus.Assigned or DelegationStatus.InProgress or DelegationStatus.Blocked,
                    d.Status == DelegationStatus.Blocked ? d.Notes : null)
                : item).ToList();

        // ---- Counters (populated for every request, not just Generic) ----------
        var allInitiatives = await initiatives.GetAllAsync(userId, statusFilter: null, cancellationToken);
        var counters = new GlobalCounters(
            OpenCommitments: userCommitments.Count(c => c.Status == CommitmentStatus.Open),
            OpenDelegations: userDelegations.Count(d => d.Status is DelegationStatus.Assigned or DelegationStatus.InProgress or DelegationStatus.Blocked),
            ActiveInitiatives: allInitiatives.Count(i => i.UserId == userId && i.Status == InitiativeStatus.Active));

        // ---- Token budget enforcement ----------------------------------------------
        // Estimate via char count; trim least-important sections first per spec priority order.
        var (finalCaptures, finalDelegations, finalCommitments, finalPersons, finalInitiatives, addedNotes) =
            EnforceBudget(captureItems, delegationItems, commitmentItems, personItems, initiativeItems);
        truncationNotes.AddRange(addedNotes);

        return new GlobalChatContextPayload(
            intents,
            counters,
            finalPersons,
            finalInitiatives,
            finalCommitments,
            finalDelegations,
            finalCaptures,
            truncationNotes,
            conversationHistory);
    }

    private static void AddCommitments(IEnumerable<Commitment> source, List<CommitmentContextItem> sink, HashSet<Guid> seen)
    {
        // Two-stage: this method only marks items as "selected" — names are resolved in a
        // single batch later for cheaper repository access.
        foreach (var c in source)
        {
            if (seen.Add(c.Id))
            {
                // Placeholder; real items rebuilt with names later. Avoid duplicating data here.
                sink.Add(new CommitmentContextItem(c.Id, c.Description, c.Direction.ToString(), null, c.Status.ToString(), c.DueDate, c.IsOverdue));
            }
        }
    }

    private static void AddDelegations(IEnumerable<Delegation> source, List<DelegationContextItem> sink, HashSet<Guid> seen)
    {
        foreach (var d in source)
        {
            if (seen.Add(d.Id))
            {
                sink.Add(new DelegationContextItem(d.Id, d.Description, null, d.Status.ToString(), d.DueDate, IsOverdue: false, BlockedReason: null));
            }
        }
    }

    /// <summary>
    /// Trim sections in priority order until the estimated payload fits within the budget.
    /// Returns the (possibly-trimmed) lists plus any notes about what was dropped.
    /// </summary>
    private static (
        IReadOnlyList<CaptureContextItem> Captures,
        IReadOnlyList<DelegationContextItem> Delegations,
        IReadOnlyList<CommitmentContextItem> Commitments,
        IReadOnlyList<PersonContextItem> Persons,
        IReadOnlyList<InitiativeContextItem> Initiatives,
        IReadOnlyList<string> Notes)
    EnforceBudget(
        List<CaptureContextItem> captures,
        List<DelegationContextItem> delegations,
        List<CommitmentContextItem> commitments,
        List<PersonContextItem> persons,
        List<InitiativeContextItem> initiatives)
    {
        var notes = new List<string>();
        var working = (caps: captures.ToList(), dels: delegations.ToList(), coms: commitments.ToList(), per: persons.ToList(), init: initiatives.ToList());

        if (Estimate(working.caps, working.dels, working.coms, working.per, working.init) <= DefaultTokenBudget)
            return (working.caps, working.dels, working.coms, working.per, working.init, notes);

        // Priority order: drop least important first.
        if (TrimList(working.caps, "captures")) notes.Add("Captures truncated to fit context budget");
        if (Estimate(working.caps, working.dels, working.coms, working.per, working.init) <= DefaultTokenBudget)
            return (working.caps, working.dels, working.coms, working.per, working.init, notes);

        if (TrimList(working.dels, "delegations")) notes.Add("Delegations truncated to fit context budget");
        if (Estimate(working.caps, working.dels, working.coms, working.per, working.init) <= DefaultTokenBudget)
            return (working.caps, working.dels, working.coms, working.per, working.init, notes);

        if (TrimList(working.coms, "commitments")) notes.Add("Commitments truncated to fit context budget");
        if (Estimate(working.caps, working.dels, working.coms, working.per, working.init) <= DefaultTokenBudget)
            return (working.caps, working.dels, working.coms, working.per, working.init, notes);

        // Last resort: trim person/initiative cores
        if (TrimList(working.per, "persons")) notes.Add("Persons truncated to fit context budget");
        if (TrimList(working.init, "initiatives")) notes.Add("Initiatives truncated to fit context budget");

        return (working.caps, working.dels, working.coms, working.per, working.init, notes);
    }

    private static bool TrimList<T>(List<T> list, string _label)
    {
        if (list.Count == 0) return false;
        var halfway = list.Count / 2;
        if (halfway == 0) halfway = list.Count - 1;
        list.RemoveRange(halfway, list.Count - halfway);
        return true;
    }

    private static int Estimate(
        IEnumerable<CaptureContextItem> captures,
        IEnumerable<DelegationContextItem> delegations,
        IEnumerable<CommitmentContextItem> commitments,
        IEnumerable<PersonContextItem> persons,
        IEnumerable<InitiativeContextItem> initiatives)
    {
        var chars = 0;
        foreach (var c in captures) chars += (c.Summary?.Length ?? 0) + 80;
        foreach (var d in delegations) chars += (d.Description?.Length ?? 0) + (d.DelegateName?.Length ?? 0) + 80;
        foreach (var c in commitments) chars += (c.Description?.Length ?? 0) + (c.PersonName?.Length ?? 0) + 80;
        foreach (var p in persons) chars += (p.Name?.Length ?? 0) + 60;
        foreach (var i in initiatives) chars += (i.Title?.Length ?? 0) + (i.BriefSummary?.Length ?? 0) + 80;
        return (chars + 3) / 4;
    }
}
