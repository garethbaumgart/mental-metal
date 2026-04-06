using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Users;

public sealed class GetAvailableModelsHandler(IAiModelCatalog modelCatalog)
{
    public AvailableModelsResponse Handle(string providerName)
    {
        if (!Enum.TryParse<AiProvider>(providerName, ignoreCase: true, out var provider))
            throw new ArgumentException($"Unsupported AI provider: {providerName}");

        var models = modelCatalog.GetModels(provider);

        return new AvailableModelsResponse(
            Provider: provider.ToString(),
            Models: models.Select(m => new ModelInfo(m.Id, m.Name, m.IsDefault)).ToList());
    }
}
