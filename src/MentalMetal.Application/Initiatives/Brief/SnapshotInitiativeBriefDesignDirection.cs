using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Brief;

public sealed class SnapshotInitiativeBriefDesignDirectionHandler(
    IInitiativeRepository repo,
    ICurrentUserService currentUser,
    IUnitOfWork uow)
{
    public async Task<LivingBriefDto> HandleAsync(Guid initiativeId, SnapshotRequest request, CancellationToken ct)
    {
        var initiative = (await repo.GetByIdAsync(initiativeId, ct)).EnsureOwned(currentUser.UserId, initiativeId);
        initiative.SnapshotDesignDirection(request.Content, BriefSource.Manual, []);
        await uow.SaveChangesAsync(ct);
        return LivingBriefDto.From(initiative);
    }
}
