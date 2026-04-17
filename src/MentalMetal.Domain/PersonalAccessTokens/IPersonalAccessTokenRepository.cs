namespace MentalMetal.Domain.PersonalAccessTokens;

public interface IPersonalAccessTokenRepository
{
    Task AddAsync(PersonalAccessToken token, CancellationToken cancellationToken);
    Task<PersonalAccessToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<PersonalAccessToken>> GetByLookupPrefixAsync(byte[] prefix, CancellationToken cancellationToken);
    Task<IReadOnlyList<PersonalAccessToken>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);
}
