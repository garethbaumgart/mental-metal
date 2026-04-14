using MentalMetal.Application.Common;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Users;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace MentalMetal.Application.Tests.Users;

public class SetPasswordTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IPasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SetPasswordHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public SetPasswordTests()
    {
        _currentUserService.UserId.Returns(_userId);
        _handler = new SetPasswordHandler(
            _userRepository, _currentUserService, _passwordHasher, _unitOfWork);
    }

    [Fact]
    public async Task GoogleOnlyUser_SetsFirstPassword()
    {
        var user = User.Register("auth-123", "user@example.com", "Name", null);
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(user);

        await _handler.HandleAsync(new SetPasswordCommand("brand-new-pw"), CancellationToken.None);

        Assert.NotNull(user.PasswordHash);
        Assert.True(user.VerifyPassword("brand-new-pw", _passwordHasher));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UserWithExistingPassword_ReplacesIt()
    {
        var existing = Password.Create("old-password", _passwordHasher);
        var user = User.RegisterWithPassword("user@example.com", "Name", existing, null);
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(user);

        await _handler.HandleAsync(new SetPasswordCommand("new-password"), CancellationToken.None);

        Assert.True(user.VerifyPassword("new-password", _passwordHasher));
        Assert.False(user.VerifyPassword("old-password", _passwordHasher));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short")]
    [InlineData("1234567")]
    public async Task ShortPassword_Throws(string? password)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _handler.HandleAsync(new SetPasswordCommand(password!), CancellationToken.None));
    }

    [Fact]
    public async Task UserNotFound_Throws()
    {
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(new SetPasswordCommand("secret-pw"), CancellationToken.None));
    }
}
