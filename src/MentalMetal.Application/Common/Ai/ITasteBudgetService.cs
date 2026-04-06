namespace MentalMetal.Application.Common.Ai;

public interface ITasteBudgetService
{
    Task<int> GetRemainingAsync(Guid userId, CancellationToken cancellationToken);
    Task DecrementAsync(Guid userId, CancellationToken cancellationToken);
    int DailyLimit { get; }
    bool IsEnabled { get; }
}
