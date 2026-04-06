using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MentalMetal.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MentalMetalDbContext>
{
    public MentalMetalDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (connectionString is not null &&
            connectionString.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = DependencyInjection.ConvertDatabaseUrl(connectionString);
        }

        connectionString ??= "Host=localhost;Port=5432;Database=mentalmetal;Username=mentalmetal;Password=devTestPassword";

        var optionsBuilder = new DbContextOptionsBuilder<MentalMetalDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new MentalMetalDbContext(optionsBuilder.Options, null!);
    }
}
