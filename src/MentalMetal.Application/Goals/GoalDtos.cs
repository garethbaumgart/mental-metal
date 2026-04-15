using MentalMetal.Domain.Goals;

namespace MentalMetal.Application.Goals;

public sealed record CreateGoalRequest(
    Guid PersonId,
    string Title,
    GoalType GoalType,
    string? Description = null,
    DateOnly? TargetDate = null);

public sealed record UpdateGoalRequest(
    string Title,
    string? Description,
    DateOnly? TargetDate);

public sealed record DeferGoalRequest(string? Reason = null);

public sealed record RecordCheckInRequest(string Note, int? Progress = null);

public sealed record GoalCheckInResponse(Guid Id, string Note, int? Progress, DateTimeOffset RecordedAt)
{
    public static GoalCheckInResponse From(GoalCheckIn c) =>
        new(c.Id, c.Note, c.Progress, c.RecordedAt);
}

public sealed record GoalResponse(
    Guid Id,
    Guid UserId,
    Guid PersonId,
    string? PersonName,
    string Title,
    string? Description,
    GoalType GoalType,
    GoalStatus Status,
    DateOnly? TargetDate,
    string? DeferralReason,
    DateTimeOffset? AchievedAt,
    IReadOnlyList<GoalCheckInResponse> CheckIns,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static GoalResponse From(Goal g, string? personName = null) => new(
        g.Id,
        g.UserId,
        g.PersonId,
        personName,
        g.Title,
        g.Description,
        g.Type,
        g.Status,
        g.TargetDate,
        g.DeferralReason,
        g.AchievedAt,
        g.CheckIns.OrderByDescending(c => c.RecordedAt).Select(GoalCheckInResponse.From).ToList(),
        g.CreatedAt,
        g.UpdatedAt);
}
