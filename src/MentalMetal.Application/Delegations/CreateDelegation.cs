using MentalMetal.Application.Common;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Delegations;

public sealed class CreateDelegationHandler(
    IDelegationRepository delegationRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<DelegationResponse> HandleAsync(
        CreateDelegationRequest request, CancellationToken cancellationToken)
    {
        var delegation = Delegation.Create(
            currentUserService.UserId,
            request.Description,
            request.DelegatePersonId,
            request.DueDate,
            request.InitiativeId,
            request.Priority,
            request.SourceCaptureId);

        if (!string.IsNullOrWhiteSpace(request.Notes))
            delegation.UpdateNotes(request.Notes);

        await delegationRepository.AddAsync(delegation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DelegationResponse.From(delegation);
    }
}
