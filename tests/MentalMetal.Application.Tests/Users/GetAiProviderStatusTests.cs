using MentalMetal.Application.Common.Ai;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Users;

public class GetAiProviderStatusTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ITasteBudgetService _tasteBudgetService = Substitute.For<ITasteBudgetService>();
    private readonly GetAiProviderStatusHandler _handler;

    public GetAiProviderStatusTests()
    {
        _handler = new GetAiProviderStatusHandler(_userRepository, _currentUserService, _tasteBudgetService);
        _tasteBudgetService.DailyLimit.Returns(5);
        _tasteBudgetService.IsEnabled.Returns(true);
    }

    [Fact]
    public async Task ConfiguredUser_ReturnsProviderDetails()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);
        user.ConfigureAiProvider(AiProvider.Anthropic, "enc_key", "claude-sonnet-4-20250514", 4096);
        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _tasteBudgetService.GetRemainingAsync(user.Id, Arg.Any<CancellationToken>()).Returns(5);

        var result = await _handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsConfigured);
        Assert.Equal("Anthropic", result.Provider);
        Assert.Equal("claude-sonnet-4-20250514", result.Model);
        Assert.Equal(4096, result.MaxTokens);
    }

    [Fact]
    public async Task UnconfiguredUser_ReturnsNotConfiguredWithTasteBudget()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);
        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _tasteBudgetService.GetRemainingAsync(user.Id, Arg.Any<CancellationToken>()).Returns(3);

        var result = await _handler.HandleAsync(CancellationToken.None);

        Assert.False(result.IsConfigured);
        Assert.Null(result.Provider);
        Assert.Null(result.Model);
        Assert.Equal(3, result.TasteBudget.Remaining);
        Assert.Equal(5, result.TasteBudget.DailyLimit);
        Assert.True(result.TasteBudget.IsEnabled);
    }
}
