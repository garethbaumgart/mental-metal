using MentalMetal.Domain.Nudges;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Nudges;

public sealed class ListNudgesHandler(
    INudgeRepository nudgeRepository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<NudgeResponse>> HandleAsync(ListNudgesFilters filters, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var nudges = await nudgeRepository.GetAllAsync(
            currentUserService.UserId,
            filters.IsActive,
            filters.PersonId,
            filters.InitiativeId,
            filters.DueBefore,
            filters.DueWithinDays,
            today,
            ct);
        return nudges.Select(NudgeResponse.From).ToList();
    }
}
