using MentalMetal.Application.Captures;
using MentalMetal.Application.DailyCloseOut;
using MentalMetal.Application.Chat.Global;
using MentalMetal.Application.Initiatives.Chat;
using MentalMetal.Application.Commitments;
using MentalMetal.Application.Delegations;
using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Application.Common.Auth;
using MentalMetal.Application.Goals;
using MentalMetal.Application.Initiatives;
using MentalMetal.Application.Initiatives.Brief;
using MentalMetal.Application.MyQueue;
using MentalMetal.Application.Observations;
using MentalMetal.Application.OneOnOnes;
using MentalMetal.Application.People;
using MentalMetal.Application.PeopleLens;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Goals;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.Observations;
using MentalMetal.Domain.OneOnOnes;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using MentalMetal.Infrastructure.Ai;
using MentalMetal.Infrastructure.Auth;
using MentalMetal.Infrastructure.Persistence;
using MentalMetal.Infrastructure.Repositories;
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
        services.Configure<MyQueueOptions>(configuration.GetSection(MyQueueOptions.SectionName));
        services.AddOptions<AiProviderSettings>()
            .Bind(configuration.GetSection(AiProviderSettings.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.EncryptionKey),
                "AiProvider:EncryptionKey is required. Generate with: openssl rand -base64 32")
            .ValidateOnStart();

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
        services.AddScoped<IPendingBriefUpdateRepository, PendingBriefUpdateRepository>();
        services.AddScoped<ICaptureRepository, CaptureRepository>();
        services.AddScoped<ICommitmentRepository, CommitmentRepository>();
        services.AddScoped<IDelegationRepository, DelegationRepository>();
        services.AddScoped<IChatThreadRepository, ChatThreadRepository>();
        services.AddScoped<IOneOnOneRepository, OneOnOneRepository>();
        services.AddScoped<IObservationRepository, ObservationRepository>();
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

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

        // Initiative handlers
        services.AddScoped<CreateInitiativeHandler>();
        services.AddScoped<GetInitiativeHandler>();
        services.AddScoped<GetInitiativesHandler>();
        services.AddScoped<UpdateInitiativeTitleHandler>();
        services.AddScoped<ChangeInitiativeStatusHandler>();
        services.AddScoped<AddMilestoneHandler>();
        services.AddScoped<UpdateMilestoneHandler>();
        services.AddScoped<RemoveMilestoneHandler>();
        services.AddScoped<CompleteMilestoneHandler>();
        services.AddScoped<LinkPersonHandler>();
        services.AddScoped<UnlinkPersonHandler>();

        // Living Brief services and handlers
        services.AddSingleton<BriefRefreshQueue>();
        services.AddScoped<IBriefMaintenanceService, BriefMaintenanceService>();
        services.AddHostedService<BriefRefreshHostedService>();
        services.AddScoped<GetInitiativeBriefHandler>();
        services.AddScoped<UpdateInitiativeBriefSummaryHandler>();
        services.AddScoped<LogInitiativeBriefDecisionHandler>();
        services.AddScoped<RaiseInitiativeBriefRiskHandler>();
        services.AddScoped<ResolveInitiativeBriefRiskHandler>();
        services.AddScoped<SnapshotInitiativeBriefRequirementsHandler>();
        services.AddScoped<SnapshotInitiativeBriefDesignDirectionHandler>();
        services.AddScoped<RefreshInitiativeBriefHandler>();
        services.AddScoped<ListPendingBriefUpdatesHandler>();
        services.AddScoped<GetPendingBriefUpdateHandler>();
        services.AddScoped<ApplyPendingBriefUpdateHandler>();
        services.AddScoped<RejectPendingBriefUpdateHandler>();
        services.AddScoped<EditPendingBriefUpdateHandler>();

        // Commitment handlers
        services.AddScoped<CreateCommitmentHandler>();
        services.AddScoped<GetCommitmentByIdHandler>();
        services.AddScoped<GetUserCommitmentsHandler>();
        services.AddScoped<UpdateCommitmentHandler>();
        services.AddScoped<CompleteCommitmentHandler>();
        services.AddScoped<CancelCommitmentHandler>();
        services.AddScoped<ReopenCommitmentHandler>();
        services.AddScoped<UpdateCommitmentDueDateHandler>();
        services.AddScoped<LinkCommitmentToInitiativeHandler>();

        // Delegation handlers
        services.AddScoped<CreateDelegationHandler>();
        services.AddScoped<GetDelegationByIdHandler>();
        services.AddScoped<GetUserDelegationsHandler>();
        services.AddScoped<UpdateDelegationHandler>();
        services.AddScoped<StartDelegationHandler>();
        services.AddScoped<CompleteDelegationHandler>();
        services.AddScoped<BlockDelegationHandler>();
        services.AddScoped<UnblockDelegationHandler>();
        services.AddScoped<RecordDelegationFollowUpHandler>();
        services.AddScoped<UpdateDelegationDueDateHandler>();
        services.AddScoped<ReprioritizeDelegationHandler>();
        services.AddScoped<ReassignDelegationHandler>();

        // Capture handlers
        services.AddScoped<CreateCaptureHandler>();
        services.AddScoped<GetCaptureByIdHandler>();
        services.AddScoped<GetUserCapturesHandler>();
        services.AddScoped<UpdateCaptureMetadataHandler>();
        services.AddScoped<LinkCaptureToPersonHandler>();
        services.AddScoped<LinkCaptureToInitiativeHandler>();
        services.AddScoped<UnlinkCaptureFromPersonHandler>();
        services.AddScoped<UnlinkCaptureFromInitiativeHandler>();
        services.AddScoped<ProcessCaptureHandler>();
        services.AddScoped<RetryProcessingHandler>();
        services.AddScoped<ConfirmExtractionHandler>();
        services.AddScoped<DiscardExtractionHandler>();

        // Initiative chat services and handlers
        services.AddScoped<IInitiativeChatContextBuilder, InitiativeChatContextBuilder>();
        services.AddScoped<IInitiativeChatCompletionService, InitiativeChatCompletionService>();
        services.AddScoped<StartInitiativeChatThreadHandler>();
        services.AddScoped<ListInitiativeChatThreadsHandler>();
        services.AddScoped<GetInitiativeChatThreadHandler>();
        services.AddScoped<RenameInitiativeChatThreadHandler>();
        services.AddScoped<PostInitiativeChatMessageHandler>();
        services.AddScoped<ArchiveInitiativeChatThreadHandler>();
        services.AddScoped<UnarchiveInitiativeChatThreadHandler>();

        // Global chat services and handlers
        services.AddScoped<RuleIntentClassifier>();
        services.AddScoped<AiIntentClassifier>();
        services.AddScoped<IIntentClassifier, HybridIntentClassifier>();
        services.AddScoped<IGlobalChatContextBuilder, GlobalChatContextBuilder>();
        services.AddScoped<IGlobalChatCompletionService, GlobalChatCompletionService>();
        services.AddScoped<StartGlobalChatThreadHandler>();
        services.AddScoped<ListGlobalChatThreadsHandler>();
        services.AddScoped<GetGlobalChatThreadHandler>();
        services.AddScoped<RenameGlobalChatThreadHandler>();
        services.AddScoped<PostGlobalChatMessageHandler>();
        services.AddScoped<ArchiveGlobalChatThreadHandler>();
        services.AddScoped<UnarchiveGlobalChatThreadHandler>();

        // OneOnOne handlers
        services.AddScoped<CreateOneOnOneHandler>();
        services.AddScoped<UpdateOneOnOneHandler>();
        services.AddScoped<GetOneOnOneByIdHandler>();
        services.AddScoped<GetUserOneOnOnesHandler>();
        services.AddScoped<AddActionItemHandler>();
        services.AddScoped<CompleteActionItemHandler>();
        services.AddScoped<RemoveActionItemHandler>();
        services.AddScoped<AddFollowUpHandler>();
        services.AddScoped<ResolveFollowUpHandler>();

        // Observation handlers
        services.AddScoped<CreateObservationHandler>();
        services.AddScoped<UpdateObservationHandler>();
        services.AddScoped<GetObservationByIdHandler>();
        services.AddScoped<GetUserObservationsHandler>();
        services.AddScoped<DeleteObservationHandler>();

        // Goal handlers
        services.AddScoped<CreateGoalHandler>();
        services.AddScoped<UpdateGoalHandler>();
        services.AddScoped<GetGoalByIdHandler>();
        services.AddScoped<GetUserGoalsHandler>();
        services.AddScoped<AchieveGoalHandler>();
        services.AddScoped<MissGoalHandler>();
        services.AddScoped<DeferGoalHandler>();
        services.AddScoped<ReactivateGoalHandler>();
        services.AddScoped<RecordGoalCheckInHandler>();

        // People Lens handlers
        services.AddScoped<GetPersonEvidenceSummaryHandler>();

        // My Queue handlers
        services.AddSingleton<QueuePrioritizationService>();
        services.AddScoped<GetMyQueueHandler>();

        // Daily close-out handlers
        services.AddScoped<GetCloseOutQueueHandler>();
        services.AddScoped<QuickDiscardCaptureHandler>();
        services.AddScoped<ReassignCaptureHandler>();
        services.AddScoped<CloseOutDayHandler>();
        services.AddScoped<GetCloseOutLogHandler>();

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
