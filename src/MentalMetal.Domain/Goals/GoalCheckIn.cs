namespace MentalMetal.Domain.Goals;

public sealed class GoalCheckIn
{
    public Guid Id { get; private set; }
    public string Note { get; private set; } = null!;
    public int? Progress { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    private GoalCheckIn() { } // EF Core

    public static GoalCheckIn Create(string note, int? progress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(note, nameof(note));

        if (progress is not null && (progress < 0 || progress > 100))
            throw new ArgumentException("Progress must be between 0 and 100.", nameof(progress));

        return new GoalCheckIn
        {
            Id = Guid.NewGuid(),
            Note = note.Trim(),
            Progress = progress,
            RecordedAt = DateTimeOffset.UtcNow,
        };
    }
}
