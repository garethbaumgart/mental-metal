using MentalMetal.Domain.Briefings;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Briefings;

public sealed record GetBriefingQuery(Guid BriefingId);

public sealed class GetBriefingHandler(
    IBriefingRepository briefingRepository,
    ICurrentUserService currentUserService)
{
    /// <summary>
    /// Returns null when the briefing does not exist or does not belong to the user;
    /// the endpoint maps that to HTTP 404.
    /// </summary>
    public async Task<BriefingResponse?> HandleAsync(
        GetBriefingQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var briefing = await briefingRepository.GetByIdAsync(
            currentUserService.UserId, query.BriefingId, cancellationToken);
        return briefing?.ToResponse();
    }
}
