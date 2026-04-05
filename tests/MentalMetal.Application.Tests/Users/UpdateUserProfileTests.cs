using MentalMetal.Application.Common;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Users;

public class UpdateUserProfileTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateUserProfileHandler _handler;

    public UpdateUserProfileTests()
    {
        _handler = new UpdateUserProfileHandler(_userRepository, _currentUserService, _unitOfWork);
    }

    [Fact]
    public async Task ValidRequest_UpdatesProfile()
    {
        var user = User.Register("auth-123", "test@example.com", "Original", null);
        var userId = user.Id;

        _currentUserService.UserId.Returns(userId);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var request = new UpdateProfileRequest("Updated Name", "https://avatar.url", "Australia/Sydney");

        await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal("Updated Name", user.Name);
        Assert.Equal("Australia/Sydney", user.Timezone);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmptyName_ThrowsArgumentException()
    {
        var user = User.Register("auth-123", "test@example.com", "Original", null);

        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var request = new UpdateProfileRequest("", null, "UTC");

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _handler.HandleAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task InvalidTimezone_ThrowsArgumentException()
    {
        var user = User.Register("auth-123", "test@example.com", "Original", null);

        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var request = new UpdateProfileRequest("Name", null, "Invalid/Zone");

        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.HandleAsync(request, CancellationToken.None));
    }
}
