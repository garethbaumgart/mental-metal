using MentalMetal.Application.Briefings;
using MentalMetal.Application.Briefings.Facts;
using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Briefings;
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
using NSubstitute.ExceptionExtensions;

namespace MentalMetal.Application.Tests.Briefings;

public class BriefingServiceTests
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
    private readonly IBriefingRepository _briefings = Substitute.For<IBriefingRepository>();
    private readonly IAiCompletionService _ai = Substitute.For<IAiCompletionService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly BriefingOptions _options = new();
    private readonly FakeTimeProvider _time;
    private readonly BriefingPromptBuilder _promptBuilder = new();
    private readonly BriefingFactsAssembler _assembler;
    private readonly BriefingService _service;

    public BriefingServiceTests()
    {
        _time = new FakeTimeProvider(_now);
        _currentUser.UserId.Returns(_userId);

        // User exists with UTC timezone and an AI provider configured.
        var user = TestUserBuilder.Build(_userId, timezone: "UTC", hasProvider: true);
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        // Default empty repository responses so facts assembly returns empty lists.
        _people.GetAllAsync(_userId, Arg.Any<PersonType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Person>());
        _initiatives.GetAllAsync(_userId, Arg.Any<InitiativeStatus?>(), Arg.Any<CancellationToken>())
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

        _briefings.GetLatestAsync(_userId, Arg.Any<BriefingType>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Briefing?)null);

        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AiCompletionResult("# briefing markdown", InputTokens: 100, OutputTokens: 50, Model: "test-model", Provider: AiProvider.Anthropic));

        _assembler = new BriefingFactsAssembler(
            _users, _people, _initiatives, _commitments, _delegations,
            _captures, _oneOnOnes, _observations, _goals,
            _currentUser, Options.Create(_options), _time);

        _service = new BriefingService(
            _assembler, _promptBuilder, _briefings, _ai, _uow,
            _currentUser, Options.Create(_options), _time);
    }

    [Fact]
    public async Task GenerateMorning_NoCache_CallsAiAndPersists()
    {
        var result = await _service.GenerateMorningAsync(force: false, default);

        Assert.False(result.WasCached);
        Assert.Equal(BriefingType.Morning, result.Briefing.Type);
        Assert.Equal("morning:2026-04-14", result.Briefing.ScopeKey);
        Assert.Equal("# briefing markdown", result.Briefing.MarkdownBody);
        await _ai.Received(1).CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>());
        await _briefings.Received(1).AddAsync(Arg.Any<Briefing>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateMorning_CacheHit_ReturnsExistingAndSkipsAi()
    {
        var existing = Briefing.Create(_userId, BriefingType.Morning, "morning:2026-04-14",
            _now.AddHours(-1), "# cached", "{}", "model", 1, 1);
        _briefings.GetLatestAsync(_userId, BriefingType.Morning, "morning:2026-04-14", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _service.GenerateMorningAsync(force: false, default);

        Assert.True(result.WasCached);
        Assert.Equal("# cached", result.Briefing.MarkdownBody);
        await _ai.DidNotReceive().CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>());
        await _briefings.DidNotReceive().AddAsync(Arg.Any<Briefing>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateMorning_ForceTrue_BypassesCache()
    {
        var existing = Briefing.Create(_userId, BriefingType.Morning, "morning:2026-04-14",
            _now.AddHours(-1), "# cached", "{}", "model", 1, 1);
        _briefings.GetLatestAsync(_userId, BriefingType.Morning, "morning:2026-04-14", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _service.GenerateMorningAsync(force: true, default);

        Assert.False(result.WasCached);
        await _ai.Received(1).CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateMorning_StaleCache_RegeneratesAndPersists()
    {
        var stale = Briefing.Create(_userId, BriefingType.Morning, "morning:2026-04-14",
            _now.AddHours(-13), "# stale", "{}", "model", 1, 1);
        _briefings.GetLatestAsync(_userId, BriefingType.Morning, "morning:2026-04-14", Arg.Any<CancellationToken>())
            .Returns(stale);

        var result = await _service.GenerateMorningAsync(force: false, default);

        Assert.False(result.WasCached);
        await _ai.Received(1).CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateMorning_AiNotConfigured_ThrowsTypedException()
    {
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsyncForAnyArgs(new InvalidOperationException(
                "AI provider is not configured. Please set up your AI provider in settings."));

        await Assert.ThrowsAsync<AiProviderNotConfiguredException>(() =>
            _service.GenerateMorningAsync(force: false, default));

        await _briefings.DidNotReceive().AddAsync(Arg.Any<Briefing>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateOneOnOnePrep_UnknownPerson_ReturnsNull()
    {
        var personId = Guid.NewGuid();
        _people.GetByIdAsync(personId, Arg.Any<CancellationToken>()).Returns((Person?)null);

        var result = await _service.GenerateOneOnOnePrepAsync(personId, force: false, default);

        Assert.Null(result);
        await _ai.DidNotReceive().CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateOneOnOnePrep_ForeignPerson_ReturnsNull()
    {
        var otherUser = Guid.NewGuid();
        var person = Person.Create(otherUser, "Sarah", PersonType.DirectReport);
        _people.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);

        var result = await _service.GenerateOneOnOnePrepAsync(person.Id, force: false, default);

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateOneOnOnePrep_OwnedPerson_GeneratesPrep()
    {
        var person = Person.Create(_userId, "Sarah", PersonType.DirectReport);
        _people.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);

        var result = await _service.GenerateOneOnOnePrepAsync(person.Id, force: false, default);

        Assert.NotNull(result);
        Assert.Equal(BriefingType.OneOnOnePrep, result!.Briefing.Type);
        Assert.Equal($"oneonone:{person.Id:N}", result.Briefing.ScopeKey);
    }

    [Fact]
    public async Task GenerateWeekly_ScopeKey_UsesIsoWeek()
    {
        // 2026-04-14 is a Tuesday; ISO week 16 of 2026.
        var result = await _service.GenerateWeeklyAsync(force: false, default);

        Assert.Equal(BriefingType.Weekly, result.Briefing.Type);
        Assert.Equal("weekly:2026-W16", result.Briefing.ScopeKey);
    }
}

internal static class TestUserBuilder
{
    /// <summary>
    /// Builds a User whose Id is forced via reflection so the test can pre-bind the userId
    /// to repository substitutes that match on Arg.Is(<c>_userId</c>).
    /// </summary>
    public static User Build(Guid id, string timezone, bool hasProvider)
    {
        var user = User.RegisterWithPassword(
            email: $"user-{id:N}@test.invalid",
            name: "Test User",
            password: Password.Create("password-123", new Microsoft.AspNetCore.Identity.PasswordHasher<User>()),
            timezone: timezone);

        // Walk up to Entity (Id has a protected setter on Entity).
        var entityType = typeof(User).BaseType!; // AggregateRoot
        while (entityType != null && entityType.GetProperty("Id",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.DeclaredOnly) is null)
        {
            entityType = entityType.BaseType;
        }
        var idProp = entityType!.GetProperty("Id",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.DeclaredOnly)!;
        idProp.SetValue(user, id);

        if (hasProvider)
        {
            user.ConfigureAiProvider(AiProvider.Anthropic, "encrypted-key", "claude-test", maxTokens: 1500);
        }
        return user;
    }
}
