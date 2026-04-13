using MentalMetal.Application.Chat.Global;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.People;
using NSubstitute;

namespace MentalMetal.Application.Tests.Chat.Global;

public class GlobalChatContextBuilderTests
{
    private readonly IPersonRepository _people = Substitute.For<IPersonRepository>();
    private readonly IInitiativeRepository _initiatives = Substitute.For<IInitiativeRepository>();
    private readonly ICommitmentRepository _commitments = Substitute.For<ICommitmentRepository>();
    private readonly IDelegationRepository _delegations = Substitute.For<IDelegationRepository>();
    private readonly ICaptureRepository _captures = Substitute.For<ICaptureRepository>();
    private readonly GlobalChatContextBuilder _builder;
    private readonly Guid _userId = Guid.NewGuid();

    public GlobalChatContextBuilderTests()
    {
        _builder = new GlobalChatContextBuilder(_people, _initiatives, _commitments, _delegations, _captures);
        // Defaults: empty everything.
        _people.GetByIdsAsync(_userId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Person>());
        _commitments.GetAllAsync(_userId, Arg.Any<CommitmentDirection?>(), Arg.Any<CommitmentStatus?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Commitment>());
        _delegations.GetAllAsync(_userId, Arg.Any<DelegationStatus?>(), Arg.Any<Priority?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Delegation>());
        _captures.GetAllAsync(_userId, Arg.Any<CaptureType?>(), Arg.Any<ProcessingStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Capture>());
        _initiatives.GetAllAsync(_userId, Arg.Any<InitiativeStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Initiative>());
    }

    [Fact]
    public async Task EmptyData_ReturnsEmptyPayload()
    {
        var result = await _builder.BuildAsync(_userId, IntentSet.Generic, [], CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Persons);
        Assert.Empty(result.Initiatives);
        Assert.Empty(result.Commitments);
        Assert.Empty(result.Delegations);
        Assert.Empty(result.Captures);
    }

    [Fact]
    public async Task EmptyUserId_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _builder.BuildAsync(Guid.Empty, IntentSet.Generic, [], CancellationToken.None));
    }

    [Fact]
    public async Task OverdueIntent_CapsCommitmentsAt30()
    {
        var personId = Guid.NewGuid();
        // Create 50 overdue commitments.
        var overdue = Enumerable.Range(0, 50)
            .Select(i => Commitment.Create(_userId, $"task {i}", CommitmentDirection.MineToThem, personId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-(i + 1)))))
            .ToList();
        _commitments.GetAllAsync(_userId, Arg.Any<CommitmentDirection?>(), Arg.Any<CommitmentStatus?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(overdue);

        var intents = new IntentSet([ChatIntent.OverdueWork], EntityHints.Empty);
        var result = await _builder.BuildAsync(_userId, intents, [], CancellationToken.None);

        Assert.Equal(GlobalChatContextBuilder.OverdueCommitmentCap, result.Commitments.Count);
    }

    [Fact]
    public async Task UserIsolation_DropsForeignRecords()
    {
        var otherUser = Guid.NewGuid();
        var foreignPerson = Person.Create(otherUser, "Foreign", PersonType.DirectReport);

        // Repository (badly) returns a foreign record. Builder must filter it out.
        _people.GetByIdsAsync(_userId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { foreignPerson });

        var intents = new IntentSet([ChatIntent.PersonLens], new EntityHints([foreignPerson.Id], [], null));
        var result = await _builder.BuildAsync(_userId, intents, [], CancellationToken.None);

        Assert.DoesNotContain(result.Persons, p => p.Id == foreignPerson.Id);
    }

    [Fact]
    public async Task PersonLens_ResolvesPersonItem()
    {
        var jane = Person.Create(_userId, "Jane", PersonType.DirectReport);
        _people.GetByIdsAsync(_userId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { jane });

        var intents = new IntentSet([ChatIntent.PersonLens], new EntityHints([jane.Id], [], null));
        var result = await _builder.BuildAsync(_userId, intents, [], CancellationToken.None);

        Assert.Contains(result.Persons, p => p.Id == jane.Id && p.Name == "Jane");
    }

    [Fact]
    public async Task GenericIntent_ProvidesCounters()
    {
        var person = Person.Create(_userId, "P", PersonType.DirectReport);
        var open = Commitment.Create(_userId, "open", CommitmentDirection.TheirsToMe, person.Id);
        _commitments.GetAllAsync(_userId, Arg.Any<CommitmentDirection?>(), Arg.Any<CommitmentStatus?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { open });

        var result = await _builder.BuildAsync(_userId, IntentSet.Generic, [], CancellationToken.None);

        Assert.Equal(1, result.Counters.OpenCommitments);
    }
}
