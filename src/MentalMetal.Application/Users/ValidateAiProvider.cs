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
            return new ValidateAiProviderResponse(false, null, ex.Message);
        }
    }
}
