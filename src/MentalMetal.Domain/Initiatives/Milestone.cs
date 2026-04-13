using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Initiatives;

public sealed class Milestone : ValueObject
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public DateOnly TargetDate { get; private set; }
    public string? Description { get; private set; }
    public bool IsCompleted { get; private set; }

    private Milestone() { } // EF Core

    public static Milestone Create(string title, DateOnly targetDate, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        return new Milestone
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            TargetDate = targetDate,
            Description = description?.Trim(),
            IsCompleted = false
        };
    }

    internal void Complete()
    {
        IsCompleted = true;
    }

    internal void ApplyUpdates(string title, DateOnly targetDate, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        Title = title.Trim();
        TargetDate = targetDate;
        Description = description?.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return Title;
        yield return TargetDate;
        yield return Description;
        yield return IsCompleted;
    }
}
