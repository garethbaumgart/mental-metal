using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.OneOnOnes;

public sealed class MoodRating : ValueObject
{
    public int Value { get; }

    private MoodRating(int value) => Value = value;

    public static MoodRating Create(int value)
    {
        if (value < 1 || value > 5)
            throw new ArgumentException("MoodRating must be between 1 and 5.", nameof(value));

        return new MoodRating(value);
    }

    public static MoodRating? CreateNullable(int? value) =>
        value is null ? null : Create(value.Value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
