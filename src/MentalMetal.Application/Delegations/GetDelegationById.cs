using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Delegations;

public sealed class GetDelegationByIdHandler(
    IDelegationRepository delegationRepository,
    ICurrentUserService currentUserService)
{
    public async Task<DelegationResponse?> HandleAsync(
        Guid delegationId, CancellationToken cancellationToken)
    {
        var delegation = await delegationRepository.GetByIdAsync(delegationId, cancellationToken);

        if (delegation is null || delegation.UserId != currentUserService.UserId)
            return null;

        return DelegationResponse.From(delegation);
    }
}
