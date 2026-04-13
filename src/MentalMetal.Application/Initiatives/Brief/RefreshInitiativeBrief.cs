using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Brief;

public sealed class RefreshInitiativeBriefHandler(
    IInitiativeRepository repo,
    ICurrentUserService currentUser,
    IBriefMaintenanceService brief)
{
    public async Task<Guid> HandleAsync(Guid initiativeId, CancellationToken ct)
    {
        var initiative = (await repo.GetByIdAsync(initiativeId, ct)).EnsureOwned(currentUser.UserId, initiativeId);
        return await brief.RefreshAsync(currentUser.UserId, initiative.Id, ct);
    }
}
