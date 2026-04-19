using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Logging;

namespace MentalMetal.Application.Captures;

public sealed class TranscribeCaptureHandler(
    ICaptureRepository captureRepository,
    ICurrentUserService currentUserService,
    IAudioBlobStore blobStore,
    ITranscriptionProviderFactory transcriptionProviderFactory,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<TranscribeCaptureHandler> logger)
{
    public async Task<CaptureResponse> HandleAsync(Guid captureId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var capture = await captureRepository.GetByIdWithTranscriptAsync(captureId, cancellationToken);
        if (capture is null || capture.UserId != userId)
            throw new AudioCaptureException(AudioCaptureErrorCodes.CaptureNotFound);

        if (capture.CaptureType != CaptureType.AudioRecording)
            throw new AudioCaptureException(AudioCaptureErrorCodes.CaptureNotFound);

        if (capture.AudioDiscardedAt is not null || string.IsNullOrWhiteSpace(capture.AudioBlobRef))
            throw new AudioCaptureException(AudioCaptureErrorCodes.TranscriptionAudioDiscarded);

        if (capture.TranscriptionStatus != TranscriptionStatus.Failed)
            throw new AudioCaptureException(
                AudioCaptureErrorCodes.TranscriptionFailed,
                $"Cannot retry transcription in status '{capture.TranscriptionStatus}'.");

        capture.RequeueTranscription(timeProvider.GetUtcNow());
        capture.BeginTranscription(timeProvider.GetUtcNow());

        try
        {
            IAudioTranscriptionProvider transcriptionProvider;
            try
            {
                transcriptionProvider = await transcriptionProviderFactory.CreateAsync(cancellationToken);
            }
            catch (AudioTranscriptionUnavailableException ex)
            {
                logger.LogWarning(ex, "Transcription provider not configured for capture {CaptureId}", capture.Id);
                capture.MarkTranscriptionFailed(ex.Message, timeProvider.GetUtcNow());
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw new AudioCaptureException(AudioCaptureErrorCodes.TranscriptionNotConfigured, ex.Message);
            }

            await using var stream = await blobStore.OpenReadAsync(capture.AudioBlobRef!, cancellationToken);
            var transcription = await transcriptionProvider.TranscribeAsync(
                new AudioTranscriptionRequest(stream, capture.AudioMimeType ?? "application/octet-stream", capture.AudioDurationSeconds ?? 0),
                cancellationToken);

            var segments = TranscriptSegmentSplitter.Split(transcription.Segments);
            capture.AttachTranscript(transcription.FullText, segments, timeProvider.GetUtcNow());
            foreach (var s in segments)
                captureRepository.MarkOwnedAdded(s);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Retry transcription failed for capture {CaptureId}", capture.Id);
            capture.MarkTranscriptionFailed(ex.Message, timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var code = ex is AudioTranscriptionUnavailableException
                ? AudioCaptureErrorCodes.TranscriptionProviderUnavailable
                : AudioCaptureErrorCodes.TranscriptionFailed;
            throw new AudioCaptureException(code, ex.Message);
        }

        var deleted = false;
        try
        {
            await blobStore.DeleteAsync(capture.AudioBlobRef!, cancellationToken);
            deleted = true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete audio blob after successful retry; capture {CaptureId} continues with blob retained", capture.Id);
        }

        if (deleted)
            capture.MarkAudioDiscarded(timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CaptureResponse.From(capture);
    }
}
