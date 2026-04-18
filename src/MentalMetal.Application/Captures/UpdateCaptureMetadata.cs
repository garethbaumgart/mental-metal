using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Captures;

public sealed class UpdateCaptureMetadataHandler(
    ICaptureRepository captureRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<CaptureResponse> HandleAsync(
        Guid captureId, UpdateCaptureMetadataRequest request, CancellationToken cancellationToken)
    {
        var capture = await captureRepository.GetByIdAsync(captureId, cancellationToken)
            ?? throw new InvalidOperationException("Capture not found.");

        if (capture.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Capture not found.");

        capture.UpdateMetadata(request.Title);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CaptureResponse.From(capture);
    }
}
