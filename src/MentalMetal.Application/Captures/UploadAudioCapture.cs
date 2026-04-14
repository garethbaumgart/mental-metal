using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Logging;

namespace MentalMetal.Application.Captures;

public sealed record UploadAudioCaptureRequest(
    Stream Audio,
    string MimeType,
    double DurationSeconds,
    string? Title = null,
    string? Source = null);

public sealed class UploadAudioCaptureHandler(
    ICaptureRepository captureRepository,
    ICurrentUserService currentUserService,
    IAudioBlobStore blobStore,
    IAudioTranscriptionProvider transcriptionProvider,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<UploadAudioCaptureHandler> logger)
{
    public async Task<CaptureResponse> HandleAsync(
        UploadAudioCaptureRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Audio);

        var userId = currentUserService.UserId;
        var now = timeProvider.GetUtcNow();

        // 1. Persist the audio blob FIRST so we have a reference for the aggregate.
        string blobRef;
        try
        {
            blobRef = await blobStore.SaveAsync(userId, request.Audio, request.MimeType, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist audio blob for user {UserId}", userId);
            throw new AudioCaptureException(AudioCaptureErrorCodes.AudioUploadFailed, ex.Message);
        }

        var capture = Capture.CreateAudio(
            userId, blobRef, request.MimeType, request.DurationSeconds, now, request.Source, request.Title);
        await captureRepository.AddAsync(capture, cancellationToken);

        // 2. Move to InProgress and transcribe synchronously.
        capture.BeginTranscription(timeProvider.GetUtcNow());

        try
        {
            await using var blobStream = await blobStore.OpenReadAsync(blobRef, cancellationToken);
            var transcription = await transcriptionProvider.TranscribeAsync(
                new AudioTranscriptionRequest(blobStream, request.MimeType, request.DurationSeconds),
                cancellationToken);

            var segments = TranscriptSegmentSplitter.Split(transcription.Segments);
            capture.AttachTranscript(transcription.FullText, segments, timeProvider.GetUtcNow());

            foreach (var segment in segments)
                captureRepository.MarkOwnedAdded(segment);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Transcription failed for capture {CaptureId}", capture.Id);
            capture.MarkTranscriptionFailed(ex.Message, timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var code = ex is AudioTranscriptionUnavailableException
                ? AudioCaptureErrorCodes.TranscriptionProviderUnavailable
                : AudioCaptureErrorCodes.TranscriptionFailed;
            throw new AudioCaptureException(code, ex.Message);
        }

        // 3. Discard the blob after successful transcription (infra concern, not aggregate).
        try
        {
            await blobStore.DeleteAsync(blobRef, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete audio blob {BlobRef} after transcription; capture {CaptureId} continues", blobRef, capture.Id);
            // Swallow — orphan cleanup is future work.
        }

        capture.MarkAudioDiscarded(timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CaptureResponse.From(capture);
    }
}

/// <summary>
/// Signals that the transcription provider was unreachable (vs. returned an
/// error for a specific file). Handlers translate this into the
/// <c>transcription.providerUnavailable</c> error code.
/// </summary>
public sealed class AudioTranscriptionUnavailableException(string? message = null, Exception? inner = null)
    : Exception(message ?? "Audio transcription provider unavailable.", inner);
