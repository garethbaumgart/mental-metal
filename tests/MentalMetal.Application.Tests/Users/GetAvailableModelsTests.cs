using MentalMetal.Application.Common.Ai;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Users;

public class GetAvailableModelsTests
{
    private readonly IAiModelCatalog _modelCatalog = Substitute.For<IAiModelCatalog>();
    private readonly GetAvailableModelsHandler _handler;

    public GetAvailableModelsTests()
    {
        _handler = new GetAvailableModelsHandler(_modelCatalog);
    }

    [Fact]
    public void ValidProvider_ReturnsModels()
    {
        var models = new List<AiModelInfo>
        {
            new("claude-sonnet-4-20250514", "Claude Sonnet 4", true),
            new("claude-haiku-4-5", "Claude Haiku 4.5", false)
        };
        _modelCatalog.GetModels(AiProvider.Anthropic).Returns(models);

        var result = _handler.Handle("Anthropic");

        Assert.Equal("Anthropic", result.Provider);
        Assert.Equal(2, result.Models.Count);
        Assert.True(result.Models[0].IsDefault);
    }

    [Fact]
    public void InvalidProvider_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _handler.Handle("InvalidProvider"));
    }

    [Fact]
    public void CaseInsensitiveProvider_Works()
    {
        _modelCatalog.GetModels(AiProvider.OpenAI).Returns(new List<AiModelInfo>());

        var result = _handler.Handle("openai");

        Assert.Equal("OpenAI", result.Provider);
    }
}
