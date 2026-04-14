using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Auth;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Users;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace MentalMetal.Application.Tests.Users;

public class RegisterWithPasswordTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IPasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegisterWithPasswordHandler _handler;

    public RegisterWithPasswordTests()
    {
        _handler = new RegisterWithPasswordHandler(
            _userRepository, _tokenService, _passwordHasher, _unitOfWork);
    }

    [Fact]
    public async Task HappyPath_RegistersUserAndReturnsTokens()
    {
        var command = new RegisterWithPasswordCommand("new@example.com", "secret-pw", "New User");

        _userRepository.ExistsByEmailAsync("new@example.com", Arg.Any<CancellationToken>())
            .Returns(false);
        _tokenService.GenerateTokens(Arg.Any<User>())
            .Returns(new TokenResult("access-token", "refresh-token"));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.True(result.User.HasPassword);
        Assert.False(result.User.HasAiProvider);
        Assert.Equal("new@example.com", result.User.Email);
        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateEmail_ThrowsEmailAlreadyInUseException()
    {
        var command = new RegisterWithPasswordCommand("taken@example.com", "secret-pw", "N");

        _userRepository.ExistsByEmailAsync("taken@example.com", Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<EmailAlreadyInUseException>(
            () => _handler.HandleAsync(command, CancellationToken.None));

        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short")]
    [InlineData("1234567")]
    public async Task ShortPassword_Throws(string password)
    {
        var command = new RegisterWithPasswordCommand("new@example.com", password, "N");

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _handler.HandleAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task InvalidEmail_Throws()
    {
        var command = new RegisterWithPasswordCommand("not-an-email", "secret-pw", "N");

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _handler.HandleAsync(command, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task EmptyName_Throws(string name)
    {
        var command = new RegisterWithPasswordCommand("new@example.com", "secret-pw", name);

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _handler.HandleAsync(command, CancellationToken.None));
    }
}
