using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Auth;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Users;
using MentalMetal.Infrastructure.Auth;
using MentalMetal.Infrastructure.Persistence;
using MentalMetal.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MentalMetal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? throw new InvalidOperationException(
                "Database connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<MentalMetalDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Infrastructure services
        services.AddHttpContextAccessor();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MentalMetalDbContext>());
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITokenService, TokenService>();

        // Application handlers
        services.AddScoped<RegisterOrLoginUserHandler>();
        services.AddScoped<GetCurrentUserHandler>();
        services.AddScoped<UpdateUserProfileHandler>();
        services.AddScoped<UpdateUserPreferencesHandler>();
        services.AddScoped<RefreshAccessTokenHandler>();
        services.AddScoped<LogoutUserHandler>();

        return services;
    }
}
