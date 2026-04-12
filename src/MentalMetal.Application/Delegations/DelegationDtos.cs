using MentalMetal.Domain.Delegations;

namespace MentalMetal.Application.Delegations;

public sealed record CreateDelegationRequest(
    string Description,
    Guid DelegatePersonId,
    DateOnly? DueDate = null,
    Guid? InitiativeId = null,
    Priority? Priority = null,
    Guid? SourceCaptureId = null,
    string? Notes = null);

public sealed record UpdateDelegationRequest(string Description, string? Notes);

public sealed record CompleteDelegationRequest(string? Notes = null);

public sealed record BlockDelegationRequest(string Reason);

public sealed record FollowUpDelegationRequest(string? Notes = null);

public sealed record UpdateDelegationDueDateRequest(DateOnly? DueDate);

public sealed record ReprioritizeDelegationRequest(Priority Priority);

public sealed record ReassignDelegationRequest(Guid DelegatePersonId);

public sealed record DelegationResponse(
    Guid Id,
    Guid UserId,
    string Description,
    Guid DelegatePersonId,
    Guid? InitiativeId,
    Guid? SourceCaptureId,
    DateOnly? DueDate,
    DelegationStatus Status,
    Priority Priority,
    DateTimeOffset? CompletedAt,
    string? Notes,
    DateTimeOffset? LastFollowedUpAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static DelegationResponse From(Delegation delegation) => new(
        delegation.Id,
        delegation.UserId,
        delegation.Description,
        delegation.DelegatePersonId,
        delegation.InitiativeId,
        delegation.SourceCaptureId,
        delegation.DueDate,
        delegation.Status,
        delegation.Priority,
        delegation.CompletedAt,
        delegation.Notes,
        delegation.LastFollowedUpAt,
        delegation.CreatedAt,
        delegation.UpdatedAt);
}
