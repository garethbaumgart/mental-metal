using MentalMetal.Domain.Users;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class UserRepository(MentalMetalDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByExternalAuthIdAsync(string externalAuthId, CancellationToken cancellationToken) =>
        await dbContext.Users.FirstOrDefaultAsync(u => u.ExternalAuthId == externalAuthId, cancellationToken);

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        await dbContext.Users.FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken) =>
        await dbContext.Users.AnyAsync(u => u.Email.Value == email.Value, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken) =>
        await dbContext.Users.AddAsync(user, cancellationToken);

    public void MarkOwnedAdded(object ownedEntity) =>
        dbContext.Entry(ownedEntity).State = EntityState.Added;

    public void MarkOwnedRemoved(object ownedEntity) =>
        dbContext.Entry(ownedEntity).State = EntityState.Deleted;
}
