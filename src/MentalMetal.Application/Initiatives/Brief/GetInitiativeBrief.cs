using MentalMetal.Application.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Brief;

public sealed class GetInitiativeBriefHandler(
    IInitiativeRepository repo,
    ICurrentUserService currentUser)
{
    public async Task<LivingBriefDto?> HandleAsync(Guid initiativeId, CancellationToken ct)
    {
        var initiative = await repo.GetByIdAsync(initiativeId, ct);
        if (initiative is null || initiative.UserId != currentUser.UserId) return null;
        return LivingBriefDto.From(initiative);
    }
}
