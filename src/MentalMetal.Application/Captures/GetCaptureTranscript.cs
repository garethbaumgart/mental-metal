using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Captures;

public sealed record TranscriptSegmentResponse(
    double StartSeconds,
    double EndSeconds,
    string SpeakerLabel,
    string Text,
    Guid? LinkedPersonId);

public sealed record GetCaptureTranscriptResponse(
    Guid CaptureId,
    TranscriptionStatus TranscriptionStatus,
    IReadOnlyList<TranscriptSegmentResponse> Segments);

public sealed class GetCaptureTranscriptHandler(
    ICaptureRepository captureRepository,
    ICurrentUserService currentUserService)
{
    public async Task<GetCaptureTranscriptResponse?> HandleAsync(
        Guid captureId, CancellationToken cancellationToken)
    {
        var capture = await captureRepository.GetByIdAsync(captureId, cancellationToken);
        if (capture is null || capture.UserId != currentUserService.UserId)
            return null;

        var segments = capture.TranscriptSegments
            .OrderBy(s => s.StartSeconds)
            .Select(s => new TranscriptSegmentResponse(
                s.StartSeconds, s.EndSeconds, s.SpeakerLabel, s.Text, s.LinkedPersonId))
            .ToList();

        return new GetCaptureTranscriptResponse(capture.Id, capture.TranscriptionStatus, segments);
    }
}
