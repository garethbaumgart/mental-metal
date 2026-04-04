using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Persistence;

public sealed class MentalMetalDbContext(DbContextOptions<MentalMetalDbContext> options)
    : DbContext(options)
{
    // No entities yet — will be added as domain models are created.
}
