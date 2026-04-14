using MentalMetal.Application.Captures;
using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.DailyCloseOut;

public sealed class QuickDiscardCaptureHandler(
    ICaptureRepository captureRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(Guid captureId, CancellationToken cancellationToken)
    {
        var capture = (await captureRepository.GetByIdAsync(captureId, cancellationToken))
            .EnsureOwned(currentUserService.UserId, captureId);

        capture.QuickDiscard();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
