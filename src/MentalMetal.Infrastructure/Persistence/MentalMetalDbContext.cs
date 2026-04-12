using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.People;
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
    public DbSet<Person> People => Set<Person>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AiTasteBudget> AiTasteBudgets => Set<AiTasteBudget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MentalMetalDbContext).Assembly);

        // Global query filters for multi-tenant isolation.
        // Uses closures over currentUserService so EF Core evaluates lazily at query time.
        modelBuilder.Entity<Person>().HasQueryFilter(p => p.UserId == currentUserService.UserId);
    }
}
