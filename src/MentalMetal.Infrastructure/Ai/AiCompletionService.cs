using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MentalMetal.Infrastructure.Ai;

public sealed class AiCompletionService(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IApiKeyEncryptionService encryptionService,
    ITasteBudgetService tasteBudgetService,
    AiModelCatalog modelCatalog,
    AnthropicAdapter anthropicAdapter,
    OpenAiAdapter openAiAdapter,
    GoogleAdapter googleAdapter,
    IOptions<AiProviderSettings> settings,
    ILogger<AiCompletionService> logger) : IAiCompletionService
{
    private readonly AiProviderSettings _settings = settings.Value;

    public async Task<AiCompletionResult> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            currentUserService.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user not found.");

        var (apiKey, provider, model, isTaste) = await ResolveConfigAsync(user, cancellationToken);

        var adapter = GetAdapter(provider);
        var fallbackModels = modelCatalog.GetFallbackModels(provider);

        // Try primary model, then fallbacks on model-not-found
        var modelsToTry = new List<string> { model };
        modelsToTry.AddRange(fallbackModels.Where(m => m != model));

        foreach (var candidateModel in modelsToTry)
        {
            try
            {
                var result = await adapter.CompleteAsync(apiKey, candidateModel, request, cancellationToken);

                if (candidateModel != model)
                    logger.LogWarning("Model fallback: {OriginalModel} -> {FallbackModel} for provider {Provider}",
                        model, candidateModel, provider);

                if (isTaste)
                    await tasteBudgetService.DecrementAsync(user.Id, cancellationToken);

                return result;
            }
            catch (AiProviderException ex) when (ex.StatusCode == 404 && candidateModel != modelsToTry[^1])
            {
                logger.LogWarning("Model {Model} not found for {Provider}, trying fallback",
                    candidateModel, provider);
            }
        }

        // Unreachable in practice — the last model attempt throws without the catch filter matching.
        // Required by the compiler since foreach doesn't guarantee iteration.
        throw new AiProviderException(provider, null, "All configured models are unavailable.");
    }

    private async Task<(string ApiKey, AiProvider Provider, string Model, bool IsTaste)> ResolveConfigAsync(
        User user, CancellationToken cancellationToken)
    {
        var config = user.AiProviderConfig;

        if (config is not null)
        {
            var apiKey = encryptionService.Decrypt(config.EncryptedApiKey);
            return (apiKey, config.Provider, config.Model, false);
        }

        // Fall back to taste key
        if (_settings.TasteKey is null)
            throw new AiNotConfiguredException();

        if (!tasteBudgetService.IsEnabled)
            throw new AiNotConfiguredException();

        var remaining = await tasteBudgetService.GetRemainingAsync(user.Id, cancellationToken);
        if (remaining <= 0)
            throw new TasteLimitExceededException();

        if (!Enum.TryParse<AiProvider>(_settings.TasteKey.Provider, ignoreCase: true, out var tasteProvider))
            throw new InvalidOperationException($"Invalid taste key provider: {_settings.TasteKey.Provider}");

        var tasteModel = modelCatalog.GetDefaultModel(tasteProvider);

        return (_settings.TasteKey.ApiKey, tasteProvider, tasteModel, true);
    }

    private IAiProviderAdapter GetAdapter(AiProvider provider) => provider switch
    {
        AiProvider.Anthropic => anthropicAdapter,
        AiProvider.OpenAI => openAiAdapter,
        AiProvider.Google => googleAdapter,
        _ => throw new ArgumentOutOfRangeException(nameof(provider))
    };
}
