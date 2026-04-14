using MentalMetal.Domain.Briefings;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Briefings;

public sealed record GetRecentBriefingsQuery(BriefingTypeDto? Type, int Limit);

public sealed class GetRecentBriefingsHandler(
    IBriefingRepository briefingRepository,
    ICurrentUserService currentUserService)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    public async Task<IReadOnlyList<BriefingSummary>> HandleAsync(
        GetRecentBriefingsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Limit < 1 || query.Limit > MaxLimit)
            throw new ArgumentOutOfRangeException(nameof(query), $"Limit must be between 1 and {MaxLimit}.");

        BriefingType? domainType = query.Type is null ? null : (BriefingType)query.Type.Value;
        var rows = await briefingRepository.ListRecentAsync(
            currentUserService.UserId, domainType, query.Limit, cancellationToken);
        return rows.Select(b => b.ToSummary()).ToList();
    }
}
