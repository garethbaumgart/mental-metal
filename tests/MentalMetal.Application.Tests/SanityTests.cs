using NSubstitute;

namespace MentalMetal.Application.Tests;

public class SanityTests
{
    [Fact]
    public void ApplicationTestRunner_Works()
    {
        // Prove NSubstitute resolves
        var mock = Substitute.For<IDisposable>();
        Assert.NotNull(mock);
    }
}
