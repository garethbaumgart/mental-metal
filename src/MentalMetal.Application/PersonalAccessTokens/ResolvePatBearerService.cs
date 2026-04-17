using MentalMetal.Application.Common;
using MentalMetal.Domain.PersonalAccessTokens;

namespace MentalMetal.Application.PersonalAccessTokens;

public sealed record PatResolution(Guid UserId, HashSet<string> Scopes);

public sealed class ResolvePatBearerService(
    IPersonalAccessTokenRepository repository,
    IPatTokenHasher hasher,
    IUnitOfWork unitOfWork)
{
    public async Task<PatResolution?> ResolveAsync(string plaintext, CancellationToken cancellationToken)
    {
        var (hash, prefix) = hasher.HashToken(plaintext);

        var candidates = await repository.GetByLookupPrefixAsync(prefix, cancellationToken);
        if (candidates.Count == 0)
            return null;

        foreach (var candidate in candidates)
        {
            if (!hasher.Verify(plaintext, candidate.TokenHash))
                continue;

            if (!candidate.IsActive)
                return null;

            candidate.TouchLastUsed();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new PatResolution(candidate.UserId, candidate.Scopes);
        }

        return null;
    }
}
