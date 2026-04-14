namespace MentalMetal.Application.Briefings.Facts;

public sealed record FactPerson(Guid Id, string Name, string Type);

public sealed record FactCommitment(
    Guid Id,
    string Description,
    string Direction,
    string Status,
    DateOnly? DueDate,
    bool IsOverdue,
    Guid? PersonId,
    string? PersonName,
    Guid? InitiativeId,
    string? InitiativeName);

public sealed record FactDelegation(
    Guid Id,
    string Description,
    string Status,
    string Priority,
    DateOnly? DueDate,
    bool IsOverdue,
    int DaysSinceLastTouch,
    Guid PersonId,
    string PersonName,
    Guid? InitiativeId,
    string? InitiativeName);

public sealed record FactOneOnOne(
    Guid Id,
    DateOnly OccurredAt,
    Guid PersonId,
    string PersonName,
    string? NotesPreview);

public sealed record FactCapture(
    Guid Id,
    string Title,
    DateTimeOffset CapturedAtUtc,
    int DaysSinceCaptured);

public sealed record FactPersonAttention(
    Guid PersonId,
    string PersonName,
    int? DaysSinceLastOneOnOne,
    int OpenCommitmentCount,
    int OpenDelegationCount);

public sealed record FactObservation(
    Guid Id,
    DateOnly OccurredAt,
    string Tag,
    string DescriptionPreview);

public sealed record FactGoal(
    Guid Id,
    string Title,
    string Type,
    string Status,
    DateOnly? TargetDate);

public sealed record FactInitiative(
    Guid Id,
    string Title,
    string Status,
    DateTimeOffset? BriefLastRefreshedAt);

public sealed record FactMilestone(
    Guid InitiativeId,
    string InitiativeTitle,
    Guid MilestoneId,
    string Title,
    DateOnly TargetDate,
    bool IsCompleted);

/// <summary>
/// Deterministic facts assembled for the morning briefing. The AI never sees anything
/// outside this object; it must not invent additional names, dates, or counts.
/// </summary>
public sealed record MorningBriefingFacts(
    string UserLocalDate,
    string UserTimezone,
    IReadOnlyList<FactCommitment> TopCommitmentsDueToday,
    IReadOnlyList<FactOneOnOne> OneOnOnesToday,
    IReadOnlyList<FactDelegation> OverdueDelegations,
    IReadOnlyList<FactCapture> RecentCaptures,
    IReadOnlyList<FactPersonAttention> PeopleNeedingAttention);

public sealed record WeeklyBriefingFacts(
    int IsoYear,
    int WeekNumber,
    string WeekStartIso,
    string WeekEndIso,
    string UserTimezone,
    IReadOnlyList<FactMilestone> MilestonesThisWeek,
    IReadOnlyList<FactCommitment> OverdueCommitments,
    IReadOnlyList<FactDelegation> OverdueDelegations,
    IReadOnlyList<FactInitiative> InitiativesNeedingAttention,
    IReadOnlyList<FactPersonAttention> PeopleWithoutRecentOneOnOne);

public sealed record OneOnOnePrepFacts(
    FactPerson Person,
    FactOneOnOne? LastOneOnOne,
    IReadOnlyList<FactGoal> OpenGoals,
    IReadOnlyList<FactObservation> RecentObservations,
    IReadOnlyList<FactCommitment> OpenCommitmentsWithPerson,
    IReadOnlyList<FactDelegation> OpenDelegationsToPerson);
