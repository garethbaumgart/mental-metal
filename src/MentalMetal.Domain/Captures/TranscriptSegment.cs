namespace MentalMetal.Domain.Captures;

/// <summary>
/// A single diarized segment of a transcribed audio capture.
/// Owned by <see cref="Capture"/> — never referenced independently.
/// </summary>
public sealed class TranscriptSegment
{
    public const int MaxTextLength = 2000;
    public const int MaxSpeakerLabelLength = 64;

    public Guid Id { get; private set; }
    public double StartSeconds { get; private set; }
    public double EndSeconds { get; private set; }
    public string SpeakerLabel { get; private set; } = null!;
    public string Text { get; private set; } = null!;
    public Guid? LinkedPersonId { get; private set; }

    private TranscriptSegment() { } // EF Core

    public static TranscriptSegment Create(
        double startSeconds,
        double endSeconds,
        string speakerLabel,
        string text,
        Guid? linkedPersonId = null)
    {
        if (startSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(startSeconds), "StartSeconds must be non-negative.");
        if (endSeconds < startSeconds)
            throw new ArgumentException("EndSeconds must be greater than or equal to StartSeconds.", nameof(endSeconds));

        ArgumentException.ThrowIfNullOrWhiteSpace(speakerLabel, nameof(speakerLabel));
        if (speakerLabel.Length > MaxSpeakerLabelLength)
            throw new ArgumentException(
                $"SpeakerLabel must be {MaxSpeakerLabelLength} characters or fewer.", nameof(speakerLabel));

        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));
        if (text.Length > MaxTextLength)
            throw new ArgumentException(
                $"Text must be {MaxTextLength} characters or fewer.", nameof(text));

        return new TranscriptSegment
        {
            Id = Guid.NewGuid(),
            StartSeconds = startSeconds,
            EndSeconds = endSeconds,
            SpeakerLabel = speakerLabel.Trim(),
            Text = text,
            LinkedPersonId = linkedPersonId
        };
    }

    internal void LinkToPerson(Guid personId)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));
        LinkedPersonId = personId;
    }
}
