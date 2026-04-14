using MentalMetal.Domain.Users;

namespace MentalMetal.Application.DailyCloseOut;

public sealed class GetCloseOutLogHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService)
{
    private const int DefaultLimit = 30;
    private const int MaxLimit = 90;

    public async Task<IReadOnlyList<DailyCloseOutLogDto>> HandleAsync(
        int? limit, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User not found: {userId}");

        var effective = limit.GetValueOrDefault(DefaultLimit);
        if (effective <= 0) effective = DefaultLimit;
        if (effective > MaxLimit) effective = MaxLimit;

        return user.DailyCloseOutLogs
            .OrderByDescending(l => l.Date)
            .Take(effective)
            .Select(DailyCloseOutLogDto.From)
            .ToList();
    }
}
