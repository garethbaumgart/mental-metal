using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Delegations;

public sealed class GetUserDelegationsHandler(
    IDelegationRepository delegationRepository,
    ICurrentUserService currentUserService)
{
    public async Task<List<DelegationResponse>> HandleAsync(
        DelegationStatus? statusFilter,
        Priority? priorityFilter,
        Guid? delegatePersonIdFilter,
        Guid? initiativeIdFilter,
        CancellationToken cancellationToken)
    {
        var delegations = await delegationRepository.GetAllAsync(
            currentUserService.UserId,
            statusFilter,
            priorityFilter,
            delegatePersonIdFilter,
            initiativeIdFilter,
            cancellationToken);

        return delegations.Select(DelegationResponse.From).ToList();
    }
}
