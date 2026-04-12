using MentalMetal.Application.Common;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Delegations;

public sealed class StartDelegationHandler(
    IDelegationRepository delegationRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<DelegationResponse> HandleAsync(
        Guid delegationId, CancellationToken cancellationToken)
    {
        var delegation = await delegationRepository.GetByIdAsync(delegationId, cancellationToken)
            ?? throw new InvalidOperationException("Delegation not found.");

        if (delegation.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Delegation not found.");

        delegation.MarkInProgress();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DelegationResponse.From(delegation);
    }
}
