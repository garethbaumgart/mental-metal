using MentalMetal.Domain.PersonalAccessTokens;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.PersonalAccessTokens;

public sealed class ListPersonalAccessTokensHandler(
    IPersonalAccessTokenRepository repository,
    ICurrentUserService currentUserService)
{
    public async Task<IReadOnlyList<PatSummaryResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var tokens = await repository.ListForUserAsync(currentUserService.UserId, cancellationToken);
        return tokens.Select(t => new PatSummaryResponse(
            t.Id,
            t.Name,
            new HashSet<string>(t.Scopes),
            t.CreatedAt,
            t.LastUsedAt,
            t.RevokedAt)).ToList();
    }
}
