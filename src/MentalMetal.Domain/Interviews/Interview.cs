using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Interviews;

public sealed class Interview : AggregateRoot, IUserScoped
{
    public const string InvalidStageTransitionCode = "invalid_stage_transition";
    public const string DecisionNotAllowedCode = "decision_not_allowed";
    public const string TranscriptMissingCode = "transcript_missing";

    private static readonly HashSet<InterviewStage> TerminalStages =
        [InterviewStage.Hired, InterviewStage.Rejected, InterviewStage.Withdrawn];

    private static readonly Dictionary<InterviewStage, InterviewStage> ForwardTransitions = new()
    {
        [InterviewStage.Applied] = InterviewStage.ScreenScheduled,
        [InterviewStage.ScreenScheduled] = InterviewStage.ScreenCompleted,
        [InterviewStage.ScreenCompleted] = InterviewStage.OnsiteScheduled,
        [InterviewStage.OnsiteScheduled] = InterviewStage.OnsiteCompleted,
        [InterviewStage.OnsiteCompleted] = InterviewStage.OfferExtended,
        [InterviewStage.OfferExtended] = InterviewStage.Hired,
    };

    private static readonly HashSet<InterviewStage> StagesPermittingDecision =
        [
            InterviewStage.ScreenCompleted,
            InterviewStage.OnsiteCompleted,
            InterviewStage.OfferExtended,
            InterviewStage.Hired,
            InterviewStage.Rejected,
        ];

    private static readonly HashSet<InterviewStage> StagesThatSetCompletedAt =
        [
            InterviewStage.ScreenCompleted,
            InterviewStage.OnsiteCompleted,
            InterviewStage.Hired,
            InterviewStage.Rejected,
        ];

    private readonly List<InterviewScorecard> _scorecards = [];

    public Guid UserId { get; private set; }
    public Guid CandidatePersonId { get; private set; }
    public string RoleTitle { get; private set; } = null!;
    public InterviewStage Stage { get; private set; }
    public DateTimeOffset? ScheduledAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public InterviewDecision? Decision { get; private set; }
    public InterviewTranscript? Transcript { get; private set; }
    public IReadOnlyList<InterviewScorecard> Scorecards => _scorecards;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Interview() { } // EF Core

    public static Interview Create(
        Guid userId,
        Guid candidatePersonId,
        string roleTitle,
        DateTimeOffset now,
        DateTimeOffset? scheduledAtUtc = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (candidatePersonId == Guid.Empty)
            throw new ArgumentException("CandidatePersonId is required.", nameof(candidatePersonId));
        ArgumentException.ThrowIfNullOrWhiteSpace(roleTitle);

        var interview = new Interview
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CandidatePersonId = candidatePersonId,
            RoleTitle = roleTitle.Trim(),
            Stage = InterviewStage.Applied,
            ScheduledAtUtc = scheduledAtUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        interview.RaiseDomainEvent(new InterviewCreated(interview.Id, userId, candidatePersonId, interview.RoleTitle));
        return interview;
    }

    public void UpdateMetadata(string? roleTitle, DateTimeOffset? scheduledAtUtc, bool clearScheduled, DateTimeOffset now)
    {
        if (roleTitle is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(roleTitle);
            RoleTitle = roleTitle.Trim();
        }

        if (clearScheduled)
            ScheduledAtUtc = null;
        else if (scheduledAtUtc is not null)
            ScheduledAtUtc = scheduledAtUtc;

        UpdatedAtUtc = now;
        RaiseDomainEvent(new InterviewUpdated(Id));
    }

    public void AdvanceStage(InterviewStage targetStage, DateTimeOffset now)
    {
        if (Stage == targetStage)
            return;

        if (TerminalStages.Contains(Stage))
            throw new DomainException(
                $"Cannot transition from terminal stage '{Stage}'.",
                InvalidStageTransitionCode);

        var isAbortTransition = targetStage is InterviewStage.Rejected or InterviewStage.Withdrawn;
        if (!isAbortTransition)
        {
            if (!ForwardTransitions.TryGetValue(Stage, out var validNext) || validNext != targetStage)
                throw new DomainException(
                    $"Invalid stage transition from '{Stage}' to '{targetStage}'.",
                    InvalidStageTransitionCode);
        }

        var fromStage = Stage;
        Stage = targetStage;

        if (StagesThatSetCompletedAt.Contains(targetStage) && CompletedAtUtc is null)
            CompletedAtUtc = now;

        UpdatedAtUtc = now;
        RaiseDomainEvent(new InterviewStageChanged(Id, fromStage, targetStage));
    }

    public void RecordDecision(InterviewDecision decision, DateTimeOffset now)
    {
        if (!StagesPermittingDecision.Contains(Stage))
            throw new DomainException(
                $"Decision cannot be recorded while interview is in stage '{Stage}'.",
                DecisionNotAllowedCode);

        Decision = decision;
        UpdatedAtUtc = now;
        RaiseDomainEvent(new InterviewDecisionRecorded(Id, decision));
    }

    public InterviewScorecard AddScorecard(string competency, int rating, string? notes, DateTimeOffset now)
    {
        var card = InterviewScorecard.Create(competency, rating, notes, now);
        _scorecards.Add(card);
        UpdatedAtUtc = now;
        RaiseDomainEvent(new InterviewScorecardAdded(Id, card.Id));
        return card;
    }

    public void UpdateScorecard(Guid scorecardId, string competency, int rating, string? notes, DateTimeOffset now)
    {
        var card = _scorecards.FirstOrDefault(s => s.Id == scorecardId)
            ?? throw new ArgumentException($"Scorecard '{scorecardId}' not found.");

        card.Update(competency, rating, notes, now);
        UpdatedAtUtc = now;
        RaiseDomainEvent(new InterviewScorecardUpdated(Id, scorecardId));
    }

    public InterviewScorecard? RemoveScorecard(Guid scorecardId, DateTimeOffset now)
    {
        var card = _scorecards.FirstOrDefault(s => s.Id == scorecardId);
        if (card is null)
            throw new ArgumentException($"Scorecard '{scorecardId}' not found.");

        _scorecards.Remove(card);
        UpdatedAtUtc = now;
        RaiseDomainEvent(new InterviewScorecardRemoved(Id, scorecardId));
        return card;
    }

    public void SetTranscript(string rawText, DateTimeOffset now)
    {
        Transcript = InterviewTranscript.Create(rawText);
        UpdatedAtUtc = now;
        RaiseDomainEvent(new InterviewTranscriptSet(Id));
    }

    public void ApplyAnalysis(
        string summary,
        InterviewDecision? recommendedDecision,
        IEnumerable<string> riskSignals,
        string model,
        DateTimeOffset now)
    {
        if (Transcript is null || string.IsNullOrWhiteSpace(Transcript.RawText))
            throw new DomainException(
                "Cannot apply AI analysis without a transcript.",
                TranscriptMissingCode);

        Transcript.WithAnalysis(summary, recommendedDecision, riskSignals, model, now);
        UpdatedAtUtc = now;
        RaiseDomainEvent(new InterviewAnalysisGenerated(Id, model));
    }

    public void MarkDeleted() => RaiseDomainEvent(new InterviewDeleted(Id));
}
