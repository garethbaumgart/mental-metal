using MentalMetal.Application.Common;
using MentalMetal.Domain.Users;
using MentalMetal.Infrastructure.Ai;
using MentalMetal.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Persistence;

public sealed class MentalMetalDbContext(
    DbContextOptions<MentalMetalDbContext> options,
    ICurrentUserService currentUserService)
    : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AiTasteBudget> AiTasteBudgets => Set<AiTasteBudget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MentalMetalDbContext).Assembly);

        // Global query filter for multi-tenant isolation.
        // User aggregate is NOT filtered by IUserScoped (it IS the tenant root).
        // All future aggregates implementing IUserScoped will be auto-filtered here.
    }
}
