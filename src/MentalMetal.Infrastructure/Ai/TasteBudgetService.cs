using MentalMetal.Application.Common.Ai;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MentalMetal.Infrastructure.Ai;

public sealed class TasteBudgetService(
    MentalMetalDbContext dbContext,
    IOptions<AiProviderSettings> settings) : ITasteBudgetService
{
    private readonly TasteKeySettings? _tasteSettings = settings.Value.TasteKey;

    public int DailyLimit => _tasteSettings?.DailyLimitPerUser ?? 5;

    public bool IsEnabled => _tasteSettings is not null;

    public async Task<int> GetRemainingAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
            return 0;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var budget = await dbContext.AiTasteBudgets
            .FirstOrDefaultAsync(b => b.UserId == userId && b.Date == today, cancellationToken);

        return Math.Max(0, DailyLimit - (budget?.OperationsUsed ?? 0));
    }

    public async Task DecrementAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Upsert: insert or increment, conflict-safe against the unique (UserId, Date) index
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "AiTasteBudgets" ("Id", "UserId", "Date", "OperationsUsed")
            VALUES ({Guid.NewGuid()}, {userId}, {today}, 1)
            ON CONFLICT ("UserId", "Date")
            DO UPDATE SET "OperationsUsed" = "AiTasteBudgets"."OperationsUsed" + 1
            """, cancellationToken);
    }
}
