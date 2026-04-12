using MentalMetal.Domain.Initiatives;

namespace MentalMetal.Application.Initiatives;

public sealed record CreateInitiativeRequest(string Title);

public sealed record UpdateTitleRequest(string Title);

public sealed record ChangeStatusRequest(InitiativeStatus NewStatus);

public sealed record MilestoneRequest(string Title, DateOnly TargetDate, string? Description);

public sealed record LinkPersonRequest(Guid PersonId);

public sealed record MilestoneResponse(
    Guid Id,
    string Title,
    DateOnly TargetDate,
    string? Description,
    bool IsCompleted);

public sealed record InitiativeResponse(
    Guid Id,
    Guid UserId,
    string Title,
    InitiativeStatus Status,
    List<MilestoneResponse> Milestones,
    List<Guid> LinkedPersonIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static InitiativeResponse From(Initiative initiative) => new(
        initiative.Id,
        initiative.UserId,
        initiative.Title,
        initiative.Status,
        initiative.Milestones.Select(m => new MilestoneResponse(
            m.Id,
            m.Title,
            m.TargetDate,
            m.Description,
            m.IsCompleted)).ToList(),
        initiative.LinkedPersonIds.ToList(),
        initiative.CreatedAt,
        initiative.UpdatedAt);
}
