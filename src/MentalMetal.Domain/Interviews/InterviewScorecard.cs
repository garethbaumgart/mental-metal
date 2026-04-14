namespace MentalMetal.Domain.Interviews;

public sealed class InterviewScorecard
{
    // Kept in sync with InterviewConfiguration column lengths. Enforced here so invariants
    // are checked at the aggregate boundary rather than relying on the database to reject.
    public const int MaxCompetencyLength = 200;
    public const int MaxNotesLength = 4000;

    public Guid Id { get; private set; }
    public string Competency { get; private set; } = null!;
    public int Rating { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }

    private InterviewScorecard() { } // EF Core

    public static InterviewScorecard Create(string competency, int rating, string? notes, DateTimeOffset recordedAtUtc)
    {
        var (trimmedCompetency, trimmedNotes) = Validate(competency, rating, notes);
        return new InterviewScorecard
        {
            Id = Guid.NewGuid(),
            Competency = trimmedCompetency,
            Rating = rating,
            Notes = trimmedNotes,
            RecordedAtUtc = recordedAtUtc,
        };
    }

    internal void Update(string competency, int rating, string? notes, DateTimeOffset recordedAtUtc)
    {
        var (trimmedCompetency, trimmedNotes) = Validate(competency, rating, notes);
        Competency = trimmedCompetency;
        Rating = rating;
        Notes = trimmedNotes;
        RecordedAtUtc = recordedAtUtc;
    }

    private static (string Competency, string? Notes) Validate(string competency, int rating, string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(competency);
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "Rating must be between 1 and 5.");

        var trimmedCompetency = competency.Trim();
        if (trimmedCompetency.Length > MaxCompetencyLength)
            throw new ArgumentException(
                $"Competency must be {MaxCompetencyLength} characters or fewer.", nameof(competency));

        var trimmedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (trimmedNotes is not null && trimmedNotes.Length > MaxNotesLength)
            throw new ArgumentException(
                $"Notes must be {MaxNotesLength} characters or fewer.", nameof(notes));

        return (trimmedCompetency, trimmedNotes);
    }
}
