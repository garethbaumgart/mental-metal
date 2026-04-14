using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Nudges;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Nudges;

public sealed class DeleteNudgeHandler(
    INudgeRepository nudgeRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(Guid id, CancellationToken ct)
    {
        var nudge = await nudgeRepository.GetByIdAsync(id, ct);
        if (nudge is null || nudge.UserId != currentUserService.UserId)
            throw new NotFoundException("Nudge", id);

        nudge.MarkDeleted();
        nudgeRepository.Remove(nudge);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
