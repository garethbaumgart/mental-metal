namespace MentalMetal.Domain.Captures;

/// <summary>
/// Lifecycle of audio-transcription for a <see cref="Capture"/>.
/// Independent of <see cref="ProcessingStatus"/> (which governs AI extraction).
/// Non-audio captures stay on <see cref="NotApplicable"/> for their entire lifetime.
/// </summary>
public enum TranscriptionStatus
{
    NotApplicable,
    Pending,
    InProgress,
    Transcribed,
    Failed
}
