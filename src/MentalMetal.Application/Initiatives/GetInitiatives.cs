using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives;

public sealed class GetInitiativesHandler(
    IInitiativeRepository initiativeRepository,
    ICurrentUserService currentUserService)
{
    public async Task<List<InitiativeResponse>> HandleAsync(
        InitiativeStatus? statusFilter, CancellationToken cancellationToken)
    {
        var initiatives = await initiativeRepository.GetAllAsync(
            currentUserService.UserId, statusFilter, cancellationToken);

        return initiatives.Select(InitiativeResponse.From).ToList();
    }
}
