namespace MentalMetal.Domain.OneOnOnes;

public sealed class FollowUp
{
    public Guid Id { get; private set; }
    public string Description { get; private set; } = null!;
    public bool Resolved { get; private set; }

    private FollowUp() { } // EF Core

    public static FollowUp Create(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));

        return new FollowUp
        {
            Id = Guid.NewGuid(),
            Description = description.Trim(),
            Resolved = false,
        };
    }

    internal void MarkResolved() => Resolved = true;
}
