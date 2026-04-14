namespace MentalMetal.Application.Tests.Briefings;

public sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _current = start;
    public override DateTimeOffset GetUtcNow() => _current;
    public void Advance(TimeSpan by) => _current = _current.Add(by);
}
