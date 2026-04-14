using MentalMetal.Application.Common;
using MentalMetal.Domain.Briefings;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Goals;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.Interviews;
using MentalMetal.Domain.Observations;
using MentalMetal.Domain.OneOnOnes;
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
    public DbSet<Initiative> Initiatives => Set<Initiative>();
    public DbSet<Capture> Captures => Set<Capture>();
    public DbSet<Commitment> Commitments => Set<Commitment>();
    public DbSet<Delegation> Delegations => Set<Delegation>();
    public DbSet<AiTasteBudget> AiTasteBudgets => Set<AiTasteBudget>();
    public DbSet<PendingBriefUpdate> PendingBriefUpdates => Set<PendingBriefUpdate>();
    public DbSet<ChatThread> ChatThreads => Set<ChatThread>();
    public DbSet<OneOnOne> OneOnOnes => Set<OneOnOne>();
    public DbSet<Observation> Observations => Set<Observation>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Briefing> Briefings => Set<Briefing>();
    public DbSet<Interview> Interviews => Set<Interview>();

    public void DiscardPendingChanges() => ChangeTracker.Clear();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MentalMetalDbContext).Assembly);

        // Global query filters for multi-tenant isolation.
        // Uses closures over currentUserService so EF Core evaluates lazily at query time.
        modelBuilder.Entity<Person>().HasQueryFilter(p => p.UserId == currentUserService.UserId);
        modelBuilder.Entity<Initiative>().HasQueryFilter(i => i.UserId == currentUserService.UserId);
        modelBuilder.Entity<Capture>().HasQueryFilter(c => c.UserId == currentUserService.UserId);
        modelBuilder.Entity<Commitment>().HasQueryFilter(c => c.UserId == currentUserService.UserId);
        modelBuilder.Entity<Delegation>().HasQueryFilter(d => d.UserId == currentUserService.UserId);
        modelBuilder.Entity<PendingBriefUpdate>().HasQueryFilter(p => p.UserId == currentUserService.UserId);
        modelBuilder.Entity<ChatThread>().HasQueryFilter(t => t.UserId == currentUserService.UserId);
        modelBuilder.Entity<OneOnOne>().HasQueryFilter(o => o.UserId == currentUserService.UserId);
        modelBuilder.Entity<Observation>().HasQueryFilter(o => o.UserId == currentUserService.UserId);
        modelBuilder.Entity<Goal>().HasQueryFilter(g => g.UserId == currentUserService.UserId);
        modelBuilder.Entity<Briefing>().HasQueryFilter(b => b.UserId == currentUserService.UserId);
        modelBuilder.Entity<Interview>().HasQueryFilter(i => i.UserId == currentUserService.UserId);
    }
}
