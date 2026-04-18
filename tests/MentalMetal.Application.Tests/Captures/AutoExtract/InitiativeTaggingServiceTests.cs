using MentalMetal.Application.Captures.AutoExtract;
using MentalMetal.Domain.Initiatives;

namespace MentalMetal.Application.Tests.Captures.AutoExtract;

public class InitiativeTaggingServiceTests
{
    private readonly InitiativeTaggingService _sut = new();
    private readonly Guid _userId = Guid.NewGuid();

    private Initiative CreateInitiative(string title)
    {
        return Initiative.Create(_userId, title);
    }

    [Fact]
    public void Resolve_ExactMatch_ReturnsInitiativeId()
    {
        var proj = CreateInitiative("Project Alpha");
        var initiatives = new List<Initiative> { proj };

        var result = _sut.Resolve(["Project Alpha"], initiatives);

        Assert.Equal(proj.Id, result["Project Alpha"]);
    }

    [Fact]
    public void Resolve_CaseInsensitiveContains_ReturnsInitiativeId()
    {
        var proj = CreateInitiative("Project Alpha");
        var initiatives = new List<Initiative> { proj };

        var result = _sut.Resolve(["project alpha"], initiatives);

        Assert.Equal(proj.Id, result["project alpha"]);
    }

    [Fact]
    public void Resolve_SubstringMatch_ReturnsInitiativeId()
    {
        var proj = CreateInitiative("Project Alpha");
        var initiatives = new List<Initiative> { proj };

        var result = _sut.Resolve(["Alpha"], initiatives);

        Assert.Equal(proj.Id, result["Alpha"]);
    }

    [Fact]
    public void Resolve_Ambiguous_ReturnsNull()
    {
        var proj1 = CreateInitiative("Alpha Project");
        var proj2 = CreateInitiative("Alpha Initiative");
        var initiatives = new List<Initiative> { proj1, proj2 };

        var result = _sut.Resolve(["Alpha"], initiatives);

        Assert.Null(result["Alpha"]);
    }

    [Fact]
    public void Resolve_NoMatch_ReturnsNull()
    {
        var proj = CreateInitiative("Project Alpha");
        var initiatives = new List<Initiative> { proj };

        var result = _sut.Resolve(["Completely Different"], initiatives);

        Assert.Null(result["Completely Different"]);
    }

    [Fact]
    public void Resolve_ShortName_ReturnsNull()
    {
        var proj = CreateInitiative("AB");
        var initiatives = new List<Initiative> { proj };

        // Raw name is too short (< 3 chars)
        var result = _sut.Resolve(["AB"], initiatives);

        Assert.Null(result["AB"]);
    }

    [Fact]
    public void Resolve_EmptyName_ReturnsNull()
    {
        var initiatives = new List<Initiative> { CreateInitiative("Project Alpha") };

        var result = _sut.Resolve([""], initiatives);

        Assert.Null(result[""]);
    }

    [Fact]
    public void Resolve_MultipleNames_ResolvesEach()
    {
        var alpha = CreateInitiative("Project Alpha");
        var beta = CreateInitiative("Project Beta");
        var initiatives = new List<Initiative> { alpha, beta };

        var result = _sut.Resolve(["Project Alpha", "Project Beta", "Gamma"], initiatives);

        Assert.Equal(alpha.Id, result["Project Alpha"]);
        Assert.Equal(beta.Id, result["Project Beta"]);
        Assert.Null(result["Gamma"]);
    }
}
