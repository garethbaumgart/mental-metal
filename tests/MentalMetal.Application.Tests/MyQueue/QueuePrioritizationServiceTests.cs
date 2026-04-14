using MentalMetal.Application.MyQueue;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Delegations;

namespace MentalMetal.Application.Tests.MyQueue;

public class QueuePrioritizationServiceTests
{
    private readonly QueuePrioritizationService _service = new();
    private readonly MyQueueOptions _options = new();
    private readonly DateTimeOffset _now = new(2026, 4, 14, 12, 0, 0, TimeSpan.Zero);
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _personId = Guid.NewGuid();

    // --- Commitments ---

    [Fact]
    public void OverdueCommitment_ScoresHigherThanDueSoonCommitment()
    {
        var today = DateOnly.FromDateTime(_now.UtcDateTime);
        var overdue = Commitment.Create(_userId, "overdue task", CommitmentDirection.MineToThem, _personId, today.AddDays(-3));
        var dueSoon = Commitment.Create(_userId, "due soon", CommitmentDirection.MineToThem, _personId, today.AddDays(3));

        var overdueScore = _service.ScoreCommitment(overdue, _now, _options);
        var dueSoonScore = _service.ScoreCommitment(dueSoon, _now, _options);

        Assert.True(overdueScore > dueSoonScore,
            $"overdue={overdueScore} should exceed dueSoon={dueSoonScore}");
    }

    [Fact]
    public void NoDueDateCommitment_HasPositiveScore()
    {
        var c = Commitment.Create(_userId, "no due", CommitmentDirection.MineToThem, _personId, dueDate: null);
        var score = _service.ScoreCommitment(c, _now, _options);
        Assert.Equal(10, score);
    }

    [Fact]
    public void OverdueContribution_IsBounded()
    {
        var today = DateOnly.FromDateTime(_now.UtcDateTime);
        // 500 days overdue should cap at 100 + 100 = 200 contribution from overdue portion.
        var veryOverdue = Commitment.Create(_userId, "ancient", CommitmentDirection.MineToThem, _personId, today.AddDays(-500));
        var score = _service.ScoreCommitment(veryOverdue, _now, _options);
        Assert.True(score <= 200, $"expected <=200, got {score}");
        Assert.True(score >= 200, $"expected cap hit, got {score}");
    }

    [Fact]
    public void DueTodayCommitment_ScoresHigherThanDueInAWeek()
    {
        var today = DateOnly.FromDateTime(_now.UtcDateTime);
        var dueToday = Commitment.Create(_userId, "today", CommitmentDirection.MineToThem, _personId, today);
        var dueInAWeek = Commitment.Create(_userId, "later", CommitmentDirection.MineToThem, _personId, today.AddDays(7));

        var todayScore = _service.ScoreCommitment(dueToday, _now, _options);
        var laterScore = _service.ScoreCommitment(dueInAWeek, _now, _options);

        Assert.True(todayScore > laterScore);
    }

    // --- Delegations ---

    [Fact]
    public void UrgentDelegation_ScoresHigherThanLow()
    {
        var urgent = Delegation.Create(_userId, "u", _personId, priority: Priority.Urgent);
        var low = Delegation.Create(_userId, "l", _personId, priority: Priority.Low);

        var uScore = _service.ScoreDelegation(urgent, _now, _options);
        var lScore = _service.ScoreDelegation(low, _now, _options);

        Assert.True(uScore > lScore);
    }

    [Fact]
    public void BlockedDelegation_ReceivesBlockerBump()
    {
        var inProgress = Delegation.Create(_userId, "a", _personId, priority: Priority.Medium);
        inProgress.MarkInProgress();
        var blocked = Delegation.Create(_userId, "b", _personId, priority: Priority.Medium);
        blocked.MarkInProgress();
        blocked.MarkBlocked("waiting on X");

        var inProgressScore = _service.ScoreDelegation(inProgress, _now, _options);
        var blockedScore = _service.ScoreDelegation(blocked, _now, _options);

        Assert.True(blockedScore > inProgressScore,
            $"blocked={blockedScore} should exceed inProgress={inProgressScore}");
    }

    // --- Captures ---

    [Fact]
    public void FailedCapture_ScoresHigherThanEquallyAgedRaw()
    {
        var raw = Capture.Create(_userId, "raw content", CaptureType.QuickNote);
        var failed = Capture.Create(_userId, "failed content", CaptureType.QuickNote);
        failed.BeginProcessing();
        failed.FailProcessing("error");

        // Evaluate 10 days "later" so both are past the staleness threshold.
        var later = _now.AddDays(10);

        var rawScore = _service.ScoreCapture(raw, later, _options);
        var failedScore = _service.ScoreCapture(failed, later, _options);

        Assert.True(failedScore > rawScore,
            $"failed={failedScore} should exceed raw={rawScore}");
    }

    [Fact]
    public void CaptureBelowStalenessThreshold_ScoresZero()
    {
        var c = Capture.Create(_userId, "fresh", CaptureType.QuickNote);
        var score = _service.ScoreCapture(c, _now, _options); // same instant = 0 days old
        Assert.Equal(0, score);
    }

    [Fact]
    public void TieBreakers_AreDeterministic()
    {
        // Two commitments with identical due dates should score the same, so ordering falls
        // to (DueDate asc, CapturedAt desc, Id asc) at the handler layer. The service itself
        // is stateless, so equal states yield equal scores.
        var today = DateOnly.FromDateTime(_now.UtcDateTime);
        var a = Commitment.Create(_userId, "a", CommitmentDirection.MineToThem, _personId, today.AddDays(2));
        var b = Commitment.Create(_userId, "b", CommitmentDirection.MineToThem, _personId, today.AddDays(2));

        Assert.Equal(_service.ScoreCommitment(a, _now, _options), _service.ScoreCommitment(b, _now, _options));
    }
}
