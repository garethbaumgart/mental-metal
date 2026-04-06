using MentalMetal.Domain.Users;

namespace MentalMetal.Domain.Tests.Users;

public class AiProviderConfigTests
{
    [Fact]
    public void Create_ValidInput_SetsProperties()
    {
        var config = new AiProviderConfig(AiProvider.Anthropic, "enc_key", "claude-sonnet-4-20250514", 4096);

        Assert.Equal(AiProvider.Anthropic, config.Provider);
        Assert.Equal("enc_key", config.EncryptedApiKey);
        Assert.Equal("claude-sonnet-4-20250514", config.Model);
        Assert.Equal(4096, config.MaxTokens);
    }

    [Fact]
    public void Create_NullMaxTokens_Allowed()
    {
        var config = new AiProviderConfig(AiProvider.OpenAI, "enc_key", "gpt-4o", null);

        Assert.Null(config.MaxTokens);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyApiKey_Throws(string? key)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new AiProviderConfig(AiProvider.Anthropic, key!, "model", null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyModel_Throws(string? model)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new AiProviderConfig(AiProvider.Anthropic, "enc_key", model!, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ZeroOrNegativeMaxTokens_Allowed(int maxTokens)
    {
        // Domain doesn't enforce MaxTokens range — provider APIs will reject invalid values
        var config = new AiProviderConfig(AiProvider.Anthropic, "enc_key", "model", maxTokens);
        Assert.Equal(maxTokens, config.MaxTokens);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new AiProviderConfig(AiProvider.Anthropic, "enc_key", "model", 4096);
        var b = new AiProviderConfig(AiProvider.Anthropic, "enc_key", "model", 4096);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentProvider_AreNotEqual()
    {
        var a = new AiProviderConfig(AiProvider.Anthropic, "enc_key", "model", null);
        var b = new AiProviderConfig(AiProvider.OpenAI, "enc_key", "model", null);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equality_DifferentModel_AreNotEqual()
    {
        var a = new AiProviderConfig(AiProvider.Anthropic, "enc_key", "model-a", null);
        var b = new AiProviderConfig(AiProvider.Anthropic, "enc_key", "model-b", null);

        Assert.NotEqual(a, b);
    }
}
