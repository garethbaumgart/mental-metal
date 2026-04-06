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
            ?? ConvertDatabaseUrl(Environment.GetEnvironmentVariable("DATABASE_URL"))
            ?? throw new InvalidOperationException(
                "Database connection string is not configured. Set 'ConnectionStrings:DefaultConnection' or 'DATABASE_URL'.");

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

    internal static string? ConvertDatabaseUrl(string? url)
    {
        if (url is null || !url.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
            return url;

        // Parse postgres:// URI manually — .NET's Uri class doesn't handle the postgres scheme reliably
        var match = System.Text.RegularExpressions.Regex.Match(url,
            @"^postgres(?:ql)?://([^:]+):([^@]+)@([^/:]+)(?::(\d+))?/([^?]+)(?:\?(.*))?$");

        if (!match.Success)
            return url;

        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = match.Groups[3].Value,
            Port = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 5432,
            Database = match.Groups[5].Value,
            Username = Uri.UnescapeDataString(match.Groups[1].Value),
            Password = Uri.UnescapeDataString(match.Groups[2].Value),
        };

        if (match.Groups[6].Success)
        {
            var query = System.Web.HttpUtility.ParseQueryString(match.Groups[6].Value);
            var sslMode = query["sslmode"];
            if (sslMode is not null)
                builder.SslMode = Enum.Parse<Npgsql.SslMode>(sslMode, ignoreCase: true);
        }

        return builder.ConnectionString;
    }
}
