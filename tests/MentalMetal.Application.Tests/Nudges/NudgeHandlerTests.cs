using MentalMetal.Application.Common;
using MentalMetal.Application.Nudges;
using MentalMetal.Application.Tests.Briefings;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Nudges;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Nudges;

public class NudgeHandlerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 4, 14, 10, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _time;

    private readonly INudgeRepository _nudgeRepo = Substitute.For<INudgeRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly IInitiativeRepository _initiativeRepo = Substitute.For<IInitiativeRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public NudgeHandlerTests()
    {
        _time = new FakeTimeProvider(_now);
        _currentUser.UserId.Returns(_userId);
    }

    private Person SeedPerson(Guid? userId = null)
    {
        var p = Person.Create(userId ?? _userId, "Sarah", PersonType.DirectReport);
        return p;
    }

    private Nudge SeedNudge(Guid? userId = null, NudgeCadence? cadence = null)
    {
        return Nudge.Create(
            userId ?? _userId,
            "t",
            cadence ?? NudgeCadence.Daily(),
            DateOnly.FromDateTime(_now.UtcDateTime));
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_HappyPath_ReturnsResponse()
    {
        var handler = new CreateNudgeHandler(
            _nudgeRepo, _personRepo, _initiativeRepo, _currentUser, _uow, _time);

        var response = await handler.HandleAsync(
            new CreateNudgeRequest("Review risk log", CadenceType.Daily),
            CancellationToken.None);

        Assert.Equal("Review risk log", response.Title);
        Assert.True(response.IsActive);
        await _nudgeRepo.Received(1).AddAsync(Arg.Any<Nudge>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_PersonBelongsToAnotherUser_ThrowsLinkedEntityNotFound()
    {
        var personId = Guid.NewGuid();
        var otherUsersPerson = SeedPerson(Guid.NewGuid());
        _personRepo.GetByIdAsync(personId, Arg.Any<CancellationToken>()).Returns(otherUsersPerson);

        var handler = new CreateNudgeHandler(
            _nudgeRepo, _personRepo, _initiativeRepo, _currentUser, _uow, _time);

        await Assert.ThrowsAsync<LinkedEntityNotFoundException>(() =>
            handler.HandleAsync(
                new CreateNudgeRequest("Title", CadenceType.Daily, PersonId: personId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_PersonNotFound_ThrowsLinkedEntityNotFound()
    {
        var personId = Guid.NewGuid();
        _personRepo.GetByIdAsync(personId, Arg.Any<CancellationToken>()).Returns((Person?)null);

        var handler = new CreateNudgeHandler(
            _nudgeRepo, _personRepo, _initiativeRepo, _currentUser, _uow, _time);

        await Assert.ThrowsAsync<LinkedEntityNotFoundException>(() =>
            handler.HandleAsync(
                new CreateNudgeRequest("Title", CadenceType.Daily, PersonId: personId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_WeeklyWithoutDayOfWeek_ThrowsInvalidCadence()
    {
        var handler = new CreateNudgeHandler(
            _nudgeRepo, _personRepo, _initiativeRepo, _currentUser, _uow, _time);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new CreateNudgeRequest("t", CadenceType.Weekly), CancellationToken.None));
        Assert.Equal("nudge.invalidCadence", ex.Code);
    }

    [Fact]
    public async Task Create_EmptyTitle_ThrowsArgumentException()
    {
        var handler = new CreateNudgeHandler(
            _nudgeRepo, _personRepo, _initiativeRepo, _currentUser, _uow, _time);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new CreateNudgeRequest("", CadenceType.Daily), CancellationToken.None));
    }

    // ---------- Get ----------

    [Fact]
    public async Task Get_NotFound_Throws()
    {
        _nudgeRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Nudge?)null);
        var handler = new GetNudgeHandler(_nudgeRepo, _currentUser);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Get_WrongOwner_Throws()
    {
        var n = SeedNudge(userId: Guid.NewGuid());
        _nudgeRepo.GetByIdAsync(n.Id, Arg.Any<CancellationToken>()).Returns(n);
        var handler = new GetNudgeHandler(_nudgeRepo, _currentUser);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(n.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Get_Found_ReturnsResponse()
    {
        var n = SeedNudge();
        _nudgeRepo.GetByIdAsync(n.Id, Arg.Any<CancellationToken>()).Returns(n);
        var handler = new GetNudgeHandler(_nudgeRepo, _currentUser);
        var result = await handler.HandleAsync(n.Id, CancellationToken.None);
        Assert.Equal(n.Id, result.Id);
    }

    // ---------- MarkNudged ----------

    [Fact]
    public async Task MarkNudged_HappyPath_AdvancesDueDate()
    {
        var n = SeedNudge();
        _nudgeRepo.GetByIdAsync(n.Id, Arg.Any<CancellationToken>()).Returns(n);
        var handler = new MarkNudgeAsNudgedHandler(_nudgeRepo, _currentUser, _uow, _time);

        var result = await handler.HandleAsync(n.Id, CancellationToken.None);

        Assert.Equal(DateOnly.FromDateTime(_now.UtcDateTime).AddDays(1), result.NextDueDate);
    }

    [Fact]
    public async Task MarkNudged_Paused_ThrowsNotActive()
    {
        var n = SeedNudge();
        n.Pause();
        _nudgeRepo.GetByIdAsync(n.Id, Arg.Any<CancellationToken>()).Returns(n);
        var handler = new MarkNudgeAsNudgedHandler(_nudgeRepo, _currentUser, _uow, _time);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(n.Id, CancellationToken.None));
        Assert.Equal("nudge.notActive", ex.Code);
    }

    // ---------- Pause / Resume ----------

    [Fact]
    public async Task Pause_AlreadyPaused_Throws()
    {
        var n = SeedNudge();
        n.Pause();
        _nudgeRepo.GetByIdAsync(n.Id, Arg.Any<CancellationToken>()).Returns(n);
        var handler = new PauseNudgeHandler(_nudgeRepo, _currentUser, _uow);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(n.Id, CancellationToken.None));
        Assert.Equal("nudge.alreadyPaused", ex.Code);
    }

    [Fact]
    public async Task Resume_Active_Throws()
    {
        var n = SeedNudge();
        _nudgeRepo.GetByIdAsync(n.Id, Arg.Any<CancellationToken>()).Returns(n);
        var handler = new ResumeNudgeHandler(_nudgeRepo, _currentUser, _uow, _time);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(n.Id, CancellationToken.None));
        Assert.Equal("nudge.alreadyActive", ex.Code);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_Found_RemovesAndSaves()
    {
        var n = SeedNudge();
        _nudgeRepo.GetByIdAsync(n.Id, Arg.Any<CancellationToken>()).Returns(n);
        var handler = new DeleteNudgeHandler(_nudgeRepo, _currentUser, _uow);

        await handler.HandleAsync(n.Id, CancellationToken.None);

        _nudgeRepo.Received(1).Remove(n);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_NotFound_Throws()
    {
        _nudgeRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Nudge?)null);
        var handler = new DeleteNudgeHandler(_nudgeRepo, _currentUser, _uow);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(Guid.NewGuid(), CancellationToken.None));
    }

    // ---------- Update cadence ----------

    [Fact]
    public async Task UpdateCadence_Valid_RecomputesDueDate()
    {
        var n = SeedNudge();
        _nudgeRepo.GetByIdAsync(n.Id, Arg.Any<CancellationToken>()).Returns(n);
        var handler = new UpdateNudgeCadenceHandler(_nudgeRepo, _currentUser, _uow, _time);

        var result = await handler.HandleAsync(n.Id,
            new UpdateCadenceRequest(CadenceType.Weekly, DayOfWeek: DayOfWeek.Thursday),
            CancellationToken.None);

        Assert.Equal(CadenceType.Weekly, result.Cadence.Type);
    }
}
