using MentalMetal.Domain.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;

namespace MentalMetal.Application.Initiatives.Brief;

internal static class BriefOwnership
{
    public static Initiative EnsureOwned(this Initiative? initiative, Guid userId, Guid initiativeId)
    {
        if (initiative is null || initiative.UserId != userId)
            throw new NotFoundException("Initiative", initiativeId);
        return initiative;
    }

    public static PendingBriefUpdate EnsureOwned(this PendingBriefUpdate? update, Guid userId, Guid updateId)
    {
        if (update is null || update.UserId != userId)
            throw new NotFoundException("PendingBriefUpdate", updateId);
        return update;
    }
}
