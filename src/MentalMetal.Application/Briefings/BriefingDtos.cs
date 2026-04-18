using MentalMetal.Domain.Commitments;

namespace MentalMetal.Application.Briefings;

// --- Daily Brief DTOs ---

public sealed record DailyBriefResponse(
    string Narrative,
    List<BriefCommitmentDto> FreshCommitments,
    List<BriefCommitmentDto> DueToday,
    List<BriefCommitmentDto> Overdue,
    List<PersonActivityDto> PeopleActivity,
    int CaptureCount,
    DateTimeOffset GeneratedAt);

public sealed record BriefCommitmentDto(
    Guid Id,
    string Description,
    CommitmentDirection Direction,
    Guid PersonId,
    string? PersonName,
    DateOnly? DueDate,
    bool IsOverdue,
    CommitmentConfidence Confidence);

public sealed record PersonActivityDto(
    Guid PersonId,
    string PersonName,
    int MentionCount);

// --- Weekly Brief DTOs ---

public sealed record WeeklyBriefResponse(
    string Narrative,
    List<string> CrossConversationInsights,
    List<string> Decisions,
    CommitmentStatusSummary CommitmentStatus,
    List<string> Risks,
    List<InitiativeActivityDto> InitiativeActivity,
    DateRange DateRange,
    DateTimeOffset GeneratedAt);

public sealed record CommitmentStatusSummary(
    int NewCount,
    int CompletedCount,
    int OverdueCount,
    int TotalOpen);

public sealed record InitiativeActivityDto(
    Guid InitiativeId,
    string Title,
    int CaptureCount,
    string? AutoSummary);

public sealed record DateRange(
    DateTimeOffset Start,
    DateTimeOffset End);
