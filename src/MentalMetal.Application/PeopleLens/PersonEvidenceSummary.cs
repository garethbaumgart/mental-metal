using MentalMetal.Application.Common;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Goals;
using MentalMetal.Domain.Observations;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.PeopleLens;

public sealed record PersonEvidenceSummaryResponse(
    Guid PersonId,
    DateOnly From,
    DateOnly To,
    int ObservationsWin,
    int ObservationsGrowth,
    int ObservationsConcern,
    int ObservationsFeedbackGiven,
    int GoalsAchieved,
    int GoalsMissed,
    int GoalsActive,
    int GoalsDeferred,
    int CommitmentsCompletedOnTime,
    int CommitmentsCompletedLate,
    int CommitmentsOpen,
    int DelegationsCompleted,
    int DelegationsInProgress,
    bool HasAny);

public sealed class GetPersonEvidenceSummaryHandler(
    IObservationRepository observationRepository,
    IGoalRepository goalRepository,
    ICommitmentRepository commitmentRepository,
    IDelegationRepository delegationRepository,
    ICurrentUserService currentUserService)
{
    public async Task<PersonEvidenceSummaryResponse> HandleAsync(
        Guid personId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var fromTs = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var toTs = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));

        var observations = await observationRepository.GetAllAsync(
            userId, personId, null, from, to, cancellationToken);

        var goals = await goalRepository.GetAllAsync(
            userId, personId, null, null, null, null, cancellationToken);

        var commitments = await commitmentRepository.GetAllAsync(
            userId, null, null, personId, null, null, cancellationToken);

        var delegations = await delegationRepository.GetAllAsync(
            userId, null, null, personId, null, cancellationToken);

        var delegationsForPerson = delegations.ToList();

        int win = 0, growth = 0, concern = 0, feedback = 0;
        foreach (var o in observations)
        {
            switch (o.Tag)
            {
                case ObservationTag.Win: win++; break;
                case ObservationTag.Growth: growth++; break;
                case ObservationTag.Concern: concern++; break;
                case ObservationTag.FeedbackGiven: feedback++; break;
            }
        }

        int achieved = 0, missed = 0, active = 0, deferred = 0;
        foreach (var g in goals)
        {
            // filter by window using AchievedAt for achieved, CreatedAt otherwise
            var relevantDate = g.AchievedAt ?? g.UpdatedAt;
            if (relevantDate < fromTs || relevantDate > toTs)
                continue;

            switch (g.Status)
            {
                case GoalStatus.Achieved: achieved++; break;
                case GoalStatus.Missed: missed++; break;
                case GoalStatus.Active: active++; break;
                case GoalStatus.Deferred: deferred++; break;
            }
        }

        int completedOnTime = 0, completedLate = 0, open = 0;
        foreach (var c in commitments)
        {
            if (c.Status == CommitmentStatus.Completed && c.CompletedAt is not null)
            {
                if (c.CompletedAt < fromTs || c.CompletedAt > toTs)
                    continue;

                var completedDate = DateOnly.FromDateTime(c.CompletedAt.Value.UtcDateTime);
                if (c.DueDate is null || completedDate <= c.DueDate)
                    completedOnTime++;
                else
                    completedLate++;
            }
            else if (c.Status == CommitmentStatus.Open)
            {
                open++;
            }
        }

        int delegComplete = 0, delegInProgress = 0;
        foreach (var d in delegationsForPerson)
        {
            if (d.Status == DelegationStatus.Completed && d.CompletedAt is not null)
            {
                if (d.CompletedAt < fromTs || d.CompletedAt > toTs)
                    continue;
                delegComplete++;
            }
            else if (d.Status is DelegationStatus.InProgress or DelegationStatus.Assigned or DelegationStatus.Blocked)
            {
                delegInProgress++;
            }
        }

        var hasAny =
            observations.Count > 0 || achieved + missed + active + deferred > 0 ||
            completedOnTime + completedLate + open > 0 ||
            delegComplete + delegInProgress > 0;

        return new PersonEvidenceSummaryResponse(
            personId, from, to,
            win, growth, concern, feedback,
            achieved, missed, active, deferred,
            completedOnTime, completedLate, open,
            delegComplete, delegInProgress,
            hasAny);
    }
}
