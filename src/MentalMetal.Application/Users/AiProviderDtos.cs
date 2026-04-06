namespace MentalMetal.Application.Users;

public sealed record ConfigureAiProviderRequest(
    string Provider,
    string ApiKey,
    string Model,
    int? MaxTokens = null);

public sealed record AiProviderStatusResponse(
    bool IsConfigured,
    string? Provider,
    string? Model,
    int? MaxTokens,
    TasteBudgetDto TasteBudget);

public sealed record TasteBudgetDto(
    int Remaining,
    int DailyLimit,
    bool IsEnabled);

public sealed record ValidateAiProviderRequest(
    string Provider,
    string ApiKey,
    string Model);

public sealed record ValidateAiProviderResponse(
    bool Success,
    string? ModelName,
    string? Error);

public sealed record ModelInfo(
    string Id,
    string Name,
    bool IsDefault);

public sealed record AvailableModelsResponse(
    string Provider,
    List<ModelInfo> Models);
