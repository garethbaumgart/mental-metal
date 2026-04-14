using MentalMetal.Application.Briefings;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Goals;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Observations;
using MentalMetal.Domain.OneOnOnes;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace MentalMetal.Application.Tests.Briefings;

public class BriefingFactsAssemblerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 4, 14, 8, 30, 0, TimeSpan.Zero);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPersonRepository _people = Substitute.For<IPersonRepository>();
    private readonly IInitiativeRepository _initiatives = Substitute.For<IInitiativeRepository>();
    private readonly ICommitmentRepository _commitments = Substitute.For<ICommitmentRepository>();
    private readonly IDelegationRepository _delegations = Substitute.For<IDelegationRepository>();
    private readonly ICaptureRepository _captures = Substitute.For<ICaptureRepository>();
    private readonly IOneOnOneRepository _oneOnOnes = Substitute.For<IOneOnOneRepository>();
    private readonly IObservationRepository _observations = Substitute.For<IObservationRepository>();
    private readonly IGoalRepository _goals = Substitute.For<IGoalRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly BriefingOptions _options = new();
    private readonly FakeTimeProvider _time;
    private readonly BriefingFactsAssembler _assembler;

    public BriefingFactsAssemblerTests()
    {
        _time = new FakeTimeProvider(_now);
        _currentUser.UserId.Returns(_userId);

        var user = TestUserBuilder.Build(_userId, "UTC", hasProvider: true);
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        _people.GetAllAsync(_userId, Arg.Any<PersonType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Person>());
        _initiatives.GetAllAsync(_userId, Arg.Any<InitiativeStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Initiative>());
        _initiatives.GetByIdsAsync(_userId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Initiative>());
        _commitments.GetAllAsync(
            _userId, Arg.Any<CommitmentDirection?>(), Arg.Any<CommitmentStatus?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Commitment>());
        _delegations.GetAllAsync(
            _userId, Arg.Any<DelegationStatus?>(), Arg.Any<Priority?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Delegation>());
        _captures.GetCloseOutQueueAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Capture>());
        _oneOnOnes.GetAllAsync(_userId, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OneOnOne>());
        _observations.GetAllAsync(
            _userId, Arg.Any<Guid?>(), Arg.Any<ObservationTag?>(),
            Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Observation>());
        _goals.GetAllAsync(
            _userId, Arg.Any<Guid?>(), Arg.Any<GoalType?>(), Arg.Any<GoalStatus?>(),
            Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Goal>());

        _assembler = new BriefingFactsAssembler(
            _users, _people, _initiatives, _commitments, _delegations,
            _captures, _oneOnOnes, _observations, _goals,
            _currentUser, Options.Create(_options), _time);
    }

    [Fact]
    public async Task BuildMorning_EmptyState_ReturnsTodayWithEmptyLists()
    {
        var facts = await _assembler.BuildMorningAsync(default);

        Assert.Equal("2026-04-14", facts.UserLocalDate);
        Assert.Equal("UTC", facts.UserTimezone);
        Assert.Empty(facts.TopCommitmentsDueToday);
        Assert.Empty(facts.OneOnOnesToday);
        Assert.Empty(facts.OverdueDelegations);
        Assert.Empty(facts.RecentCaptures);
        Assert.Empty(facts.PeopleNeedingAttention);
    }

    [Fact]
    public async Task BuildMorning_TwoIdenticalCalls_ProduceEquivalentFacts()
    {
        // Records use reference equality for collection members, so compare the
        // structural shape: scope, scalar fields, and list counts/contents.
        var first = await _assembler.BuildMorningAsync(default);
        var second = await _assembler.BuildMorningAsync(default);

        Assert.Equal(first.UserLocalDate, second.UserLocalDate);
        Assert.Equal(first.UserTimezone, second.UserTimezone);
        Assert.Equal(first.TopCommitmentsDueToday, second.TopCommitmentsDueToday);
        Assert.Equal(first.OneOnOnesToday, second.OneOnOnesToday);
        Assert.Equal(first.OverdueDelegations, second.OverdueDelegations);
        Assert.Equal(first.RecentCaptures, second.RecentCaptures);
        Assert.Equal(first.PeopleNeedingAttention, second.PeopleNeedingAttention);
    }

    [Fact]
    public async Task BuildMorning_BeforeMorningHour_RollsBackToYesterday()
    {
        // 4 a.m. UTC: before MorningBriefingHour (default 5) → scope key reflects yesterday.
        var earlyMorning = new DateTimeOffset(2026, 4, 14, 4, 0, 0, TimeSpan.Zero);
        _time.Advance(earlyMorning - _now);

        var facts = await _assembler.BuildMorningAsync(default);

        Assert.Equal("2026-04-13", facts.UserLocalDate);
    }

    [Fact]
    public async Task BuildMorning_TopCommitmentsDueToday_AppliesCap()
    {
        var today = new DateOnly(2026, 4, 14);
        var personId = Guid.NewGuid();
        // Seven open commitments due today; cap is 5.
        var commitments = Enumerable.Range(0, 7)
            .Select(_ => Commitment.Create(_userId, $"task-{Guid.NewGuid():N}", CommitmentDirection.MineToThem, personId, today))
            .ToList();
        _commitments.GetAllAsync(
            _userId, Arg.Any<CommitmentDirection?>(), CommitmentStatus.Open,
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(commitments);

        var facts = await _assembler.BuildMorningAsync(default);

        Assert.Equal(_options.TopItemsPerSection, facts.TopCommitmentsDueToday.Count);
    }

    [Fact]
    public async Task BuildWeekly_ScopeFields_AreIsoWeekValues()
    {
        var facts = await _assembler.BuildWeeklyAsync(default);

        Assert.Equal(2026, facts.IsoYear);
        Assert.Equal(16, facts.WeekNumber);
        // Monday 2026-04-13 → Sunday 2026-04-19.
        Assert.Equal("2026-04-13", facts.WeekStartIso);
        Assert.Equal("2026-04-19", facts.WeekEndIso);
    }

    [Fact]
    public async Task BuildOneOnOnePrep_UnknownPerson_ReturnsNull()
    {
        var personId = Guid.NewGuid();
        _people.GetByIdAsync(personId, Arg.Any<CancellationToken>()).Returns((Person?)null);

        var facts = await _assembler.BuildOneOnOnePrepAsync(personId, default);

        Assert.Null(facts);
    }

    [Fact]
    public async Task BuildOneOnOnePrep_OwnedPerson_PopulatesPersonFact()
    {
        var person = Person.Create(_userId, "Sarah", PersonType.DirectReport);
        _people.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);

        var facts = await _assembler.BuildOneOnOnePrepAsync(person.Id, default);

        Assert.NotNull(facts);
        Assert.Equal(person.Id, facts!.Person.Id);
        Assert.Equal("Sarah", facts.Person.Name);
        Assert.Equal("DirectReport", facts.Person.Type);
    }
}
