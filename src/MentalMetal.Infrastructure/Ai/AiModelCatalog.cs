using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Options;

namespace MentalMetal.Infrastructure.Ai;

public sealed class AiModelCatalog(IOptions<AiProviderSettings> settings) : IAiModelCatalog
{
    private readonly AiProviderSettings _settings = settings.Value;

    public IReadOnlyList<AiModelInfo> GetModels(AiProvider provider)
    {
        var providerName = provider.ToString();
        if (!_settings.Providers.TryGetValue(providerName, out var providerSettings))
            return [];

        var defaultModel = providerSettings.DefaultModel;
        var models = new List<AiModelInfo>
        {
            new(defaultModel, FormatModelName(defaultModel), true)
        };

        foreach (var fallback in providerSettings.FallbackModels)
        {
            models.Add(new AiModelInfo(fallback, FormatModelName(fallback), false));
        }

        return models;
    }

    public string GetDefaultModel(AiProvider provider)
    {
        var providerName = provider.ToString();
        return _settings.Providers.TryGetValue(providerName, out var providerSettings)
            ? providerSettings.DefaultModel
            : throw new InvalidOperationException($"No model configuration for provider: {providerName}");
    }

    internal IReadOnlyList<string> GetFallbackModels(AiProvider provider)
    {
        var providerName = provider.ToString();
        return _settings.Providers.TryGetValue(providerName, out var providerSettings)
            ? providerSettings.FallbackModels
            : [];
    }

    private static string FormatModelName(string modelId)
    {
        // Convert "claude-sonnet-4-20250514" to "Claude Sonnet 4 20250514" etc.
        return string.Join(' ', modelId.Split('-').Select(p =>
            p.Length > 0 ? char.ToUpperInvariant(p[0]) + p[1..] : p));
    }
}
