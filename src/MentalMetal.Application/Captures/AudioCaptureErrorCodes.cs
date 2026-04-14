namespace MentalMetal.Application.Captures;

/// <summary>
/// Canonical error codes for the audio-capture feature. Mirrored exactly in
/// proposal.md, tasks.md, and the capture-audio spec.
/// </summary>
public static class AudioCaptureErrorCodes
{
    public const string AudioInvalidFormat = "audio.invalidFormat";
    public const string AudioTooLarge = "audio.tooLarge";
    public const string AudioUploadFailed = "audio.uploadFailed";
    public const string TranscriptionAudioDiscarded = "transcription.audioDiscarded";
    public const string TranscriptionFailed = "transcription.failed";
    public const string TranscriptionProviderUnavailable = "transcription.providerUnavailable";
    public const string CaptureNotFound = "capture.notFound";
    public const string SpeakerPersonNotFound = "speaker.personNotFound";
    public const string SpeakerLabelNotFound = "speaker.labelNotFound";
}

/// <summary>
/// Thrown by audio-capture handlers to surface a specific error code to the
/// API layer. The endpoint catches this and maps it to an HTTP response.
/// </summary>
public sealed class AudioCaptureException : Exception
{
    public string ErrorCode { get; }

    public AudioCaptureException(string errorCode, string? message = null)
        : base(message ?? errorCode)
    {
        ErrorCode = errorCode;
    }
}
