using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using MentalMetal.Application.Briefings;
using MentalMetal.Application.Captures;
using MentalMetal.Application.Captures.AutoExtract;
using MentalMetal.Web;
using MentalMetal.Web.Features.Captures;
using MentalMetal.Web.Features.Transcription;
using MentalMetal.Application.Commitments;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Application.Initiatives;
using MentalMetal.Application.People;
using MentalMetal.Application.People.Dossier;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.People;
using Google.Cloud.Storage.V1;
using MentalMetal.Infrastructure;
using MentalMetal.Infrastructure.Auth;
using MentalMetal.Web.Auth;
using MentalMetal.Web.Features.PersonalAccessTokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOptions<DeepgramSettings>()
    .Bind(builder.Configuration.GetSection(DeepgramSettings.SectionName));

builder.Services.AddOptions<MentalMetal.Web.Features.Captures.AudioUploadOptions>()
    .Bind(builder.Configuration.GetSection(MentalMetal.Web.Features.Captures.AudioUploadOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// IAudioTranscriptionProvider — Development-only stub. Production environments must register
// a real provider (future work); attempting to upload audio without one will fail at request
// time with a clear DI error rather than silently returning fake transcripts.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<
        MentalMetal.Application.Common.Ai.IAudioTranscriptionProvider,
        MentalMetal.Infrastructure.Ai.StubAudioTranscriptionProvider>();
}

// --- DataProtection ---
// In hosted environments (e.g. Cloud Run) the local filesystem is ephemeral, so
// keys must be persisted to durable storage. When DataProtection:BucketName is
// set, store keys in a Google Cloud Storage object so OAuth state cookies issued
// by one container instance can be validated by another (#75 Bug 4). Otherwise
// fall back to the framework default (filesystem under the current user profile),
// which is fine for local development.
var dataProtectionBucket = builder.Configuration["DataProtection:BucketName"];
builder.Services.AddDataProtection()
    .SetApplicationName("MentalMetal");
if (!string.IsNullOrWhiteSpace(dataProtectionBucket))
{
    // Prefix under which each key is stored as its own object (one object per key,
    // to avoid read-modify-write races between Cloud Run instances).
    var dataProtectionObjectPrefix =
        builder.Configuration["DataProtection:ObjectPrefix"] ?? "keys/";

    builder.Services.TryAddSingleton(_ => StorageClient.Create());
    builder.Services.AddSingleton<IXmlRepository>(sp =>
        new GoogleCloudStorageXmlRepository(
            sp.GetRequiredService<StorageClient>(),
            dataProtectionBucket,
            dataProtectionObjectPrefix,
            sp.GetRequiredService<ILogger<GoogleCloudStorageXmlRepository>>()));
    builder.Services.AddOptions<KeyManagementOptions>()
        .Configure<IServiceProvider>((options, sp) =>
        {
            options.XmlRepository = sp.GetRequiredService<IXmlRepository>();
        });
}

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are not configured. Add a 'Jwt' section to appsettings.");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddScheme<PatAuthenticationSchemeOptions, PatAuthenticationHandler>(
        PatAuthenticationHandler.SchemeName, _ => { });

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
if (!string.IsNullOrEmpty(googleClientId))
{
    builder.Services.AddAuthentication()
        .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
            options.CallbackPath = "/api/auth/google-callback";
        });
}

builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ScopeRequirementHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireCapturesWriteScope", policy =>
    {
        policy.AddAuthenticationSchemes(
            JwtBearerDefaults.AuthenticationScheme,
            PatAuthenticationHandler.SchemeName);
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new ScopeRequirement("captures:write"));
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ImportIngestFromGoogle", policy =>
    {
        policy.WithOrigins("https://docs.google.com", "https://calendar.google.com")
            .WithMethods("POST")
            .WithHeaders("Authorization", "Content-Type")
            .DisallowCredentials();
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    var forwardedOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto
    };
    forwardedOptions.KnownNetworks.Clear();
    forwardedOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedOptions);
}

app.UseStaticFiles();
app.UseWebSockets();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (TasteLimitExceededException ex) when (!context.Response.HasStarted)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (AiProviderException ex) when (!context.Response.HasStarted)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("AiErrorMiddleware");
        logger.LogWarning(ex, "AI provider error from {Provider}", ex.Provider);
        context.Response.StatusCode = StatusCodes.Status502BadGateway;
        await context.Response.WriteAsJsonAsync(new { error = "AI provider request failed. Please try again or check your provider configuration." });
    }
});

// --- Test-only Auth Endpoint (Development/E2E only) ---

var enableTestLogin = app.Environment.IsDevelopment()
    && string.Equals(builder.Configuration["Testing:EnableTestLogin"], "true", StringComparison.OrdinalIgnoreCase);

if (enableTestLogin)
{
    app.MapPost("/api/auth/test-login", async (
        HttpContext httpContext,
        RegisterOrLoginUserHandler handler,
        TestLoginRequest body) =>
    {
        var authResult = await handler.HandleAsync(
            new RegisterOrLoginCommand(
                $"test-{body.Email}",
                body.Email,
                body.Name,
                null),
            httpContext.RequestAborted);

        httpContext.Response.Cookies.Append("refresh_token", authResult.RefreshToken, RefreshTokenCookieOptions());

        return Results.Ok(new { authResult.AccessToken });
    });
}

// --- Auth Endpoints ---

CookieOptions RefreshTokenCookieOptions() => new()
{
    HttpOnly = true,
    Secure = !app.Environment.IsDevelopment(),
    SameSite = SameSiteMode.Strict,
    Expires = DateTimeOffset.UtcNow.AddDays(7)
};

app.MapGet("/api/auth/login", (string? returnUrl) =>
{
    // Prevent open redirect — only allow local paths
    var safeReturnUrl = returnUrl is not null && Uri.TryCreate(returnUrl, UriKind.Relative, out _)
        ? returnUrl
        : "/";

    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = $"/api/auth/callback?returnUrl={safeReturnUrl}" },
        [GoogleDefaults.AuthenticationScheme]);
});

app.MapGet("/api/auth/callback", async (
    HttpContext httpContext,
    RegisterOrLoginUserHandler handler,
    string? returnUrl) =>
{
    var result = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (!result.Succeeded)
        return Results.Unauthorized();

    var claims = result.Principal!.Claims.ToList();
    var externalId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("Missing NameIdentifier claim from Google.");
    var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "";
    var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "";
    var avatar = claims.FirstOrDefault(c => c.Type == "urn:google:picture")?.Value;

    var authResult = await handler.HandleAsync(
        new RegisterOrLoginCommand(externalId, email, name, avatar),
        httpContext.RequestAborted);

    httpContext.Response.Cookies.Append("refresh_token", authResult.RefreshToken, RefreshTokenCookieOptions());

    // Sign out of the temporary cookie scheme
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    // Redirect back to the SPA with the access token as a query param
    var redirect = $"{returnUrl ?? "/"}#access_token={authResult.AccessToken}";
    return Results.Redirect(redirect);
});

app.MapPost("/api/auth/refresh", async (
    HttpContext httpContext,
    RefreshAccessTokenHandler handler) =>
{
    var refreshToken = httpContext.Request.Cookies["refresh_token"];
    if (string.IsNullOrEmpty(refreshToken))
        return Results.Unauthorized();

    var result = await handler.HandleAsync(refreshToken, httpContext.RequestAborted);
    if (result is null)
    {
        httpContext.Response.Cookies.Delete("refresh_token");
        return Results.Unauthorized();
    }

    // Rotate the refresh token cookie
    if (result.RefreshToken is not null)
    {
        httpContext.Response.Cookies.Append("refresh_token", result.RefreshToken, RefreshTokenCookieOptions());
    }

    return Results.Ok(new { result.AccessToken });
});

app.MapPost("/api/auth/register", async (
    HttpContext httpContext,
    RegisterWithPasswordRequest body,
    RegisterWithPasswordHandler handler) =>
{
    try
    {
        var result = await handler.HandleAsync(
            new RegisterWithPasswordCommand(body.Email, body.Password, body.Name),
            httpContext.RequestAborted);

        httpContext.Response.Cookies.Append("refresh_token", result.RefreshToken, RefreshTokenCookieOptions());
        return Results.Ok(new { result.AccessToken, User = result.User });
    }
    catch (EmailAlreadyInUseException)
    {
        return Results.Conflict(new { error = "Email already in use." });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/auth/login", async (
    HttpContext httpContext,
    LoginWithPasswordRequest body,
    LoginWithPasswordHandler handler) =>
{
    try
    {
        var result = await handler.HandleAsync(
            new LoginWithPasswordCommand(body.Email, body.Password),
            httpContext.RequestAborted);

        httpContext.Response.Cookies.Append("refresh_token", result.RefreshToken, RefreshTokenCookieOptions());
        return Results.Ok(new { result.AccessToken, User = result.User });
    }
    catch (InvalidCredentialsException)
    {
        return Results.Unauthorized();
    }
});

app.MapPost("/api/auth/password", async (
    SetPasswordRequest body,
    SetPasswordHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        await handler.HandleAsync(new SetPasswordCommand(body.NewPassword), cancellationToken);
        return Results.NoContent();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/auth/logout", async (
    HttpContext httpContext,
    LogoutUserHandler handler) =>
{
    var refreshToken = httpContext.Request.Cookies["refresh_token"];
    if (!string.IsNullOrEmpty(refreshToken))
        await handler.HandleAsync(refreshToken, httpContext.RequestAborted);

    httpContext.Response.Cookies.Delete("refresh_token");
    return Results.Ok();
});

// --- User Endpoints ---

app.MapGet("/api/users/me", async (
    GetCurrentUserHandler handler,
    CancellationToken cancellationToken) =>
{
    var user = await handler.HandleAsync(cancellationToken);
    return Results.Ok(user);
}).RequireAuthorization();

app.MapPut("/api/users/me/profile", async (
    UpdateProfileRequest request,
    UpdateUserProfileHandler handler,
    CancellationToken cancellationToken) =>
{
    await handler.HandleAsync(request, cancellationToken);
    return Results.NoContent();
}).RequireAuthorization();

app.MapPut("/api/users/me/preferences", async (
    UpdatePreferencesRequest request,
    UpdateUserPreferencesHandler handler,
    CancellationToken cancellationToken) =>
{
    await handler.HandleAsync(request, cancellationToken);
    return Results.NoContent();
}).RequireAuthorization();

// --- AI Provider Endpoints ---

app.MapGet("/api/users/me/ai-provider", async (
    GetAiProviderStatusHandler handler,
    CancellationToken cancellationToken) =>
{
    var status = await handler.HandleAsync(cancellationToken);
    return Results.Ok(status);
}).RequireAuthorization();

app.MapPut("/api/users/me/ai-provider", async (
    ConfigureAiProviderRequest request,
    ConfigureAiProviderHandler handler,
    CancellationToken cancellationToken) =>
{
    await handler.HandleAsync(request, cancellationToken);
    return Results.NoContent();
}).RequireAuthorization();

app.MapPost("/api/users/me/ai-provider/validate", async (
    ValidateAiProviderRequest request,
    ValidateAiProviderHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(request, cancellationToken);
    return Results.Ok(result);
}).RequireAuthorization();

app.MapDelete("/api/users/me/ai-provider", async (
    RemoveAiProviderHandler handler,
    CancellationToken cancellationToken) =>
{
    await handler.HandleAsync(cancellationToken);
    return Results.NoContent();
}).RequireAuthorization();

app.MapGet("/api/ai/models", (
    string provider,
    GetAvailableModelsHandler handler) =>
{
    try
    {
        var result = handler.Handle(provider);
        return Results.Ok(result);
    }
    catch (ArgumentException)
    {
        return Results.BadRequest(new { error = $"Unsupported provider: {provider}" });
    }
}).RequireAuthorization();

// --- People Endpoints ---

app.MapPost("/api/people", async (
    CreatePersonRequest request,
    CreatePersonHandler handler,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Name is required.",
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "person.validation",
                ["field"] = "name",
            });
    }

    try
    {
        var response = await handler.HandleAsync(request, cancellationToken);
        return Results.Created($"/api/people/{response.Id}", response);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (ArgumentException)
    {
        // Defensive: domain guard clauses (e.g. name/type invariants) fall here.
        // Return a sanitized 400 rather than leaking internal argument names or
        // stack traces from the exception message.
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid person data.",
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "person.validation",
                ["field"] = "name",
            });
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateException)
    {
        // Defensive: EF constraint / translation failures can leak Npgsql/LINQ
        // details through the default error handler. Fold them into a clean 400.
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid person data.",
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "person.validation",
                ["field"] = "name",
            });
    }
}).RequireAuthorization();

app.MapGet("/api/people", async (
    PersonType? type,
    bool? includeArchived,
    GetPeopleHandler handler,
    CancellationToken cancellationToken) =>
{
    var list = await handler.HandleAsync(type, includeArchived ?? false, cancellationToken);
    return Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/people/{id:guid}", async (
    Guid id,
    GetPersonHandler handler,
    CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(id, cancellationToken);
    return response is not null ? Results.Ok(response) : Results.NotFound();
}).RequireAuthorization();

app.MapPut("/api/people/{id:guid}", async (
    Guid id,
    UpdatePersonRequest request,
    UpdatePersonProfileHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("archived"))
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
    {
        return Results.Conflict(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPut("/api/people/{id:guid}/type", async (
    Guid id,
    ChangeTypeRequest request,
    ChangePersonTypeHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

app.MapPut("/api/people/{id:guid}/aliases", async (
    Guid id,
    SetAliasesRequest request,
    SetAliasesHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("archived"))
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/people/{id:guid}/aliases", async (
    Guid id,
    AddAliasRequest request,
    AddAliasHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("archived"))
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/people/{id:guid}/archive", async (
    Guid id,
    ArchivePersonHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        await handler.HandleAsync(id, cancellationToken);
        return Results.NoContent();
    }
    catch (InvalidOperationException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

// --- People Dossier Endpoints ---

app.MapGet("/api/people/{id:guid}/dossier", async (
    Guid id,
    string? mode,
    int? captureLimit,
    GetPersonDossierHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(
            id,
            mode ?? "default",
            captureLimit ?? 20,
            cancellationToken);
        return Results.Ok(response);
    }
    catch (NotFoundException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

app.MapPost("/api/people/{id:guid}/dossier/refresh", async (
    Guid id,
    string? mode,
    int? captureLimit,
    GetPersonDossierHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(
            id,
            mode ?? "default",
            captureLimit ?? 20,
            cancellationToken);
        return Results.Ok(response);
    }
    catch (NotFoundException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

// --- Briefing Endpoints ---

app.MapGet("/api/briefing/daily", async (
    GenerateDailyBriefHandler handler,
    CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(cancellationToken);
    return Results.Ok(response);
}).RequireAuthorization();

app.MapPost("/api/briefing/daily/refresh", async (
    GenerateDailyBriefHandler handler,
    CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(cancellationToken);
    return Results.Ok(response);
}).RequireAuthorization();

app.MapGet("/api/briefing/weekly", async (
    DateOnly? weekOf,
    GenerateWeeklyBriefHandler handler,
    CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(weekOf, cancellationToken);
    return Results.Ok(response);
}).RequireAuthorization();

app.MapPost("/api/briefing/weekly/refresh", async (
    DateOnly? weekOf,
    GenerateWeeklyBriefHandler handler,
    CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(weekOf, cancellationToken);
    return Results.Ok(response);
}).RequireAuthorization();

// --- Initiative Endpoints ---

app.MapPost("/api/initiatives", async (
    CreateInitiativeRequest request,
    CreateInitiativeHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(request, cancellationToken);
        return Results.Created($"/api/initiatives/{response.Id}", response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapGet("/api/initiatives", async (
    InitiativeStatus? status,
    GetInitiativesHandler handler,
    CancellationToken cancellationToken) =>
{
    var list = await handler.HandleAsync(status, cancellationToken);
    return Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/initiatives/{id:guid}", async (
    Guid id,
    GetInitiativeHandler handler,
    CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(id, cancellationToken);
    return response is not null ? Results.Ok(response) : Results.NotFound();
}).RequireAuthorization();

app.MapPut("/api/initiatives/{id:guid}", async (
    Guid id,
    UpdateTitleRequest request,
    UpdateInitiativeTitleHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);
        return Results.Ok(response);
    }
    catch (NotFoundException)
    {
        return Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPut("/api/initiatives/{id:guid}/status", async (
    Guid id,
    ChangeStatusRequest request,
    ChangeInitiativeStatusHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);
        return Results.Ok(response);
    }
    catch (NotFoundException)
    {
        return Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/initiatives/{id:guid}/refresh-summary", async (
    Guid id,
    RefreshSummaryHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, cancellationToken);
        return Results.Ok(response);
    }
    catch (NotFoundException)
    {
        return Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

// --- Capture Endpoints ---

app.MapPost("/api/captures", async (
    CreateCaptureRequest request,
    CreateCaptureHandler handler,
    AutoExtractCaptureHandler extractHandler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var created = await handler.HandleAsync(request, cancellationToken);

        // Auto-trigger extraction synchronously (best-effort — failures are recorded on the capture)
        var response = await extractHandler.HandleAsync(created.Id, cancellationToken);
        return Results.Created($"/api/captures/{response.Id}", response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapGet("/api/captures", async (
    CaptureType? type,
    ProcessingStatus? status,
    GetUserCapturesHandler handler,
    CancellationToken cancellationToken) =>
{
    var list = await handler.HandleAsync(type, status, cancellationToken);
    return Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/captures/{id:guid}", async (
    Guid id,
    GetCaptureByIdHandler handler,
    CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(id, cancellationToken);
    return response is not null ? Results.Ok(response) : Results.NotFound();
}).RequireAuthorization();

app.MapPut("/api/captures/{id:guid}", async (
    Guid id,
    UpdateCaptureMetadataRequest request,
    UpdateCaptureMetadataHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

app.MapPost("/api/captures/{id:guid}/retry", async (
    Guid id,
    RetryProcessingHandler retryHandler,
    AutoExtractCaptureHandler extractHandler,
    CancellationToken cancellationToken) =>
{
    try
    {
        // Reset to Raw status, then re-run extraction
        await retryHandler.HandleAsync(id, cancellationToken);
        var response = await extractHandler.HandleAsync(id, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot retry"))
    {
        return Results.Conflict(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/captures/{id:guid}/resolve-person-mention", async (
    Guid id,
    ResolvePersonMentionRequest request,
    ResolvePersonMentionHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/captures/{id:guid}/resolve-initiative-tag", async (
    Guid id,
    ResolveInitiativeTagRequest request,
    ResolveInitiativeTagHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

// --- Commitment Endpoints ---

app.MapGet("/api/commitments", async (
    CommitmentDirection? direction,
    CommitmentStatus? status,
    Guid? personId,
    Guid? initiativeId,
    bool? overdue,
    GetUserCommitmentsHandler handler,
    CancellationToken cancellationToken) =>
{
    var list = await handler.HandleAsync(direction, status, personId, initiativeId, overdue, cancellationToken);
    return Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/commitments/{id:guid}", async (
    Guid id,
    GetCommitmentByIdHandler handler,
    CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(id, cancellationToken);
    return response is not null ? Results.Ok(response) : Results.NotFound();
}).RequireAuthorization();

app.MapPost("/api/commitments/{id:guid}/complete", async (
    Guid id,
    CompleteCommitmentRequest request,
    CompleteCommitmentHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot"))
    {
        return Results.Conflict(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/commitments/{id:guid}/dismiss", async (
    Guid id,
    DismissCommitmentHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot"))
    {
        return Results.Conflict(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/commitments/{id:guid}/reopen", async (
    Guid id,
    ReopenCommitmentHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot"))
    {
        return Results.Conflict(new { error = ex.Message });
    }
}).RequireAuthorization();






app.MapAudioCaptureEndpoints();
app.MapImportCaptureEndpoints();
app.MapPersonalAccessTokenEndpoints();
app.MapTranscriptionEndpoints();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

// Return 404 for unmatched /api requests instead of serving the SPA shell.
app.MapFallback("/api/{**catch-all}", () => Results.NotFound());

app.MapFallbackToFile("index.html");

app.Run();

internal sealed record TestLoginRequest(string Email, string Name);

// Expose the top-level-statements Program class to WebApplicationFactory<Program> in integration tests.
public partial class Program { }
