using MentalMetal.Domain.Common;
using MentalMetal.Domain.Nudges;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Nudges;

public sealed class GetNudgeHandler(
    INudgeRepository nudgeRepository,
    ICurrentUserService currentUserService)
{
    public async Task<NudgeResponse> HandleAsync(Guid id, CancellationToken ct)
    {
        var nudge = await nudgeRepository.GetByIdAsync(id, ct);
        if (nudge is null || nudge.UserId != currentUserService.UserId)
            throw new NotFoundException("Nudge", id);
        return NudgeResponse.From(nudge);
    }
}
