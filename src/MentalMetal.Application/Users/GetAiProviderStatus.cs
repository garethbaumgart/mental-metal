using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Users;

public sealed class GetAiProviderStatusHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    ITasteBudgetService tasteBudgetService)
{
    public async Task<AiProviderStatusResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            currentUserService.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user not found.");

        var config = user.AiProviderConfig;

        // Only query taste budget if user doesn't have their own key and taste is enabled
        var remaining = config is null && tasteBudgetService.IsEnabled
            ? await tasteBudgetService.GetRemainingAsync(user.Id, cancellationToken)
            : tasteBudgetService.DailyLimit;

        return new AiProviderStatusResponse(
            IsConfigured: config is not null,
            Provider: config?.Provider.ToString(),
            Model: config?.Model,
            MaxTokens: config?.MaxTokens,
            TasteBudget: new TasteBudgetDto(
                Remaining: remaining,
                DailyLimit: tasteBudgetService.DailyLimit,
                IsEnabled: tasteBudgetService.IsEnabled));
    }
}
