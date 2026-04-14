using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Auth;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Users;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace MentalMetal.Application.Tests.Users;

public class LoginWithPasswordTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IPasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly LoginWithPasswordHandler _handler;

    public LoginWithPasswordTests()
    {
        _handler = new LoginWithPasswordHandler(
            _userRepository, _tokenService, _passwordHasher, _unitOfWork);
    }

    private User CreatePasswordUser(string email, string password)
    {
        var pw = Password.Create(password, _passwordHasher);
        return User.RegisterWithPassword(email, "Test User", pw, null);
    }

    [Fact]
    public async Task HappyPath_ReturnsTokens()
    {
        var user = CreatePasswordUser("user@example.com", "secret-pw");
        _userRepository.GetByEmailAsync(Arg.Is<Email>(e => e.Value == "user@example.com"), Arg.Any<CancellationToken>())
            .Returns(user);
        _tokenService.GenerateTokens(user)
            .Returns(new TokenResult("access-token", "refresh-token"));

        var result = await _handler.HandleAsync(
            new LoginWithPasswordCommand("user@example.com", "secret-pw"), CancellationToken.None);

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.True(result.User.HasPassword);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownEmail_ThrowsInvalidCredentials()
    {
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.HandleAsync(
                new LoginWithPasswordCommand("nobody@example.com", "secret-pw"),
                CancellationToken.None));
    }

    [Fact]
    public async Task WrongPassword_ThrowsInvalidCredentials()
    {
        var user = CreatePasswordUser("user@example.com", "correct-pw");
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.HandleAsync(
                new LoginWithPasswordCommand("user@example.com", "wrong-pw"),
                CancellationToken.None));
    }

    [Fact]
    public async Task GoogleOnlyUser_ThrowsInvalidCredentials()
    {
        var user = User.Register("auth-123", "user@example.com", "Name", null);
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.HandleAsync(
                new LoginWithPasswordCommand("user@example.com", "anything-pw"),
                CancellationToken.None));
    }

    [Theory]
    [InlineData("", "pw-12345")]
    [InlineData("user@example.com", "")]
    [InlineData("not-an-email", "pw-12345")]
    public async Task BlankOrInvalidInputs_ThrowInvalidCredentials(string email, string password)
    {
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.HandleAsync(
                new LoginWithPasswordCommand(email, password),
                CancellationToken.None));
    }
}
