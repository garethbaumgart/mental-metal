using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Users;

public sealed class ValidateAiProviderHandler(IAiProviderValidator providerValidator)
{
    public async Task<ValidateAiProviderResponse> HandleAsync(
        ValidateAiProviderRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AiProvider>(request.Provider, ignoreCase: true, out var provider))
            return new ValidateAiProviderResponse(false, null, $"Unsupported provider: {request.Provider}");

        try
        {
            var model = await providerValidator.ValidateAsync(
                provider, request.ApiKey, request.Model, cancellationToken);

            return new ValidateAiProviderResponse(true, model, null);
        }
        catch (AiProviderException ex)
        {
            var safeMessage = ex.StatusCode switch
            {
                401 => "Invalid API key. Please check your key and try again.",
                403 => "Access denied. Your API key may not have the required permissions.",
                404 => "Model not found. Please select a different model.",
                429 => "Rate limit exceeded. Please try again later.",
                _ => "Unable to connect to the AI provider. Please check your configuration."
            };
            return new ValidateAiProviderResponse(false, null, safeMessage);
        }
    }
}
