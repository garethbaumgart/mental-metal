using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Brief;

public sealed class ListPendingBriefUpdatesHandler(
    IInitiativeRepository initiativeRepo,
    IPendingBriefUpdateRepository repo,
    ICurrentUserService currentUser)
{
    public async Task<IReadOnlyList<PendingBriefUpdateDto>?> HandleAsync(
        Guid initiativeId, PendingBriefUpdateStatus? statusFilter, CancellationToken ct)
    {
        var initiative = await initiativeRepo.GetByIdAsync(initiativeId, ct);
        if (initiative is null || initiative.UserId != currentUser.UserId) return null;

        var current = initiative.Brief?.BriefVersion ?? 0;
        var list = await repo.ListForInitiativeAsync(currentUser.UserId, initiativeId, statusFilter, ct);
        return list.Select(p => PendingBriefUpdateDto.From(p, current)).ToList();
    }
}
