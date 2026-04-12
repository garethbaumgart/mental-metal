using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Application.Common.Auth;
using MentalMetal.Application.People;
using MentalMetal.Application.Users;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using MentalMetal.Infrastructure.Ai;
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
        services.AddOptions<AiProviderSettings>()
            .Bind(configuration.GetSection(AiProviderSettings.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.EncryptionKey),
                "AiProvider:EncryptionKey is required. Generate with: openssl rand -base64 32")
            .ValidateOnStart();

        // Infrastructure services
        services.AddHttpContextAccessor();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MentalMetalDbContext>());
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<ITokenService, TokenService>();

        // AI provider services
        services.AddHttpClient("GoogleAi");
        services.AddSingleton<IApiKeyEncryptionService, AesApiKeyEncryptionService>();
        services.AddSingleton<AiModelCatalog>();
        services.AddSingleton<IAiModelCatalog>(sp => sp.GetRequiredService<AiModelCatalog>());
        services.AddSingleton<AnthropicAdapter>();
        services.AddSingleton<OpenAiAdapter>();
        services.AddSingleton<GoogleAdapter>();
        services.AddScoped<IAiCompletionService, AiCompletionService>();
        services.AddScoped<IAiProviderValidator, AiProviderValidator>();
        services.AddScoped<ITasteBudgetService, TasteBudgetService>();

        // Application handlers
        services.AddScoped<RegisterOrLoginUserHandler>();
        services.AddScoped<GetCurrentUserHandler>();
        services.AddScoped<UpdateUserProfileHandler>();
        services.AddScoped<UpdateUserPreferencesHandler>();
        services.AddScoped<RefreshAccessTokenHandler>();
        services.AddScoped<LogoutUserHandler>();
        services.AddScoped<ConfigureAiProviderHandler>();
        services.AddScoped<GetAiProviderStatusHandler>();
        services.AddScoped<ValidateAiProviderHandler>();
        services.AddScoped<RemoveAiProviderHandler>();
        services.AddSingleton<GetAvailableModelsHandler>();

        // Person handlers
        services.AddScoped<CreatePersonHandler>();
        services.AddScoped<GetPersonHandler>();
        services.AddScoped<GetPeopleHandler>();
        services.AddScoped<UpdatePersonProfileHandler>();
        services.AddScoped<ChangePersonTypeHandler>();
        services.AddScoped<UpdateCareerDetailsHandler>();
        services.AddScoped<UpdateCandidateDetailsHandler>();
        services.AddScoped<AdvanceCandidatePipelineHandler>();
        services.AddScoped<ArchivePersonHandler>();

        return services;
    }

    internal static string? ConvertDatabaseUrl(string? url)
    {
        if (url is null || !url.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
            return url;

        // Parse postgres:// URI manually — .NET's Uri class doesn't handle the postgres scheme reliably
        var match = System.Text.RegularExpressions.Regex.Match(url.Trim(),
            @"^postgres(?:ql)?://([^:]+):([^@]+)@([^/:]+)(?::(\d+))?/([^?]+)(?:\?(.*))?$");

        if (!match.Success)
            throw new InvalidOperationException(
                $"DATABASE_URL is not a valid postgres:// URI (length={url.Length}, starts='{url[..Math.Min(20, url.Length)]}...')");

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
            var channelBinding = query["channel_binding"];
            if (channelBinding is not null)
                builder.ChannelBinding = Enum.Parse<Npgsql.ChannelBinding>(channelBinding, ignoreCase: true);
        }

        return builder.ConnectionString;
    }
}
