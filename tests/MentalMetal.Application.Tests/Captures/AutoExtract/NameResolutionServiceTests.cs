using MentalMetal.Application.Captures.AutoExtract;
using MentalMetal.Domain.People;

namespace MentalMetal.Application.Tests.Captures.AutoExtract;

public class NameResolutionServiceTests
{
    private readonly NameResolutionService _sut = new();
    private readonly Guid _userId = Guid.NewGuid();

    private Person CreatePerson(string name, params string[] aliases)
    {
        return Person.Create(_userId, name, PersonType.Peer, aliases: aliases);
    }

    [Fact]
    public void Resolve_ExactNameMatch_ReturnsPersonId()
    {
        var alice = CreatePerson("Alice Smith");
        var people = new List<Person> { alice };

        var result = _sut.Resolve(["Alice Smith"], people);

        Assert.Equal(alice.Id, result["Alice Smith"]);
    }

    [Fact]
    public void Resolve_CaseInsensitiveNameMatch_ReturnsPersonId()
    {
        var bob = CreatePerson("Bob Jones");
        var people = new List<Person> { bob };

        var result = _sut.Resolve(["bob jones"], people);

        Assert.Equal(bob.Id, result["bob jones"]);
    }

    [Fact]
    public void Resolve_AliasMatch_ReturnsPersonId()
    {
        var charlie = CreatePerson("Charlie Brown", "Chuck");
        var people = new List<Person> { charlie };

        var result = _sut.Resolve(["Chuck"], people);

        Assert.Equal(charlie.Id, result["Chuck"]);
    }

    [Fact]
    public void Resolve_CaseInsensitiveAliasMatch_ReturnsPersonId()
    {
        var person = CreatePerson("Diana Prince", "WonderWoman");
        var people = new List<Person> { person };

        var result = _sut.Resolve(["wonderwoman"], people);

        Assert.Equal(person.Id, result["wonderwoman"]);
    }

    [Fact]
    public void Resolve_FuzzySubstringMatch_Unambiguous_ReturnsPersonId()
    {
        var person = CreatePerson("Alice Wonderland");
        var people = new List<Person> { person };

        var result = _sut.Resolve(["Alice"], people);

        Assert.Equal(person.Id, result["Alice"]);
    }

    [Fact]
    public void Resolve_FuzzySubstringMatch_Ambiguous_ReturnsNull()
    {
        var alice1 = CreatePerson("Alice Smith");
        var alice2 = CreatePerson("Alice Jones");
        var people = new List<Person> { alice1, alice2 };

        var result = _sut.Resolve(["Alice"], people);

        Assert.Null(result["Alice"]);
    }

    [Fact]
    public void Resolve_NoMatch_ReturnsNull()
    {
        var person = CreatePerson("Bob Builder");
        var people = new List<Person> { person };

        var result = _sut.Resolve(["Unknown Person"], people);

        Assert.Null(result["Unknown Person"]);
    }

    [Fact]
    public void Resolve_ShortName_SkipsFuzzy_ReturnsNull()
    {
        var person = CreatePerson("Al Smith");
        var people = new List<Person> { person };

        var result = _sut.Resolve(["Al"], people);

        // "Al" is only 2 chars — below the 3-char fuzzy minimum
        Assert.Null(result["Al"]);
    }

    [Fact]
    public void Resolve_EmptyName_ReturnsNull()
    {
        var people = new List<Person> { CreatePerson("Alice") };

        var result = _sut.Resolve([""], people);

        Assert.Null(result[""]);
    }

    [Fact]
    public void Resolve_MultipleNames_ResolvesEach()
    {
        var alice = CreatePerson("Alice Smith");
        var bob = CreatePerson("Bob Jones");
        var people = new List<Person> { alice, bob };

        var result = _sut.Resolve(["Alice Smith", "Bob Jones", "Unknown"], people);

        Assert.Equal(alice.Id, result["Alice Smith"]);
        Assert.Equal(bob.Id, result["Bob Jones"]);
        Assert.Null(result["Unknown"]);
    }

    [Fact]
    public void Resolve_DuplicateNames_HandledCorrectly()
    {
        var alice = CreatePerson("Alice");
        var people = new List<Person> { alice };

        var result = _sut.Resolve(["Alice", "Alice"], people);

        Assert.Single(result);
        Assert.Equal(alice.Id, result["Alice"]);
    }

    [Fact]
    public void Resolve_ExactMatchTakesPriorityOverFuzzy()
    {
        var aliceS = CreatePerson("Alice Smith");
        var alice = CreatePerson("Alice");
        var people = new List<Person> { aliceS, alice };

        var result = _sut.Resolve(["Alice"], people);

        // Exact match on "Alice" should take priority
        Assert.Equal(alice.Id, result["Alice"]);
    }
}
