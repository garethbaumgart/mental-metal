using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Nudges;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Nudges;

public sealed class MarkNudgeAsNudgedHandler(
    INudgeRepository nudgeRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<NudgeResponse> HandleAsync(Guid id, CancellationToken ct)
    {
        var nudge = await nudgeRepository.GetByIdAsync(id, ct);
        if (nudge is null || nudge.UserId != currentUserService.UserId)
            throw new NotFoundException("Nudge", id);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        nudge.MarkNudged(today);
        await unitOfWork.SaveChangesAsync(ct);
        return NudgeResponse.From(nudge);
    }
}
