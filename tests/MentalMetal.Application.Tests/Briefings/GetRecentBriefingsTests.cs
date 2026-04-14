using MentalMetal.Application.Briefings;
using MentalMetal.Domain.Briefings;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Briefings;

public class GetRecentBriefingsTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IBriefingRepository _briefings = Substitute.For<IBriefingRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetRecentBriefingsHandler _handler;

    public GetRecentBriefingsTests()
    {
        _currentUser.UserId.Returns(_userId);
        _briefings.ListRecentAsync(_userId, Arg.Any<BriefingType?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BriefingListItem>());
        _handler = new GetRecentBriefingsHandler(_briefings, _currentUser);
    }

    [Fact]
    public async Task Handle_DefaultLimit_DelegatesWithCorrectArgs()
    {
        await _handler.HandleAsync(new GetRecentBriefingsQuery(null, GetRecentBriefingsHandler.DefaultLimit), default);

        await _briefings.Received(1).ListRecentAsync(_userId, null, GetRecentBriefingsHandler.DefaultLimit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TypeFilter_PassedThrough()
    {
        await _handler.HandleAsync(new GetRecentBriefingsQuery(BriefingTypeDto.Weekly, 10), default);

        await _briefings.Received(1).ListRecentAsync(_userId, BriefingType.Weekly, 10, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)]
    public async Task Handle_OutOfRangeLimit_Throws(int limit)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _handler.HandleAsync(new GetRecentBriefingsQuery(null, limit), default));
    }

    [Fact]
    public async Task Handle_MapsListItemToSummary()
    {
        var listItem = new BriefingListItem(
            Id: Guid.NewGuid(),
            UserId: _userId,
            Type: BriefingType.Morning,
            ScopeKey: "morning:2026-04-14",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Model: "model",
            InputTokens: 12,
            OutputTokens: 8);
        _briefings.ListRecentAsync(_userId, Arg.Any<BriefingType?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { listItem });

        var result = await _handler.HandleAsync(new GetRecentBriefingsQuery(null, 5), default);

        var item = Assert.Single(result);
        Assert.Equal(listItem.Id, item.Id);
        Assert.Equal(BriefingTypeDto.Morning, item.Type);
        Assert.Equal("morning:2026-04-14", item.ScopeKey);
        Assert.Equal(12, item.InputTokens);
        Assert.Equal(8, item.OutputTokens);
    }
}
