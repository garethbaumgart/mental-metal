using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Users;

public sealed class TranscriptionProviderConfig : ValueObject
{
    public TranscriptionProvider Provider { get; }
    public string EncryptedApiKey { get; }
    public string Model { get; }

    private TranscriptionProviderConfig() // EF Core
    {
        EncryptedApiKey = null!;
        Model = null!;
    }

    public TranscriptionProviderConfig(TranscriptionProvider provider, string encryptedApiKey, string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedApiKey, nameof(encryptedApiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(model, nameof(model));

        Provider = provider;
        EncryptedApiKey = encryptedApiKey;
        Model = model;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Provider;
        yield return EncryptedApiKey;
        yield return Model;
    }
}
