using MentalMetal.Application.Chat.Global;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.People;
using NSubstitute;

namespace MentalMetal.Application.Tests.Chat.Global;

public class RuleIntentClassifierTests
{
    private readonly IPersonRepository _people = Substitute.For<IPersonRepository>();
    private readonly IInitiativeRepository _initiatives = Substitute.For<IInitiativeRepository>();
    private readonly RuleIntentClassifier _classifier;
    private readonly Guid _userId = Guid.NewGuid();

    public RuleIntentClassifierTests()
    {
        _classifier = new RuleIntentClassifier(_people, _initiatives);
        _people.GetAllAsync(_userId, Arg.Any<PersonType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Person>());
        _initiatives.GetAllAsync(_userId, Arg.Any<InitiativeStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Initiative>());
    }

    [Fact]
    public async Task OverduePhrase_LightsOverdueWork()
    {
        var result = await _classifier.ClassifyAsync(_userId, "What's overdue?", CancellationToken.None);
        Assert.Contains(ChatIntent.OverdueWork, result.Intents);
    }

    [Fact]
    public async Task TodayPhrase_LightsMyDay()
    {
        var result = await _classifier.ClassifyAsync(_userId, "What's on my plate today?", CancellationToken.None);
        Assert.Contains(ChatIntent.MyDay, result.Intents);
    }

    [Fact]
    public async Task ThisWeekPhrase_LightsMyWeek()
    {
        var result = await _classifier.ClassifyAsync(_userId, "Anything due this week?", CancellationToken.None);
        Assert.Contains(ChatIntent.MyWeek, result.Intents);
    }

    [Fact]
    public async Task PersonNameMatch_LightsPersonLens_AndExtractsId()
    {
        var jane = Person.Create(_userId, "Jane Doe", PersonType.DirectReport);
        _people.GetAllAsync(_userId, Arg.Any<PersonType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new[] { jane });

        var result = await _classifier.ClassifyAsync(_userId, "How is Jane doing?", CancellationToken.None);

        Assert.Contains(ChatIntent.PersonLens, result.Intents);
        Assert.Contains(jane.Id, result.Hints.PersonIds);
    }

    [Fact]
    public async Task AmbiguousFirstName_ReturnsAllMatches()
    {
        var sarahA = Person.Create(_userId, "Sarah Chen", PersonType.DirectReport);
        var sarahB = Person.Create(_userId, "Sarah Patel", PersonType.DirectReport);
        _people.GetAllAsync(_userId, Arg.Any<PersonType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new[] { sarahA, sarahB });

        var result = await _classifier.ClassifyAsync(_userId, "How is Sarah doing?", CancellationToken.None);

        Assert.Contains(ChatIntent.PersonLens, result.Intents);
        Assert.Contains(sarahA.Id, result.Hints.PersonIds);
        Assert.Contains(sarahB.Id, result.Hints.PersonIds);
    }

    [Fact]
    public async Task NoMatch_ReturnsGeneric()
    {
        var result = await _classifier.ClassifyAsync(_userId, "Anything I should worry about?", CancellationToken.None);
        Assert.True(result.IsGenericOnly);
    }

    [Fact]
    public async Task EmptyMessage_ReturnsGeneric()
    {
        var result = await _classifier.ClassifyAsync(_userId, "", CancellationToken.None);
        Assert.True(result.IsGenericOnly);
    }

    [Fact]
    public async Task InitiativeNameMatch_LightsInitiativeStatus()
    {
        var initiative = Initiative.Create(_userId, "Q3 Migration");
        _initiatives.GetAllAsync(_userId, Arg.Any<InitiativeStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { initiative });

        var result = await _classifier.ClassifyAsync(_userId, "Status of Q3 Migration?", CancellationToken.None);

        Assert.Contains(ChatIntent.InitiativeStatus, result.Intents);
        Assert.Contains(initiative.Id, result.Hints.InitiativeIds);
    }
}
