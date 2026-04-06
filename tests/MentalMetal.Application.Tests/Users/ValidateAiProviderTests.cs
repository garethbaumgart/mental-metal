using MentalMetal.Application.Common.Ai;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Users;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MentalMetal.Application.Tests.Users;

public class ValidateAiProviderTests
{
    private readonly IAiProviderValidator _validator = Substitute.For<IAiProviderValidator>();
    private readonly ValidateAiProviderHandler _handler;

    public ValidateAiProviderTests()
    {
        _handler = new ValidateAiProviderHandler(_validator);
    }

    [Fact]
    public async Task ValidKey_ReturnsSuccess()
    {
        _validator.ValidateAsync(AiProvider.Anthropic, "sk-key", "claude-sonnet-4-20250514", Arg.Any<CancellationToken>())
            .Returns("claude-sonnet-4-20250514");

        var request = new ValidateAiProviderRequest("Anthropic", "sk-key", "claude-sonnet-4-20250514");
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("claude-sonnet-4-20250514", result.ModelName);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task InvalidKey_ReturnsSanitizedError()
    {
        _validator.ValidateAsync(AiProvider.Anthropic, "bad-key", "model", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AiProviderException(AiProvider.Anthropic, 401, "raw internal error details"));

        var request = new ValidateAiProviderRequest("Anthropic", "bad-key", "model");
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Invalid API key. Please check your key and try again.", result.Error);
    }

    [Fact]
    public async Task InvalidProvider_ReturnsFailure()
    {
        var request = new ValidateAiProviderRequest("InvalidProvider", "key", "model");
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Unsupported provider", result.Error);
    }
}
