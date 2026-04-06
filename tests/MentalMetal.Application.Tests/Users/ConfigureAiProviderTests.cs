using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Users;

public class ConfigureAiProviderTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IApiKeyEncryptionService _encryptionService = Substitute.For<IApiKeyEncryptionService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ConfigureAiProviderHandler _handler;

    public ConfigureAiProviderTests()
    {
        _handler = new ConfigureAiProviderHandler(
            _userRepository, _currentUserService, _encryptionService, _unitOfWork);
    }

    [Fact]
    public async Task ValidRequest_EncryptsKeyAndConfigures()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);
        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _encryptionService.Encrypt("sk-ant-plainkey").Returns("encrypted_value");

        var request = new ConfigureAiProviderRequest("Anthropic", "sk-ant-plainkey", "claude-sonnet-4-20250514", null);

        await _handler.HandleAsync(request, CancellationToken.None);

        Assert.NotNull(user.AiProviderConfig);
        Assert.Equal(AiProvider.Anthropic, user.AiProviderConfig.Provider);
        Assert.Equal("encrypted_value", user.AiProviderConfig.EncryptedApiKey);
        Assert.Equal("claude-sonnet-4-20250514", user.AiProviderConfig.Model);
        _encryptionService.Received(1).Encrypt("sk-ant-plainkey");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CaseInsensitiveProvider_Works()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);
        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _encryptionService.Encrypt(Arg.Any<string>()).Returns("encrypted");

        var request = new ConfigureAiProviderRequest("anthropic", "key", "model", null);

        await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(AiProvider.Anthropic, user.AiProviderConfig!.Provider);
    }

    [Fact]
    public async Task InvalidProvider_ThrowsArgumentException()
    {
        var user = User.Register("auth-123", "test@example.com", "Name", null);
        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var request = new ConfigureAiProviderRequest("InvalidProvider", "key", "model", null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.HandleAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task UserNotFound_ThrowsInvalidOperationException()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var request = new ConfigureAiProviderRequest("Anthropic", "key", "model", null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(request, CancellationToken.None));
    }
}
