using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives;

public sealed class GetInitiativeHandler(
    IInitiativeRepository initiativeRepository,
    ICurrentUserService currentUserService)
{
    public async Task<InitiativeResponse?> HandleAsync(
        Guid initiativeId, CancellationToken cancellationToken)
    {
        var initiative = await initiativeRepository.GetByIdAsync(initiativeId, cancellationToken);

        if (initiative is null || initiative.UserId != currentUserService.UserId)
            return null;

        return InitiativeResponse.From(initiative);
    }
}
