using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Auth;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Users;

public class RegisterOrLoginUserTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegisterOrLoginUserHandler _handler;

    public RegisterOrLoginUserTests()
    {
        _handler = new RegisterOrLoginUserHandler(_userRepository, _tokenService, _unitOfWork);
    }

    [Fact]
    public async Task NewUser_RegistersAndReturnsTokens()
    {
        var command = new RegisterOrLoginCommand("auth-123", "new@example.com", "New User", null);

        _userRepository.GetByExternalAuthIdAsync("auth-123", Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _userRepository.ExistsByEmailAsync("new@example.com", Arg.Any<CancellationToken>())
            .Returns(false);
        _tokenService.GenerateTokens(Arg.Any<User>())
            .Returns(new TokenResult("access-token", "refresh-token"));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsNewUser);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturningUser_RecordsLoginAndReturnsTokens()
    {
        var existingUser = User.Register("auth-123", "existing@example.com", "Existing", null);
        var command = new RegisterOrLoginCommand("auth-123", "existing@example.com", "Existing", null);

        _userRepository.GetByExternalAuthIdAsync("auth-123", Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _tokenService.GenerateTokens(existingUser)
            .Returns(new TokenResult("access-token", "refresh-token"));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsNewUser);
        Assert.Equal("access-token", result.AccessToken);
        Assert.True(existingUser.LastLoginAt >= existingUser.CreatedAt);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateEmail_ThrowsInvalidOperationException()
    {
        var command = new RegisterOrLoginCommand("auth-new", "taken@example.com", "New User", null);

        _userRepository.GetByExternalAuthIdAsync("auth-new", Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _userRepository.ExistsByEmailAsync("taken@example.com", Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command, CancellationToken.None));
    }
}
