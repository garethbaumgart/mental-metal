namespace MentalMetal.Application.Common.Ai;

/// <summary>
/// Transcribes an audio stream to text plus speaker-labeled segments.
/// Implementations are registered behind the existing AI-provider abstraction
/// pattern. Failure SHOULD be signaled via exceptions — handlers translate
/// those into the <c>transcription.failed</c> / <c>transcription.providerUnavailable</c>
/// error codes.
/// </summary>
public interface IAudioTranscriptionProvider
{
    Task<AudioTranscriptionResult> TranscribeAsync(
        AudioTranscriptionRequest request,
        CancellationToken cancellationToken);
}

public sealed record AudioTranscriptionRequest(
    Stream AudioStream,
    string MimeType,
    double DurationSeconds);

public sealed record AudioTranscriptionResult(
    string FullText,
    IReadOnlyList<AudioTranscriptSegmentDto> Segments);

public sealed record AudioTranscriptSegmentDto(
    double StartSeconds,
    double EndSeconds,
    string SpeakerLabel,
    string Text);
