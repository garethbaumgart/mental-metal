using MentalMetal.Domain.PersonalAccessTokens;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class PersonalAccessTokenRepository(MentalMetalDbContext dbContext) : IPersonalAccessTokenRepository
{
    public async Task AddAsync(PersonalAccessToken token, CancellationToken cancellationToken) =>
        await dbContext.PersonalAccessTokens.AddAsync(token, cancellationToken);

    public async Task<PersonalAccessToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.PersonalAccessTokens.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PersonalAccessToken>> GetByLookupPrefixAsync(byte[] prefix, CancellationToken cancellationToken) =>
        await dbContext.PersonalAccessTokens
            .IgnoreQueryFilters()
            .Where(t => t.TokenLookupPrefix == prefix)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PersonalAccessToken>> ListForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.PersonalAccessTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
}
