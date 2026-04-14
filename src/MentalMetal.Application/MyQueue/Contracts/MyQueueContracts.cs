using System.Text.Json.Serialization;

namespace MentalMetal.Application.MyQueue.Contracts;

/// <summary>
/// Discriminates queue items by source aggregate.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QueueItemType
{
    Commitment,
    Delegation,
    Capture
}

/// <summary>
/// Scope filter for the queue endpoint.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QueueScope
{
    All,
    Overdue,
    Today,
    ThisWeek
}

public sealed record QueueItemResponse(
    QueueItemType ItemType,
    Guid Id,
    string Title,
    string Status,
    DateOnly? DueDate,
    bool IsOverdue,
    Guid? PersonId,
    string? PersonName,
    Guid? InitiativeId,
    string? InitiativeName,
    int? DaysSinceCaptured,
    DateTimeOffset? LastFollowedUpAt,
    int PriorityScore,
    bool SuggestDelegate);

public sealed record QueueCountsResponse(
    int Overdue,
    int DueSoon,
    int StaleCaptures,
    int StaleDelegations,
    int Total);

public sealed record QueueFiltersResponse(
    QueueScope Scope,
    IReadOnlyList<QueueItemType> ItemType,
    Guid? PersonId,
    Guid? InitiativeId);

public sealed record MyQueueResponse(
    IReadOnlyList<QueueItemResponse> Items,
    QueueCountsResponse Counts,
    QueueFiltersResponse Filters);
