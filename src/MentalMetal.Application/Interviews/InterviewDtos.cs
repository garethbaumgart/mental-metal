using MentalMetal.Domain.Interviews;

namespace MentalMetal.Application.Interviews;

public sealed record CreateInterviewRequest(
    Guid CandidatePersonId,
    string RoleTitle,
    DateTimeOffset? ScheduledAtUtc = null);

public sealed record UpdateInterviewRequest(
    string? RoleTitle,
    DateTimeOffset? ScheduledAtUtc,
    bool ClearScheduledAt = false);

public sealed record AdvanceInterviewStageRequest(InterviewStage TargetStage);

public sealed record RecordInterviewDecisionRequest(InterviewDecision Decision);

public sealed record UpsertScorecardRequest(string Competency, int Rating, string? Notes = null);

public sealed record SetTranscriptRequest(string RawText);

public sealed record InterviewScorecardResponse(
    Guid Id,
    string Competency,
    int Rating,
    string? Notes,
    DateTimeOffset RecordedAtUtc)
{
    public static InterviewScorecardResponse From(InterviewScorecard s) =>
        new(s.Id, s.Competency, s.Rating, s.Notes, s.RecordedAtUtc);
}

public sealed record InterviewTranscriptResponse(
    string RawText,
    string? Summary,
    InterviewDecision? RecommendedDecision,
    IReadOnlyList<string> RiskSignals,
    DateTimeOffset? AnalyzedAtUtc,
    string? Model)
{
    public static InterviewTranscriptResponse? From(InterviewTranscript? t) =>
        t is null
            ? null
            : new InterviewTranscriptResponse(
                t.RawText,
                t.Summary,
                t.RecommendedDecision,
                t.RiskSignals.ToList(),
                t.AnalyzedAtUtc,
                t.Model);
}

public sealed record InterviewResponse(
    Guid Id,
    Guid UserId,
    Guid CandidatePersonId,
    string RoleTitle,
    InterviewStage Stage,
    DateTimeOffset? ScheduledAtUtc,
    DateTimeOffset? CompletedAtUtc,
    InterviewDecision? Decision,
    InterviewTranscriptResponse? Transcript,
    IReadOnlyList<InterviewScorecardResponse> Scorecards,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static InterviewResponse From(Interview i) => new(
        i.Id,
        i.UserId,
        i.CandidatePersonId,
        i.RoleTitle,
        i.Stage,
        i.ScheduledAtUtc,
        i.CompletedAtUtc,
        i.Decision,
        InterviewTranscriptResponse.From(i.Transcript),
        i.Scorecards.Select(InterviewScorecardResponse.From).ToList(),
        i.CreatedAtUtc,
        i.UpdatedAtUtc);
}

public sealed record InterviewAnalysisResponse(
    string Summary,
    InterviewDecision? RecommendedDecision,
    IReadOnlyList<string> RiskSignals,
    string Model,
    DateTimeOffset AnalyzedAtUtc,
    string? Warning);
