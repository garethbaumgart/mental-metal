using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Brief;

public sealed class GetPendingBriefUpdateHandler(
    IInitiativeRepository initiativeRepo,
    IPendingBriefUpdateRepository repo,
    ICurrentUserService currentUser)
{
    public async Task<PendingBriefUpdateDto?> HandleAsync(Guid updateId, CancellationToken ct)
    {
        var p = await repo.GetByIdAsync(updateId, ct);
        if (p is null || p.UserId != currentUser.UserId) return null;
        var initiative = await initiativeRepo.GetByIdAsync(p.InitiativeId, ct);
        var current = initiative?.Brief?.BriefVersion ?? p.BriefVersionAtProposal;
        return PendingBriefUpdateDto.From(p, current);
    }
}
