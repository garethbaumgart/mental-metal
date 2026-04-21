using MentalMetal.Application.Briefings;
using MentalMetal.Application.Captures;
using MentalMetal.Application.Captures.AutoExtract;
using MentalMetal.Application.Captures.ImportCapture;
using MentalMetal.Application.Commitments;
using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Application.Common.Auth;
using MentalMetal.Application.Initiatives;
using MentalMetal.Application.People;
using MentalMetal.Application.People.Dossier;
using MentalMetal.Application.PersonalAccessTokens;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.PersonalAccessTokens;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using MentalMetal.Infrastructure.Ai;
using MentalMetal.Infrastructure.Auth;
using MentalMetal.Infrastructure.Caching;
using MentalMetal.Infrastructure.Persistence;
using MentalMetal.Infrastructure.Repositories;
using MentalMetal.Infrastructure.Parsers;
using MentalMetal.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddOptions<AudioBlobStoreOptions>()
            .Bind(configuration.GetSection(AudioBlobStoreOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<AiProviderSettings>()
            .Bind(configuration.GetSection(AiProviderSettings.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.EncryptionKey),
                "AiProvider:EncryptionKey is required. Generate with: openssl rand -base64 32")
            .ValidateOnStart();

        // Caching — SizeLimit caps entries (not bytes). When SizeLimit is set,
        // every cache.Set() call MUST specify Size in MemoryCacheEntryOptions
        // (otherwise the entry is silently rejected). MemoryBriefCacheService
        // sets Size = 1 on each entry. 256 entries covers ~30 users *
        // (1 daily + 4 weekly) with headroom.
        services.AddMemoryCache(options => options.SizeLimit = 256);
        services.AddSingleton<IBriefCacheService, MemoryBriefCacheService>();

        // Infrastructure services
        services.AddHttpContextAccessor();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MentalMetalDbContext>());
        services.AddScoped<CurrentUserService>();
        services.AddScoped<ICurrentUserService>(sp => sp.GetRequiredService<CurrentUserService>());
        services.AddScoped<IBackgroundUserScope>(sp => sp.GetRequiredService<CurrentUserService>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IInitiativeRepository, InitiativeRepository>();
        services.AddScoped<ICaptureRepository, CaptureRepository>();
        services.AddScoped<ICommitmentRepository, CommitmentRepository>();
        services.AddScoped<IPersonalAccessTokenRepository, PersonalAccessTokenRepository>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        // AI provider services
        services.AddHttpClient("GoogleAi");
        services.AddHttpClient("Deepgram");
        services.AddSingleton<IApiKeyEncryptionService, AesApiKeyEncryptionService>();
        services.AddSingleton<AiModelCatalog>();
        services.AddSingleton<IAiModelCatalog>(sp => sp.GetRequiredService<AiModelCatalog>());
        services.AddSingleton<AnthropicAdapter>();
        services.AddSingleton<OpenAiAdapter>();
        services.AddSingleton<GoogleAdapter>();
        services.AddScoped<IAiCompletionService, AiCompletionService>();
        services.AddScoped<IAiProviderValidator, AiProviderValidator>();
        services.AddScoped<ITasteBudgetService, TasteBudgetService>();

        // Transcription provider services
        services.AddOptions<DeepgramSettings>()
            .Bind(configuration.GetSection(DeepgramSettings.SectionName));
        services.AddScoped<ITranscriptionProviderFactory, TranscriptionProviderFactory>();
        services.AddScoped<ITranscriptionProviderValidator, DeepgramTranscriptionProviderValidator>();

        // Application handlers
        services.AddScoped<RegisterOrLoginUserHandler>();
        services.AddScoped<RegisterWithPasswordHandler>();
        services.AddScoped<LoginWithPasswordHandler>();
        services.AddScoped<SetPasswordHandler>();
        services.AddScoped<GetCurrentUserHandler>();
        services.AddScoped<UpdateUserProfileHandler>();
        services.AddScoped<UpdateUserPreferencesHandler>();
        services.AddScoped<RefreshAccessTokenHandler>();
        services.AddScoped<LogoutUserHandler>();
        services.AddScoped<ConfigureAiProviderHandler>();
        services.AddScoped<GetAiProviderStatusHandler>();
        services.AddScoped<ValidateAiProviderHandler>();
        services.AddScoped<RemoveAiProviderHandler>();
        services.AddScoped<ConfigureTranscriptionProviderHandler>();
        services.AddScoped<GetTranscriptionProviderStatusHandler>();
        services.AddScoped<RemoveTranscriptionProviderHandler>();
        services.AddScoped<ValidateTranscriptionProviderHandler>();
        services.AddSingleton<GetAvailableModelsHandler>();

        // Person handlers
        services.AddScoped<CreatePersonHandler>();
        services.AddScoped<GetPersonHandler>();
        services.AddScoped<GetPeopleHandler>();
        services.AddScoped<UpdatePersonProfileHandler>();
        services.AddScoped<ChangePersonTypeHandler>();
        services.AddScoped<SetAliasesHandler>();
        services.AddScoped<AddAliasHandler>();
        services.AddScoped<ArchivePersonHandler>();

        // Initiative handlers
        services.AddScoped<CreateInitiativeHandler>();
        services.AddScoped<GetInitiativeHandler>();
        services.AddScoped<GetInitiativesHandler>();
        services.AddScoped<UpdateInitiativeTitleHandler>();
        services.AddScoped<ChangeInitiativeStatusHandler>();
        services.AddScoped<RefreshSummaryHandler>();

        // Commitment handlers
        services.AddScoped<GetCommitmentByIdHandler>();
        services.AddScoped<GetUserCommitmentsHandler>();
        services.AddScoped<UpdateCommitmentHandler>();
        services.AddScoped<CompleteCommitmentHandler>();
        services.AddScoped<DismissCommitmentHandler>();
        services.AddScoped<ReopenCommitmentHandler>();

        // Capture handlers
        services.AddScoped<CreateCaptureHandler>();
        services.AddScoped<GetCaptureByIdHandler>();
        services.AddScoped<GetUserCapturesHandler>();
        services.AddScoped<UpdateCaptureMetadataHandler>();
        services.AddScoped<RetryProcessingHandler>();

        // Audio capture — transcription providers are resolved per-request via
        // ITranscriptionProviderFactory using the user's BYOK config.
        services.AddSingleton<IAudioBlobStore, FileSystemAudioBlobStore>();
        services.AddScoped<UploadAudioCaptureHandler>();
        services.AddScoped<TranscribeCaptureHandler>();
        services.AddScoped<GetCaptureTranscriptHandler>();
        services.AddScoped<UpdateCaptureSpeakersHandler>();

        // Personal Access Token handlers
        services.AddSingleton<IPatTokenHasher, PatTokenHasher>();
        services.AddScoped<CreatePersonalAccessTokenHandler>();
        services.AddScoped<ListPersonalAccessTokensHandler>();
        services.AddScoped<RevokePersonalAccessTokenHandler>();
        services.AddScoped<ResolvePatBearerService>();

        // Dossier handler
        services.AddScoped<GetPersonDossierHandler>();

        // Briefing handlers
        services.AddScoped<GenerateDailyBriefHandler>();
        services.AddScoped<GenerateWeeklyBriefHandler>();

        // Auto-extraction pipeline
        services.AddSingleton<BackgroundExtractionTrigger>();
        services.AddScoped<AutoExtractCaptureHandler>();
        services.AddScoped<NameResolutionService>();
        services.AddScoped<InitiativeTaggingService>();
        services.AddScoped<ResolvePersonMentionHandler>();
        services.AddScoped<QuickCreateAndResolveHandler>();
        services.AddScoped<ResolveInitiativeTagHandler>();

        // Capture import handlers and parsers
        services.AddSingleton<ITranscriptFileParser, PlainTextTranscriptParser>();
        services.AddSingleton<ITranscriptFileParser, HtmlTranscriptParser>();
        services.AddSingleton<ITranscriptFileParser, DocxTranscriptParser>();
        services.AddScoped<ImportCaptureFromJsonHandler>();
        services.AddScoped<ImportCaptureFromFileHandler>();

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
