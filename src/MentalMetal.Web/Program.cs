using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using MentalMetal.Application.Captures;
using MentalMetal.Application.Commitments;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Application.Delegations;
using MentalMetal.Application.Initiatives;
using MentalMetal.Application.Initiatives.Brief;
using MentalMetal.Application.Chat.Global;
using MentalMetal.Application.Initiatives.Chat;
using MentalMetal.Application.People;
using MentalMetal.Application.Users;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Domain.People;
using MentalMetal.Infrastructure;
using MentalMetal.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddInfrastructure(builder.Configuration);

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
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

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

builder.Services.AddAuthorization();

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
    try
    {
        var response = await handler.HandleAsync(request, cancellationToken);
        return Results.Created($"/api/people/{response.Id}", response);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapGet("/api/people", async (
    PersonType? type,
    bool includeArchived,
    GetPeopleHandler handler,
    CancellationToken cancellationToken) =>
{
    var list = await handler.HandleAsync(type, includeArchived, cancellationToken);
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

app.MapPut("/api/people/{id:guid}/career-details", async (
    Guid id,
    CareerDetailsRequest request,
    UpdateCareerDetailsHandler handler,
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
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPut("/api/people/{id:guid}/candidate-details", async (
    Guid id,
    CandidateDetailsRequest request,
    UpdateCandidateDetailsHandler handler,
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
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/people/{id:guid}/advance-pipeline", async (
    Guid id,
    AdvancePipelineRequest request,
    AdvanceCandidatePipelineHandler handler,
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

app.MapPost("/api/initiatives/{id:guid}/milestones", async (
    Guid id,
    MilestoneRequest request,
    AddMilestoneHandler handler,
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

app.MapPut("/api/initiatives/{id:guid}/milestones/{milestoneId:guid}", async (
    Guid id,
    Guid milestoneId,
    MilestoneRequest request,
    UpdateMilestoneHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, milestoneId, request, cancellationToken);
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

app.MapDelete("/api/initiatives/{id:guid}/milestones/{milestoneId:guid}", async (
    Guid id,
    Guid milestoneId,
    RemoveMilestoneHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, milestoneId, cancellationToken);
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

app.MapPost("/api/initiatives/{id:guid}/milestones/{milestoneId:guid}/complete", async (
    Guid id,
    Guid milestoneId,
    CompleteMilestoneHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, milestoneId, cancellationToken);
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

app.MapPost("/api/initiatives/{id:guid}/link-person", async (
    Guid id,
    MentalMetal.Application.Initiatives.LinkPersonRequest request,
    LinkPersonHandler handler,
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

app.MapDelete("/api/initiatives/{id:guid}/link-person/{personId:guid}", async (
    Guid id,
    Guid personId,
    UnlinkPersonHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, personId, cancellationToken);
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
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(request, cancellationToken);
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

app.MapPost("/api/captures/{id:guid}/link-person", async (
    Guid id,
    MentalMetal.Application.Captures.LinkPersonRequest request,
    LinkCaptureToPersonHandler handler,
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
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/captures/{id:guid}/link-initiative", async (
    Guid id,
    LinkInitiativeRequest request,
    LinkCaptureToInitiativeHandler handler,
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
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/captures/{id:guid}/unlink-person", async (
    Guid id,
    MentalMetal.Application.Captures.LinkPersonRequest request,
    UnlinkCaptureFromPersonHandler handler,
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
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/captures/{id:guid}/unlink-initiative", async (
    Guid id,
    LinkInitiativeRequest request,
    UnlinkCaptureFromInitiativeHandler handler,
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
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/captures/{id:guid}/process", async (
    Guid id,
    ProcessCaptureHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(id, cancellationToken);
        return Results.Accepted(null, response);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot begin processing"))
    {
        return Results.Conflict(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/captures/{id:guid}/retry", async (
    Guid id,
    RetryProcessingHandler handler,
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
    catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot retry"))
    {
        return Results.Conflict(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/captures/{id:guid}/confirm-extraction", async (
    Guid id,
    ConfirmExtractionHandler handler,
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
    catch (InvalidOperationException ex) when (
        ex.Message.Contains("Cannot confirm") ||
        ex.Message.Contains("No extraction to confirm"))
    {
        return Results.Conflict(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/captures/{id:guid}/discard-extraction", async (
    Guid id,
    DiscardExtractionHandler handler,
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
    catch (InvalidOperationException ex) when (
        ex.Message.Contains("Cannot discard") ||
        ex.Message.Contains("Extraction already confirmed"))
    {
        return Results.Conflict(new { error = ex.Message });
    }
}).RequireAuthorization();

// --- Commitment Endpoints ---

app.MapPost("/api/commitments", async (
    CreateCommitmentRequest request,
    CreateCommitmentHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(request, cancellationToken);
        return Results.Created($"/api/commitments/{response.Id}", response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

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

app.MapPut("/api/commitments/{id:guid}", async (
    Guid id,
    UpdateCommitmentRequest request,
    UpdateCommitmentHandler handler,
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
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
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

app.MapPost("/api/commitments/{id:guid}/cancel", async (
    Guid id,
    CancelCommitmentRequest request,
    CancelCommitmentHandler handler,
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

app.MapPut("/api/commitments/{id:guid}/due-date", async (
    Guid id,
    UpdateDueDateRequest request,
    UpdateCommitmentDueDateHandler handler,
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

app.MapPost("/api/commitments/{id:guid}/link-initiative", async (
    Guid id,
    LinkCommitmentToInitiativeRequest request,
    LinkCommitmentToInitiativeHandler handler,
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

// --- Delegation Endpoints ---

app.MapPost("/api/delegations", async (
    CreateDelegationRequest request,
    CreateDelegationHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await handler.HandleAsync(request, cancellationToken);
        return Results.Created($"/api/delegations/{response.Id}", response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapGet("/api/delegations", async (
    DelegationStatus? status,
    Priority? priority,
    Guid? delegatePersonId,
    Guid? initiativeId,
    GetUserDelegationsHandler handler,
    CancellationToken cancellationToken) =>
{
    var list = await handler.HandleAsync(status, priority, delegatePersonId, initiativeId, cancellationToken);
    return Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/delegations/{id:guid}", async (
    Guid id,
    GetDelegationByIdHandler handler,
    CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(id, cancellationToken);
    return response is not null ? Results.Ok(response) : Results.NotFound();
}).RequireAuthorization();

app.MapPut("/api/delegations/{id:guid}", async (
    Guid id,
    UpdateDelegationRequest request,
    UpdateDelegationHandler handler,
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
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/delegations/{id:guid}/start", async (
    Guid id,
    StartDelegationHandler handler,
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

app.MapPost("/api/delegations/{id:guid}/complete", async (
    Guid id,
    CompleteDelegationRequest request,
    CompleteDelegationHandler handler,
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

app.MapPost("/api/delegations/{id:guid}/block", async (
    Guid id,
    BlockDelegationRequest request,
    BlockDelegationHandler handler,
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
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/delegations/{id:guid}/unblock", async (
    Guid id,
    UnblockDelegationHandler handler,
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

app.MapPost("/api/delegations/{id:guid}/follow-up", async (
    Guid id,
    FollowUpDelegationRequest request,
    RecordDelegationFollowUpHandler handler,
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

app.MapPut("/api/delegations/{id:guid}/due-date", async (
    Guid id,
    UpdateDelegationDueDateRequest request,
    UpdateDelegationDueDateHandler handler,
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

app.MapPut("/api/delegations/{id:guid}/priority", async (
    Guid id,
    ReprioritizeDelegationRequest request,
    ReprioritizeDelegationHandler handler,
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

app.MapPost("/api/delegations/{id:guid}/reassign", async (
    Guid id,
    ReassignDelegationRequest request,
    ReassignDelegationHandler handler,
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
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

// ---- Living Brief endpoints --------------------------------------------------

app.MapGet("/api/initiatives/{id:guid}/brief", async (
    Guid id, GetInitiativeBriefHandler handler, CancellationToken ct) =>
{
    var brief = await handler.HandleAsync(id, ct);
    return brief is null ? Results.NotFound() : Results.Ok(brief);
}).RequireAuthorization();

app.MapPut("/api/initiatives/{id:guid}/brief/summary", async (
    Guid id, UpdateSummaryRequest request, UpdateInitiativeBriefSummaryHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(id, request, ct)); }
    catch (NotFoundException) { return Results.NotFound(); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/initiatives/{id:guid}/brief/decisions", async (
    Guid id, LogDecisionRequest request, LogInitiativeBriefDecisionHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(id, request, ct)); }
    catch (NotFoundException) { return Results.NotFound(); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/initiatives/{id:guid}/brief/risks", async (
    Guid id, RaiseRiskRequest request, RaiseInitiativeBriefRiskHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(id, request, ct)); }
    catch (NotFoundException) { return Results.NotFound(); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/initiatives/{id:guid}/brief/risks/{riskId:guid}/resolve", async (
    Guid id, Guid riskId, ResolveRiskRequest? request, ResolveInitiativeBriefRiskHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(id, riskId, request ?? new ResolveRiskRequest(null), ct)); }
    catch (NotFoundException) { return Results.NotFound(); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/initiatives/{id:guid}/brief/requirements", async (
    Guid id, SnapshotRequest request, SnapshotInitiativeBriefRequirementsHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(id, request, ct)); }
    catch (NotFoundException) { return Results.NotFound(); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/initiatives/{id:guid}/brief/design-direction", async (
    Guid id, SnapshotRequest request, SnapshotInitiativeBriefDesignDirectionHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(id, request, ct)); }
    catch (NotFoundException) { return Results.NotFound(); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/initiatives/{id:guid}/brief/refresh", async (
    Guid id, RefreshInitiativeBriefHandler handler, CancellationToken ct) =>
{
    try
    {
        var pendingId = await handler.HandleAsync(id, ct);
        return Results.Accepted($"/api/initiatives/{id}/brief/pending-updates/{pendingId}",
            new { pendingUpdateId = pendingId });
    }
    catch (NotFoundException) { return Results.NotFound(); }
}).RequireAuthorization();

app.MapGet("/api/initiatives/{id:guid}/brief/pending-updates", async (
    Guid id, PendingBriefUpdateStatus? status, ListPendingBriefUpdatesHandler handler, CancellationToken ct) =>
{
    var list = await handler.HandleAsync(id, status, ct);
    return list is null ? Results.NotFound() : Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/initiatives/{id:guid}/brief/pending-updates/{updateId:guid}", async (
    Guid id, Guid updateId, GetPendingBriefUpdateHandler handler, CancellationToken ct) =>
{
    var dto = await handler.HandleAsync(updateId, ct);
    return dto is null || dto.InitiativeId != id ? Results.NotFound() : Results.Ok(dto);
}).RequireAuthorization();

app.MapPost("/api/initiatives/{id:guid}/brief/pending-updates/{updateId:guid}/apply", async (
    Guid id, Guid updateId, ApplyPendingBriefUpdateHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(id, updateId, ct)); }
    catch (NotFoundException) { return Results.NotFound(); }
    catch (ApplyPendingBriefUpdateHandler.StaleProposalException ex)
    {
        return Results.Conflict(new
        {
            error = ex.Message,
            currentBriefVersion = ex.CurrentBriefVersion,
            proposalBriefVersion = ex.ProposalBriefVersion
        });
    }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/initiatives/{id:guid}/brief/pending-updates/{updateId:guid}/reject", async (
    Guid id, Guid updateId, RejectPendingUpdateRequest? request, RejectPendingBriefUpdateHandler handler, CancellationToken ct) =>
{
    try { await handler.HandleAsync(id, updateId, request, ct); return Results.NoContent(); }
    catch (NotFoundException) { return Results.NotFound(); }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPut("/api/initiatives/{id:guid}/brief/pending-updates/{updateId:guid}", async (
    Guid id, Guid updateId, EditPendingUpdateRequest request, EditPendingBriefUpdateHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(id, updateId, request, ct)); }
    catch (NotFoundException) { return Results.NotFound(); }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
}).RequireAuthorization();

// ---- Initiative Chat endpoints ----------------------------------------------

app.MapPost("/api/initiatives/{id:guid}/chat/threads", async (
    Guid id, StartInitiativeChatThreadHandler handler, CancellationToken ct) =>
{
    var thread = await handler.HandleAsync(id, ct);
    return thread is null
        ? Results.NotFound()
        : Results.Created($"/api/initiatives/{id}/chat/threads/{thread.Id}", thread);
}).RequireAuthorization();

app.MapGet("/api/initiatives/{id:guid}/chat/threads", async (
    Guid id, ChatThreadStatus? status, ListInitiativeChatThreadsHandler handler, CancellationToken ct) =>
{
    var list = await handler.HandleAsync(id, status, ct);
    return list is null ? Results.NotFound() : Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/initiatives/{id:guid}/chat/threads/{threadId:guid}", async (
    Guid id, Guid threadId, GetInitiativeChatThreadHandler handler, CancellationToken ct) =>
{
    var thread = await handler.HandleAsync(id, threadId, ct);
    return thread is null ? Results.NotFound() : Results.Ok(thread);
}).RequireAuthorization();

app.MapPut("/api/initiatives/{id:guid}/chat/threads/{threadId:guid}", async (
    Guid id, Guid threadId, RenameChatThreadRequest request, RenameInitiativeChatThreadHandler handler, CancellationToken ct) =>
{
    try
    {
        var thread = await handler.HandleAsync(id, threadId, request, ct);
        return thread is null ? Results.NotFound() : Results.Ok(thread);
    }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/initiatives/{id:guid}/chat/threads/{threadId:guid}/messages", async (
    Guid id, Guid threadId, PostChatMessageRequest request, PostInitiativeChatMessageHandler handler, CancellationToken ct) =>
{
    try
    {
        var response = await handler.HandleAsync(id, threadId, request, ct);
        return response is null ? Results.NotFound() : Results.Ok(response);
    }
    catch (PostInitiativeChatMessageHandler.ArchivedThreadException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/initiatives/{id:guid}/chat/threads/{threadId:guid}/archive", async (
    Guid id, Guid threadId, ArchiveInitiativeChatThreadHandler handler, CancellationToken ct) =>
{
    try
    {
        var thread = await handler.HandleAsync(id, threadId, ct);
        return thread is null ? Results.NotFound() : Results.Ok(thread);
    }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/initiatives/{id:guid}/chat/threads/{threadId:guid}/unarchive", async (
    Guid id, Guid threadId, UnarchiveInitiativeChatThreadHandler handler, CancellationToken ct) =>
{
    try
    {
        var thread = await handler.HandleAsync(id, threadId, ct);
        return thread is null ? Results.NotFound() : Results.Ok(thread);
    }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
}).RequireAuthorization();

// ---- Global Chat endpoints --------------------------------------------------

app.MapPost("/api/chat/threads", async (
    StartGlobalChatThreadHandler handler, CancellationToken ct) =>
{
    var thread = await handler.HandleAsync(ct);
    return Results.Created($"/api/chat/threads/{thread.Id}", thread);
}).RequireAuthorization();

app.MapGet("/api/chat/threads", async (
    ChatThreadStatus? status, ListGlobalChatThreadsHandler handler, CancellationToken ct) =>
{
    var list = await handler.HandleAsync(status, ct);
    return Results.Ok(list);
}).RequireAuthorization();

app.MapGet("/api/chat/threads/{threadId:guid}", async (
    Guid threadId, GetGlobalChatThreadHandler handler, CancellationToken ct) =>
{
    var thread = await handler.HandleAsync(threadId, ct);
    return thread is null ? Results.NotFound() : Results.Ok(thread);
}).RequireAuthorization();

app.MapPut("/api/chat/threads/{threadId:guid}", async (
    Guid threadId, RenameChatThreadRequest request, RenameGlobalChatThreadHandler handler, CancellationToken ct) =>
{
    try
    {
        var thread = await handler.HandleAsync(threadId, request, ct);
        return thread is null ? Results.NotFound() : Results.Ok(thread);
    }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/chat/threads/{threadId:guid}/messages", async (
    Guid threadId, PostChatMessageRequest request, PostGlobalChatMessageHandler handler, CancellationToken ct) =>
{
    try
    {
        var response = await handler.HandleAsync(threadId, request, ct);
        return response is null ? Results.NotFound() : Results.Ok(response);
    }
    catch (PostGlobalChatMessageHandler.ArchivedThreadException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/chat/threads/{threadId:guid}/archive", async (
    Guid threadId, ArchiveGlobalChatThreadHandler handler, CancellationToken ct) =>
{
    try
    {
        var thread = await handler.HandleAsync(threadId, ct);
        return thread is null ? Results.NotFound() : Results.Ok(thread);
    }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapPost("/api/chat/threads/{threadId:guid}/unarchive", async (
    Guid threadId, UnarchiveGlobalChatThreadHandler handler, CancellationToken ct) =>
{
    try
    {
        var thread = await handler.HandleAsync(threadId, ct);
        return thread is null ? Results.NotFound() : Results.Ok(thread);
    }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
}).RequireAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

// Return 404 for unmatched /api requests instead of serving the SPA shell.
app.MapFallback("/api/{**catch-all}", () => Results.NotFound());

app.MapFallbackToFile("index.html");

app.Run();

internal sealed record TestLoginRequest(string Email, string Name);
