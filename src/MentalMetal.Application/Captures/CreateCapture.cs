using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Captures;

public sealed class CreateCaptureHandler(
    ICaptureRepository captureRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<CaptureResponse> HandleAsync(
        CreateCaptureRequest request, CancellationToken cancellationToken)
    {
        var capture = Capture.Create(
            currentUserService.UserId,
            request.RawContent,
            request.Type,
            request.Source,
            request.Title);

        await captureRepository.AddAsync(capture, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CaptureResponse.From(capture);
    }
}
