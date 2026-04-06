using MentalMetal.Application.Common;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Users;

public class RemoveAiProviderTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RemoveAiProviderHandler _handler;

    public RemoveAiProviderTests()
    {
        _handler = new RemoveAiProviderHandler(_userRepository, _currentUserService, _unitOfWork);
    }

    [Fact]
    public async Task ConfiguredUser_RemovesAndPersists()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);
        user.ConfigureAiProvider(AiProvider.Anthropic, "enc_key", "model", null);
        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(CancellationToken.None);

        Assert.Null(user.AiProviderConfig);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnconfiguredUser_StillPersists()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);
        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(CancellationToken.None);

        Assert.Null(user.AiProviderConfig);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
