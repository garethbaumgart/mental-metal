using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Brief;

public sealed class RejectPendingBriefUpdateHandler(
    IPendingBriefUpdateRepository repo,
    ICurrentUserService currentUser,
    IUnitOfWork uow)
{
    public async Task HandleAsync(Guid initiativeId, Guid updateId, RejectPendingUpdateRequest? request, CancellationToken ct)
    {
        var pending = (await repo.GetByIdAsync(updateId, ct)).EnsureOwned(currentUser.UserId, updateId);
        if (pending.InitiativeId != initiativeId)
            throw new NotFoundException("PendingBriefUpdate", updateId);
        pending.Reject(request?.Reason);
        await uow.SaveChangesAsync(ct);
    }
}
