using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.People;

public sealed class CareerDetails : ValueObject
{
    public string? Level { get; private set; }
    public string? Aspirations { get; private set; }
    public string? GrowthAreas { get; private set; }

    private CareerDetails() { } // EF Core

    public static CareerDetails Create(string? level, string? aspirations, string? growthAreas)
    {
        return new CareerDetails
        {
            Level = level?.Trim(),
            Aspirations = aspirations?.Trim(),
            GrowthAreas = growthAreas?.Trim()
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Level;
        yield return Aspirations;
        yield return GrowthAreas;
    }
}
