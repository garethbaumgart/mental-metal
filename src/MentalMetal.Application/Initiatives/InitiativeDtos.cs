using MentalMetal.Domain.Initiatives;

namespace MentalMetal.Application.Initiatives;

public sealed record CreateInitiativeRequest(string Title);

public sealed record UpdateTitleRequest(string Title);

public sealed record ChangeStatusRequest(InitiativeStatus NewStatus);

public sealed record InitiativeResponse(
    Guid Id,
    Guid UserId,
    string Title,
    InitiativeStatus Status,
    string? AutoSummary,
    DateTimeOffset? LastSummaryRefreshedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static InitiativeResponse From(Initiative initiative) => new(
        initiative.Id,
        initiative.UserId,
        initiative.Title,
        initiative.Status,
        initiative.AutoSummary,
        initiative.LastSummaryRefreshedAt,
        initiative.CreatedAt,
        initiative.UpdatedAt);
}
