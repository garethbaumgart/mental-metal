namespace MentalMetal.Domain.OneOnOnes;

public sealed class ActionItem
{
    public Guid Id { get; private set; }
    public string Description { get; private set; } = null!;
    public bool Completed { get; private set; }

    private ActionItem() { } // EF Core

    public static ActionItem Create(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));

        return new ActionItem
        {
            Id = Guid.NewGuid(),
            Description = description.Trim(),
            Completed = false,
        };
    }

    internal void MarkCompleted() => Completed = true;
}
