using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Captures.ImportCapture;

public sealed class ImportCaptureFromJsonHandler(
    ICaptureRepository captureRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<ImportCaptureResponse> HandleAsync(
        ImportCaptureFromJsonRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Content, nameof(request.Content));

        if (request.Type is not (CaptureType.Transcript or CaptureType.QuickNote))
            throw new ArgumentException($"Unsupported capture type for import: {request.Type}");

        var detected = TranscriptFormatDetector.Detect(request.Content);

        var source = request.SourceUrl is not null ? CaptureSource.Bookmarklet : (CaptureSource?)null;
        var capture = Capture.Create(
            currentUserService.UserId,
            detected.NormalizedContent,
            request.Type,
            source,
            request.Title);

        await captureRepository.AddAsync(capture, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportCaptureResponse(capture.Id);
    }
}
