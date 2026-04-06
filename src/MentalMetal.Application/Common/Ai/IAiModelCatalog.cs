using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Common.Ai;

public interface IAiModelCatalog
{
    IReadOnlyList<AiModelInfo> GetModels(AiProvider provider);
    string GetDefaultModel(AiProvider provider);
}

public sealed record AiModelInfo(string Id, string Name, bool IsDefault);
