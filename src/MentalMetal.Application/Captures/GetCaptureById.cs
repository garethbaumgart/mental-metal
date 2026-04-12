using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Captures;

public sealed class GetCaptureByIdHandler(
    ICaptureRepository captureRepository,
    ICurrentUserService currentUserService)
{
    public async Task<CaptureResponse?> HandleAsync(
        Guid captureId, CancellationToken cancellationToken)
    {
        var capture = await captureRepository.GetByIdAsync(captureId, cancellationToken);

        if (capture is null || capture.UserId != currentUserService.UserId)
            return null;

        return CaptureResponse.From(capture);
    }
}
