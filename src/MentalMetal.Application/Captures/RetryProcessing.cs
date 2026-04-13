using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
namespace MentalMetal.Application.Captures;

public sealed class RetryProcessingHandler(
    ICaptureRepository captureRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<CaptureResponse> HandleAsync(Guid captureId, CancellationToken cancellationToken)
    {
        var capture = await captureRepository.GetByIdAsync(captureId, cancellationToken)
            ?? throw new InvalidOperationException($"Capture not found: {captureId}");

        capture.RetryProcessing();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CaptureResponse.From(capture);
    }
}
