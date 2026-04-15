using MentalMetal.Domain.OneOnOnes;

namespace MentalMetal.Application.OneOnOnes;

public sealed record CreateOneOnOneRequest(
    Guid PersonId,
    DateOnly? OccurredAt,
    string? Notes = null,
    IReadOnlyList<string>? Topics = null,
    int? MoodRating = null);

public sealed record UpdateOneOnOneRequest(
    string? Notes,
    IReadOnlyList<string>? Topics,
    int? MoodRating);

public sealed record AddActionItemRequest(string Description);

public sealed record AddFollowUpRequest(string Description);

public sealed record ActionItemResponse(Guid Id, string Description, bool Completed)
{
    public static ActionItemResponse From(ActionItem item) =>
        new(item.Id, item.Description, item.Completed);
}

public sealed record FollowUpResponse(Guid Id, string Description, bool Resolved)
{
    public static FollowUpResponse From(FollowUp followUp) =>
        new(followUp.Id, followUp.Description, followUp.Resolved);
}

public sealed record OneOnOneResponse(
    Guid Id,
    Guid UserId,
    Guid PersonId,
    DateOnly OccurredAt,
    string? Notes,
    int? MoodRating,
    IReadOnlyList<string> Topics,
    IReadOnlyList<ActionItemResponse> ActionItems,
    IReadOnlyList<FollowUpResponse> FollowUps,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static OneOnOneResponse From(OneOnOne o) => new(
        o.Id,
        o.UserId,
        o.PersonId,
        o.OccurredAt,
        o.Notes,
        o.MoodRating,
        o.Topics.ToList(),
        o.ActionItems.Select(ActionItemResponse.From).ToList(),
        o.FollowUps.Select(FollowUpResponse.From).ToList(),
        o.CreatedAt,
        o.UpdatedAt);
}
