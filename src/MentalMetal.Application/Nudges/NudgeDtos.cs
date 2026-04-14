using MentalMetal.Domain.Nudges;

namespace MentalMetal.Application.Nudges;

public sealed record CreateNudgeRequest(
    string Title,
    CadenceType CadenceType,
    int? CustomIntervalDays = null,
    DayOfWeek? DayOfWeek = null,
    int? DayOfMonth = null,
    DateOnly? StartDate = null,
    Guid? PersonId = null,
    Guid? InitiativeId = null,
    string? Notes = null);

public sealed record UpdateNudgeRequest(
    string Title,
    string? Notes,
    Guid? PersonId,
    Guid? InitiativeId);

public sealed record UpdateCadenceRequest(
    CadenceType CadenceType,
    int? CustomIntervalDays = null,
    DayOfWeek? DayOfWeek = null,
    int? DayOfMonth = null);

public sealed record ListNudgesFilters(
    bool? IsActive = null,
    Guid? PersonId = null,
    Guid? InitiativeId = null,
    DateOnly? DueBefore = null,
    int? DueWithinDays = null);

public sealed record NudgeCadenceResponse(
    CadenceType Type,
    int? CustomIntervalDays,
    DayOfWeek? DayOfWeek,
    int? DayOfMonth);

public sealed record NudgeResponse(
    Guid Id,
    Guid UserId,
    string Title,
    NudgeCadenceResponse Cadence,
    DateOnly StartDate,
    DateOnly? NextDueDate,
    DateTimeOffset? LastNudgedAt,
    Guid? PersonId,
    Guid? InitiativeId,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static NudgeResponse From(Nudge nudge) => new(
        nudge.Id,
        nudge.UserId,
        nudge.Title,
        new NudgeCadenceResponse(
            nudge.Cadence.Type,
            nudge.Cadence.CustomIntervalDays,
            nudge.Cadence.DayOfWeek,
            nudge.Cadence.DayOfMonth),
        nudge.StartDate,
        nudge.NextDueDate,
        nudge.LastNudgedAt,
        nudge.PersonId,
        nudge.InitiativeId,
        nudge.Notes,
        nudge.IsActive,
        nudge.CreatedAtUtc,
        nudge.UpdatedAtUtc);
}
