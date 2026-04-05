using MentalMetal.Domain.Users;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class UserRepository(MentalMetalDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByExternalAuthIdAsync(string externalAuthId, CancellationToken cancellationToken) =>
        await dbContext.Users.FirstOrDefaultAsync(u => u.ExternalAuthId == externalAuthId, cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) =>
        await dbContext.Users.AnyAsync(u => u.Email.Value == email.ToLower(), cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken) =>
        await dbContext.Users.AddAsync(user, cancellationToken);
}
