using System.Globalization;
using MentalMetal.Application.Briefings.Facts;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Goals;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Observations;
using MentalMetal.Domain.OneOnOnes;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Options;

namespace MentalMetal.Application.Briefings;

/// <summary>
/// Pure read-side facts assembly for briefings. Reads from repositories, never mutates,
/// never calls the LLM. Output is fully determined by repository state and the injected
/// <see cref="TimeProvider"/>; passing the same fake clock + same data twice yields the
/// same facts object. The synthesis (LLM) phase consumes these facts as JSON.
/// </summary>
public sealed class BriefingFactsAssembler(
    IUserRepository userRepository,
    IPersonRepository personRepository,
    IInitiativeRepository initiativeRepository,
    ICommitmentRepository commitmentRepository,
    IDelegationRepository delegationRepository,
    ICaptureRepository captureRepository,
    IOneOnOneRepository oneOnOneRepository,
    IObservationRepository observationRepository,
    IGoalRepository goalRepository,
    ICurrentUserService currentUserService,
    IOptions<BriefingOptions> options,
    TimeProvider timeProvider)
{
    private const int PeopleAttentionLastOneOnOneDays = 14;
    private const int PeopleWithoutOneOnOneWeeklyDays = 21;
    private const int RecentObservationDays = 30;
    private const int InitiativeAttentionStaleBriefDays = 7;
    private const int CapturesRecentHours = 24;
    private const int NotesPreviewLength = 240;

    public async Task<MorningBriefingFacts> BuildMorningAsync(CancellationToken cancellationToken)
    {
        var (userId, tz, today, _) = await ResolveUserContextAsync(cancellationToken);
        var opts = options.Value;
        var now = timeProvider.GetUtcNow();
        var topN = opts.TopItemsPerSection;

        // People + initiative resolution maps (built lazily after we know which ids appear).
        var people = await personRepository.GetAllAsync(userId, typeFilter: null, includeArchived: false, cancellationToken);
        var personById = people.ToDictionary(p => p.Id, p => p);

        // Top commitments due today or overdue.
        var openCommitments = await commitmentRepository.GetAllAsync(
            userId, directionFilter: null, statusFilter: CommitmentStatus.Open,
            personIdFilter: null, initiativeIdFilter: null, overdueFilter: null,
            cancellationToken);
        var topCommitments = openCommitments
            .Where(c => c.IsOverdue || c.DueDate == today)
            .OrderByDescending(c => c.IsOverdue)
            .ThenBy(c => c.DueDate ?? DateOnly.MaxValue)
            .ThenBy(c => c.Id)
            .Take(topN)
            .ToList();

        // 1:1s today.
        var allOneOnOnes = await oneOnOneRepository.GetAllAsync(userId, personIdFilter: null, cancellationToken);
        var oneOnOnesToday = allOneOnOnes
            .Where(o => o.OccurredAt == today)
            .OrderBy(o => o.PersonId)
            .ToList();

        // Overdue delegations.
        var allDelegations = await delegationRepository.GetAllAsync(
            userId, statusFilter: null, priorityFilter: null,
            delegatePersonIdFilter: null, initiativeIdFilter: null, cancellationToken);
        var overdueDelegations = allDelegations
            .Where(d => QueueTermsHelper.IsDelegationOverdue(d, today))
            .OrderByDescending(d => today.DayNumber - (d.DueDate?.DayNumber ?? today.DayNumber))
            .ThenBy(d => d.Id)
            .Take(topN)
            .ToList();

        // Recent captures (last 24h).
        var recentCaptureCutoff = now - TimeSpan.FromHours(CapturesRecentHours);
        var allCaptures = await captureRepository.GetCloseOutQueueAsync(userId, cancellationToken);
        var recentCaptures = allCaptures
            .Where(c => c.CapturedAt >= recentCaptureCutoff)
            .OrderByDescending(c => c.CapturedAt)
            .Take(topN)
            .ToList();

        // People needing attention: no 1:1 in 14d AND has at least one open commitment or delegation.
        var oneOnOnesByPerson = allOneOnOnes
            .GroupBy(o => o.PersonId)
            .ToDictionary(g => g.Key, g => g.Max(o => o.OccurredAt));
        var peopleNeedingAttention = BuildPeopleAttention(
            userId,
            people,
            openCommitments,
            allDelegations,
            oneOnOnesByPerson,
            today,
            PeopleAttentionLastOneOnOneDays,
            topN);

        // Resolve initiative names referenced by commitments/delegations once.
        var initiativeIds = openCommitments
            .Where(c => c.InitiativeId is not null)
            .Select(c => c.InitiativeId!.Value)
            .Concat(allDelegations.Where(d => d.InitiativeId is not null).Select(d => d.InitiativeId!.Value))
            .Distinct()
            .ToList();
        var initiativesById = await ResolveInitiativeNamesAsync(userId, initiativeIds, cancellationToken);

        return new MorningBriefingFacts(
            UserLocalDate: today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            UserTimezone: tz.Id,
            TopCommitmentsDueToday: topCommitments
                .Select(c => MapCommitment(c, personById, initiativesById))
                .ToList(),
            OneOnOnesToday: oneOnOnesToday
                .Select(o => MapOneOnOne(o, personById))
                .ToList(),
            OverdueDelegations: overdueDelegations
                .Select(d => MapDelegation(d, now, today, personById, initiativesById))
                .ToList(),
            RecentCaptures: recentCaptures
                .Select(c => MapCapture(c, now))
                .ToList(),
            PeopleNeedingAttention: peopleNeedingAttention);
    }

    public async Task<WeeklyBriefingFacts> BuildWeeklyAsync(CancellationToken cancellationToken)
    {
        var (userId, tz, today, _) = await ResolveUserContextAsync(cancellationToken);
        var opts = options.Value;
        var now = timeProvider.GetUtcNow();
        var topN = opts.TopItemsPerSection;

        // ISO week math (Monday-based).
        var (weekStart, weekEnd) = GetIsoWeekBounds(today);
        var isoWeek = ISOWeek.GetWeekOfYear(today.ToDateTime(TimeOnly.MinValue));
        var isoYear = ISOWeek.GetYear(today.ToDateTime(TimeOnly.MinValue));

        var people = await personRepository.GetAllAsync(userId, typeFilter: null, includeArchived: false, cancellationToken);
        var personById = people.ToDictionary(p => p.Id, p => p);

        // Milestones falling in this week.
        var initiatives = await initiativeRepository.GetAllAsync(userId, statusFilter: null, cancellationToken);
        var initiativesById = initiatives.ToDictionary(i => i.Id, i => i);
        var milestonesThisWeek = initiatives
            .SelectMany(i => i.Milestones.Select(m => (Initiative: i, Milestone: m)))
            .Where(t => t.Milestone.TargetDate >= weekStart && t.Milestone.TargetDate <= weekEnd)
            .OrderBy(t => t.Milestone.TargetDate)
            .ThenBy(t => t.Initiative.Title)
            .Take(2 * topN)
            .Select(t => new FactMilestone(t.Initiative.Id, t.Initiative.Title, t.Milestone.Id, t.Milestone.Title, t.Milestone.TargetDate, t.Milestone.IsCompleted))
            .ToList();

        // Overdue commitments + delegations.
        var openCommitments = await commitmentRepository.GetAllAsync(
            userId, directionFilter: null, statusFilter: CommitmentStatus.Open,
            personIdFilter: null, initiativeIdFilter: null, overdueFilter: null,
            cancellationToken);
        var overdueCommitments = openCommitments
            .Where(c => c.IsOverdue)
            .OrderBy(c => c.DueDate ?? DateOnly.MaxValue)
            .ThenBy(c => c.Id)
            .Take(2 * topN)
            .ToList();

        var allDelegations = await delegationRepository.GetAllAsync(
            userId, statusFilter: null, priorityFilter: null,
            delegatePersonIdFilter: null, initiativeIdFilter: null, cancellationToken);
        var overdueDelegations = allDelegations
            .Where(d => QueueTermsHelper.IsDelegationOverdue(d, today))
            .OrderBy(d => d.DueDate ?? DateOnly.MaxValue)
            .ThenBy(d => d.Id)
            .Take(2 * topN)
            .ToList();

        // Initiatives needing attention: brief is stale (>7 days) - InitiativeStatus has no AtRisk
        // value so we use the brief-staleness signal from initiative-living-brief instead.
        var staleBriefCutoff = now - TimeSpan.FromDays(InitiativeAttentionStaleBriefDays);
        var initiativesNeedingAttention = initiatives
            .Where(i => i.Status == InitiativeStatus.Active
                && (i.Brief.SummaryLastRefreshedAt is null || i.Brief.SummaryLastRefreshedAt < staleBriefCutoff))
            .OrderBy(i => i.Brief.SummaryLastRefreshedAt ?? DateTimeOffset.MinValue)
            .Take(topN)
            .Select(i => new FactInitiative(i.Id, i.Title, i.Status.ToString(), i.Brief.SummaryLastRefreshedAt))
            .ToList();

        // People without recent 1:1 (≥ 21 days).
        var allOneOnOnes = await oneOnOneRepository.GetAllAsync(userId, personIdFilter: null, cancellationToken);
        var oneOnOnesByPerson = allOneOnOnes
            .GroupBy(o => o.PersonId)
            .ToDictionary(g => g.Key, g => g.Max(o => o.OccurredAt));
        var peopleWithoutRecent = BuildPeopleAttention(
            userId,
            people,
            openCommitments,
            allDelegations,
            oneOnOnesByPerson,
            today,
            PeopleWithoutOneOnOneWeeklyDays,
            topN);

        // Resolve initiative names referenced by commitments/delegations.
        var refIds = overdueCommitments
            .Where(c => c.InitiativeId is not null)
            .Select(c => c.InitiativeId!.Value)
            .Concat(overdueDelegations.Where(d => d.InitiativeId is not null).Select(d => d.InitiativeId!.Value))
            .Distinct()
            .ToList();
        var initiativeNameMap = initiativesById
            .Where(kvp => refIds.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Title);

        return new WeeklyBriefingFacts(
            IsoYear: isoYear,
            WeekNumber: isoWeek,
            WeekStartIso: weekStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            WeekEndIso: weekEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            UserTimezone: tz.Id,
            MilestonesThisWeek: milestonesThisWeek,
            OverdueCommitments: overdueCommitments
                .Select(c => MapCommitment(c, personById, initiativeNameMap))
                .ToList(),
            OverdueDelegations: overdueDelegations
                .Select(d => MapDelegation(d, now, today, personById, initiativeNameMap))
                .ToList(),
            InitiativesNeedingAttention: initiativesNeedingAttention,
            PeopleWithoutRecentOneOnOne: peopleWithoutRecent);
    }

    public async Task<OneOnOnePrepFacts?> BuildOneOnOnePrepAsync(Guid personId, CancellationToken cancellationToken)
    {
        var (userId, _, today, _) = await ResolveUserContextAsync(cancellationToken);
        var opts = options.Value;
        var topN = opts.TopItemsPerSection;

        var person = await personRepository.GetByIdAsync(personId, cancellationToken);
        if (person is null || person.UserId != userId)
            return null;

        var oneOnOnes = await oneOnOneRepository.GetAllAsync(userId, personIdFilter: personId, cancellationToken);
        var lastOneOnOne = oneOnOnes
            .OrderByDescending(o => o.OccurredAt)
            .ThenByDescending(o => o.CreatedAt)
            .FirstOrDefault();

        var goals = await goalRepository.GetAllAsync(
            userId, personIdFilter: personId, typeFilter: null,
            statusFilter: GoalStatus.Active, fromDate: null, toDate: null, cancellationToken);

        var observationCutoff = today.AddDays(-RecentObservationDays);
        var observations = await observationRepository.GetAllAsync(
            userId, personIdFilter: personId, tagFilter: null,
            fromDate: observationCutoff, toDate: null, cancellationToken);
        var recentObservations = observations
            .OrderByDescending(o => o.OccurredAt)
            .ThenByDescending(o => o.CreatedAt)
            .Take(topN)
            .Select(o => new FactObservation(o.Id, o.OccurredAt, o.Tag.ToString(), Truncate(o.Description, NotesPreviewLength) ?? string.Empty))
            .ToList();

        var commitmentsWithPerson = await commitmentRepository.GetAllAsync(
            userId, directionFilter: null, statusFilter: CommitmentStatus.Open,
            personIdFilter: personId, initiativeIdFilter: null, overdueFilter: null,
            cancellationToken);
        var openCommitments = commitmentsWithPerson
            .OrderBy(c => c.DueDate ?? DateOnly.MaxValue)
            .ThenBy(c => c.Id)
            .Take(topN)
            .ToList();

        var delegationsToPerson = await delegationRepository.GetAllAsync(
            userId, statusFilter: null, priorityFilter: null,
            delegatePersonIdFilter: personId, initiativeIdFilter: null, cancellationToken);
        var openDelegations = delegationsToPerson
            .Where(d => d.Status != DelegationStatus.Completed)
            .OrderBy(d => d.DueDate ?? DateOnly.MaxValue)
            .ThenBy(d => d.Id)
            .Take(topN)
            .ToList();

        // Initiative-name resolution.
        var refIds = openCommitments
            .Where(c => c.InitiativeId is not null)
            .Select(c => c.InitiativeId!.Value)
            .Concat(openDelegations.Where(d => d.InitiativeId is not null).Select(d => d.InitiativeId!.Value))
            .Distinct()
            .ToList();
        var initiativesById = await ResolveInitiativeNamesAsync(userId, refIds, cancellationToken);
        var personById = new Dictionary<Guid, Person> { [person.Id] = person };

        var now = timeProvider.GetUtcNow();
        return new OneOnOnePrepFacts(
            Person: new FactPerson(person.Id, person.Name, person.Type.ToString()),
            LastOneOnOne: lastOneOnOne is null
                ? null
                : new FactOneOnOne(lastOneOnOne.Id, lastOneOnOne.OccurredAt, person.Id, person.Name, Truncate(lastOneOnOne.Notes, NotesPreviewLength)),
            OpenGoals: goals
                .OrderBy(g => g.TargetDate ?? DateOnly.MaxValue)
                .ThenBy(g => g.Title)
                .Select(g => new FactGoal(g.Id, g.Title, g.Type.ToString(), g.Status.ToString(), g.TargetDate))
                .ToList(),
            RecentObservations: recentObservations,
            OpenCommitmentsWithPerson: openCommitments
                .Select(c => MapCommitment(c, personById, initiativesById))
                .ToList(),
            OpenDelegationsToPerson: openDelegations
                .Select(d => MapDelegation(d, now, today, personById, initiativesById))
                .ToList());
    }

    private async Task<(Guid UserId, TimeZoneInfo TimeZone, DateOnly UserLocalToday, int LocalHour)> ResolveUserContextAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user not found.");

        var tz = ResolveTimeZone(user.Timezone);
        var nowLocal = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), tz);
        var todayLocal = DateOnly.FromDateTime(nowLocal.DateTime);

        // Apply the morning-briefing-hour rollback.
        var hourThreshold = options.Value.MorningBriefingHour;
        if (nowLocal.Hour < hourThreshold)
            todayLocal = todayLocal.AddDays(-1);

        return (userId, tz, todayLocal, nowLocal.Hour);
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return TimeZoneInfo.Utc;
        return TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz) ? tz : TimeZoneInfo.Utc;
    }

    private async Task<Dictionary<Guid, string>> ResolveInitiativeNamesAsync(
        Guid userId, IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return new Dictionary<Guid, string>();
        var initiatives = await initiativeRepository.GetByIdsAsync(userId, ids, cancellationToken);
        return initiatives.ToDictionary(i => i.Id, i => i.Title);
    }

    private static IReadOnlyList<FactPersonAttention> BuildPeopleAttention(
        Guid userId,
        IReadOnlyList<Person> people,
        IReadOnlyList<Commitment> openCommitments,
        IReadOnlyList<Delegation> allDelegations,
        Dictionary<Guid, DateOnly> oneOnOnesByPerson,
        DateOnly today,
        int staleDays,
        int topN)
    {
        var commitmentsByPerson = openCommitments
            .GroupBy(c => c.PersonId)
            .ToDictionary(g => g.Key, g => g.Count());
        var openDelegationsByPerson = allDelegations
            .Where(d => d.Status != DelegationStatus.Completed)
            .GroupBy(d => d.DelegatePersonId)
            .ToDictionary(g => g.Key, g => g.Count());

        var candidates = new List<(Person Person, int? DaysSince, int OpenC, int OpenD)>();
        foreach (var p in people)
        {
            var openC = commitmentsByPerson.GetValueOrDefault(p.Id, 0);
            var openD = openDelegationsByPerson.GetValueOrDefault(p.Id, 0);
            if (openC == 0 && openD == 0) continue;

            int? daysSince = null;
            if (oneOnOnesByPerson.TryGetValue(p.Id, out var lastDate))
                daysSince = today.DayNumber - lastDate.DayNumber;

            // Qualify only if 1:1 is missing or past staleDays.
            if (daysSince is not null && daysSince < staleDays) continue;

            candidates.Add((p, daysSince, openC, openD));
        }

        return candidates
            // Stable ordering: longest-since-1:1 first (null = "never" sorts highest), then
            // by total open items desc, then by name for determinism across runs.
            .OrderByDescending(t => t.DaysSince ?? int.MaxValue)
            .ThenByDescending(t => t.OpenC + t.OpenD)
            .ThenBy(t => t.Person.Name)
            .Take(topN)
            .Select(t => new FactPersonAttention(t.Person.Id, t.Person.Name, t.DaysSince, t.OpenC, t.OpenD))
            .ToList();
    }

    private static FactCommitment MapCommitment(
        Commitment c,
        Dictionary<Guid, Person> personById,
        Dictionary<Guid, string> initiativesById) =>
        new(
            c.Id,
            c.Description,
            c.Direction.ToString(),
            c.Status.ToString(),
            c.DueDate,
            c.IsOverdue,
            c.PersonId,
            personById.TryGetValue(c.PersonId, out var p) ? p.Name : null,
            c.InitiativeId,
            c.InitiativeId is { } iid && initiativesById.TryGetValue(iid, out var iname) ? iname : null);

    private static FactDelegation MapDelegation(
        Delegation d,
        DateTimeOffset now,
        DateOnly userLocalToday,
        Dictionary<Guid, Person> personById,
        Dictionary<Guid, string> initiativesById)
    {
        var lastTouch = d.LastFollowedUpAt ?? d.CreatedAt;
        var daysSinceLastTouch = (int)Math.Floor((now - lastTouch).TotalDays);
        var personName = personById.TryGetValue(d.DelegatePersonId, out var p) ? p.Name : "(unknown)";
        // Use the caller's user-local today so overdue determination matches the
        // selection predicate used to load these delegations - avoids drift near
        // UTC midnight when the user's local date != the server's UTC date.
        return new FactDelegation(
            d.Id,
            d.Description,
            d.Status.ToString(),
            d.Priority.ToString(),
            d.DueDate,
            QueueTermsHelper.IsDelegationOverdue(d, userLocalToday),
            daysSinceLastTouch,
            d.DelegatePersonId,
            personName,
            d.InitiativeId,
            d.InitiativeId is { } iid && initiativesById.TryGetValue(iid, out var iname) ? iname : null);
    }

    private static FactOneOnOne MapOneOnOne(
        OneOnOne o,
        Dictionary<Guid, Person> personById) =>
        new(
            o.Id,
            o.OccurredAt,
            o.PersonId,
            personById.TryGetValue(o.PersonId, out var p) ? p.Name : "(unknown)",
            Truncate(o.Notes, NotesPreviewLength));

    private static FactCapture MapCapture(Capture c, DateTimeOffset now)
    {
        var daysSince = (int)Math.Floor((now - c.CapturedAt).TotalDays);
        var title = string.IsNullOrWhiteSpace(c.Title) ? c.RawContent : c.Title;
        return new FactCapture(c.Id, Truncate(title, NotesPreviewLength) ?? string.Empty, c.CapturedAt, daysSince);
    }

    private static string? Truncate(string? input, int max)
    {
        if (input is null) return null;
        if (input.Length <= max) return input;
        return string.Concat(input.AsSpan(0, max), "…");
    }

    private static (DateOnly WeekStart, DateOnly WeekEnd) GetIsoWeekBounds(DateOnly date)
    {
        // ISO-8601 week starts on Monday (DayOfWeek.Monday=1, Sunday=0).
        var dt = date.ToDateTime(TimeOnly.MinValue);
        var dow = (int)dt.DayOfWeek;
        var daysFromMonday = dow == 0 ? 6 : dow - 1;
        var monday = date.AddDays(-daysFromMonday);
        var sunday = monday.AddDays(6);
        return (monday, sunday);
    }
}

/// <summary>
/// Local helper duplicating <c>QueuePrioritizationService.IsDelegationOverdue</c> to avoid
/// the briefing slice depending on the my-queue slice. Logic is intentionally identical.
/// </summary>
internal static class QueueTermsHelper
{
    public static bool IsDelegationOverdue(Delegation delegation, DateOnly today) =>
        delegation.DueDate is { } due
        && due < today
        && delegation.Status != DelegationStatus.Completed;
}
