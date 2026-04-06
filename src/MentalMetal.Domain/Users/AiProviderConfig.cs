using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Users;

public sealed class AiProviderConfig : ValueObject
{
    public AiProvider Provider { get; }
    public string EncryptedApiKey { get; }
    public string Model { get; }
    public int? MaxTokens { get; }

    private AiProviderConfig()  // EF Core
    {
        EncryptedApiKey = null!;
        Model = null!;
    }

    public AiProviderConfig(AiProvider provider, string encryptedApiKey, string model, int? maxTokens)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedApiKey, nameof(encryptedApiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(model, nameof(model));

        Provider = provider;
        EncryptedApiKey = encryptedApiKey;
        Model = model;
        MaxTokens = maxTokens;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Provider;
        yield return EncryptedApiKey;
        yield return Model;
        yield return MaxTokens;
    }
}
