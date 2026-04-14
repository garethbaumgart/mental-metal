using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Delegations;

namespace MentalMetal.Application.MyQueue;

/// <summary>
/// Pure, deterministic scoring for My Queue items. No I/O. See design/D2 for the formula.
/// </summary>
public sealed class QueuePrioritizationService
{
    public int ScoreCommitment(Commitment commitment, DateTimeOffset now, MyQueueOptions options)
    {
        ArgumentNullException.ThrowIfNull(commitment);
        ArgumentNullException.ThrowIfNull(options);

        var score = 0;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var isOverdue =
            commitment.Status == CommitmentStatus.Open
            && commitment.DueDate is not null
            && commitment.DueDate < today;

        if (isOverdue)
        {
            var daysOverdue = today.DayNumber - commitment.DueDate!.Value.DayNumber;
            score += 100 + Math.Min(daysOverdue * 5, 100);
        }
        else if (commitment.DueDate is { } due)
        {
            var daysUntil = due.DayNumber - today.DayNumber;
            if (daysUntil >= 0 && daysUntil <= options.CommitmentDueSoonDays)
            {
                score += Math.Max(0, 50 - (daysUntil * 5));
            }
        }

        if (commitment.DueDate is null)
        {
            score += 10;
        }

        return score;
    }

    public int ScoreDelegation(Delegation delegation, DateTimeOffset now, MyQueueOptions options)
    {
        ArgumentNullException.ThrowIfNull(delegation);
        ArgumentNullException.ThrowIfNull(options);

        var priorityWeight = delegation.Priority switch
        {
            Priority.Urgent => 60,
            Priority.High => 40,
            Priority.Medium => 20,
            Priority.Low => 5,
            _ => 0,
        };

        var score = priorityWeight;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        if (IsDelegationOverdue(delegation, today))
        {
            var daysOverdue = today.DayNumber - delegation.DueDate!.Value.DayNumber;
            score += 80 + Math.Min(daysOverdue * 4, 80);
        }

        var lastTouch = delegation.LastFollowedUpAt ?? delegation.CreatedAt;
        var daysSinceTouch = (int)Math.Floor((now - lastTouch).TotalDays);
        if (daysSinceTouch >= options.DelegationStalenessDays)
        {
            score += Math.Min((daysSinceTouch - options.DelegationStalenessDays) * 3 + 20, 80);
        }

        if (delegation.Status == DelegationStatus.Blocked)
        {
            score += 25;
        }

        return score;
    }

    public int ScoreCapture(Capture capture, DateTimeOffset now, MyQueueOptions options)
    {
        ArgumentNullException.ThrowIfNull(capture);
        ArgumentNullException.ThrowIfNull(options);

        var daysSince = (int)Math.Floor((now - capture.CapturedAt).TotalDays);
        if (daysSince < options.CaptureStalenessDays)
        {
            return 0;
        }

        var score = 30 + Math.Min((daysSince - options.CaptureStalenessDays) * 4, 60);
        if (capture.ProcessingStatus == ProcessingStatus.Failed)
        {
            score += 20;
        }

        return score;
    }

    public static bool IsDelegationOverdue(Delegation delegation, DateOnly today) =>
        delegation.DueDate is { } due
        && due < today
        && delegation.Status != DelegationStatus.Completed;
}
