using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Brief;

public sealed class ResolveInitiativeBriefRiskHandler(
    IInitiativeRepository repo,
    ICurrentUserService currentUser,
    IUnitOfWork uow)
{
    public async Task<LivingBriefDto> HandleAsync(Guid initiativeId, Guid riskId, ResolveRiskRequest request, CancellationToken ct)
    {
        var initiative = (await repo.GetByIdAsync(initiativeId, ct)).EnsureOwned(currentUser.UserId, initiativeId);
        initiative.ResolveRisk(riskId, request?.ResolutionNote);
        await uow.SaveChangesAsync(ct);
        return LivingBriefDto.From(initiative);
    }
}
