using MentalMetal.Domain.People;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class PersonRepository(MentalMetalDbContext dbContext) : IPersonRepository
{
    public async Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.People.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Person>> GetByIdsAsync(
        Guid userId, IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        // Use List<T>.Contains (translatable by EF) rather than HashSet.Contains.
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return [];

        return await dbContext.People
            .Where(p => p.UserId == userId && idList.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetAllAsync(
        Guid userId, PersonType? typeFilter, bool includeArchived, CancellationToken cancellationToken)
    {
        var query = dbContext.People.Where(p => p.UserId == userId);

        if (!includeArchived)
            query = query.Where(p => !p.IsArchived);

        if (typeFilter is not null)
            query = query.Where(p => p.Type == typeFilter.Value);

        return await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        Guid userId, string name, Guid? excludeId, CancellationToken cancellationToken)
    {
        var query = dbContext.People
            .Where(p => p.UserId == userId)
            .Where(p => !p.IsArchived)
            .Where(p => p.Name.ToLower() == name.Trim().ToLower());

        if (excludeId is not null)
            query = query.Where(p => p.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> AliasExistsForOtherPersonAsync(
        Guid userId, string alias, Guid excludePersonId, CancellationToken cancellationToken)
    {
        // Note: This is a simple implementation. For production, a more efficient
        // query against the JSONB aliases column would be used.
        var people = await dbContext.People
            .Where(p => p.UserId == userId && p.Id != excludePersonId && !p.IsArchived)
            .ToListAsync(cancellationToken);

        return people.Any(p => p.Aliases.Any(a =>
            string.Equals(a, alias.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    public async Task AddAsync(Person person, CancellationToken cancellationToken) =>
        await dbContext.People.AddAsync(person, cancellationToken);
}
