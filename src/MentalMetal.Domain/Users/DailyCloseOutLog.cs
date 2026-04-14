namespace MentalMetal.Domain.Users;

/// <summary>
/// Owned entity on <see cref="User"/> capturing a single day's close-out snapshot.
/// Keyed by (UserId, Date). Idempotent — recording the same date overwrites.
/// </summary>
public sealed class DailyCloseOutLog
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public DateTimeOffset ClosedAtUtc { get; private set; }
    public int ConfirmedCount { get; private set; }
    public int DiscardedCount { get; private set; }
    public int RemainingCount { get; private set; }

    private DailyCloseOutLog() { } // EF Core

    internal DailyCloseOutLog(
        DateOnly date,
        DateTimeOffset closedAtUtc,
        int confirmedCount,
        int discardedCount,
        int remainingCount)
    {
        ValidateNonNegative(confirmedCount, nameof(confirmedCount));
        ValidateNonNegative(discardedCount, nameof(discardedCount));
        ValidateNonNegative(remainingCount, nameof(remainingCount));

        Id = Guid.NewGuid();
        Date = date;
        ClosedAtUtc = closedAtUtc;
        ConfirmedCount = confirmedCount;
        DiscardedCount = discardedCount;
        RemainingCount = remainingCount;
    }

    internal void Overwrite(
        DateTimeOffset closedAtUtc,
        int confirmedCount,
        int discardedCount,
        int remainingCount)
    {
        ValidateNonNegative(confirmedCount, nameof(confirmedCount));
        ValidateNonNegative(discardedCount, nameof(discardedCount));
        ValidateNonNegative(remainingCount, nameof(remainingCount));

        ClosedAtUtc = closedAtUtc;
        ConfirmedCount = confirmedCount;
        DiscardedCount = discardedCount;
        RemainingCount = remainingCount;
    }

    private static void ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(paramName, "Count cannot be negative.");
    }
}
