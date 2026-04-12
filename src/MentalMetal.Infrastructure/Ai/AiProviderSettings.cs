namespace MentalMetal.Infrastructure.Ai;

public sealed class AiProviderSettings
{
    public const string SectionName = "AiProvider";

    public string EncryptionKey { get; set; } = null!;

    public TasteKeySettings? TasteKey { get; set; }

    public Dictionary<string, ProviderModelSettings> Providers { get; set; } = new();
}

public sealed class TasteKeySettings
{
    public string Provider { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public int DailyLimitPerUser { get; set; } = 5;
}

public sealed class ProviderModelSettings
{
    public string DefaultModel { get; set; } = null!;
    public List<string> FallbackModels { get; set; } = [];
}
