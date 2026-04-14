namespace MentalMetal.Domain.Interviews;

public sealed class InterviewScorecard
{
    public Guid Id { get; private set; }
    public string Competency { get; private set; } = null!;
    public int Rating { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }

    private InterviewScorecard() { } // EF Core

    public static InterviewScorecard Create(string competency, int rating, string? notes, DateTimeOffset recordedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(competency);
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "Rating must be between 1 and 5.");

        return new InterviewScorecard
        {
            Id = Guid.NewGuid(),
            Competency = competency.Trim(),
            Rating = rating,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            RecordedAtUtc = recordedAtUtc,
        };
    }

    internal void Update(string competency, int rating, string? notes, DateTimeOffset recordedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(competency);
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "Rating must be between 1 and 5.");

        Competency = competency.Trim();
        Rating = rating;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        RecordedAtUtc = recordedAtUtc;
    }
}
