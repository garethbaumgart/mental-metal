using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.DailyCloseOut;

public sealed class CloseOutDayHandler(
    IUserRepository userRepository,
    ICaptureRepository captureRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<DailyCloseOutLogDto> HandleAsync(
        CloseOutDayRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = currentUserService.UserId;
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User not found: {userId}");

        var date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Count all of today's triaged captures for this user, split by outcome,
        // plus remaining captures still in the close-out queue.
        var startOfDay = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDay = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var captures = await captureRepository.GetAllAsync(
            userId, typeFilter: null, statusFilter: null, cancellationToken, includeTriaged: true);

        int confirmed = 0;
        int discarded = 0;
        int remaining = 0;

        foreach (var c in captures)
        {
            if (!c.Triaged)
            {
                remaining++;
                continue;
            }

            if (c.TriagedAtUtc is null)
                continue;

            var triagedAt = c.TriagedAtUtc.Value.UtcDateTime;
            if (triagedAt < startOfDay || triagedAt > endOfDay)
                continue;

            if (c.ExtractionStatus == ExtractionStatus.Confirmed)
                confirmed++;
            else
                discarded++;
        }

        var result = user.RecordDailyCloseOut(date, confirmed, discarded, remaining);

        if (result.IsNew)
            userRepository.MarkOwnedAdded(result.Log);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return DailyCloseOutLogDto.From(result.Log);
    }
}
