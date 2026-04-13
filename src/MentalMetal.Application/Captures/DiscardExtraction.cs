using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;
namespace MentalMetal.Application.Captures;

public sealed class DiscardExtractionHandler(
    ICaptureRepository captureRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<CaptureResponse> HandleAsync(Guid captureId, CancellationToken cancellationToken)
    {
        var capture = await captureRepository.GetByIdAsync(captureId, cancellationToken);

        if (capture is null || capture.UserId != currentUserService.UserId)
            throw new InvalidOperationException($"Capture not found: {captureId}");

        capture.DiscardExtraction();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CaptureResponse.From(capture);
    }
}
