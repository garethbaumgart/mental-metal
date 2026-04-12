using System.Security.Claims;
using System.Text;
using MentalMetal.Application.Users;
using MentalMetal.Infrastructure;
using MentalMetal.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

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
});

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

// Return 404 for unmatched /api requests instead of serving the SPA shell.
app.MapFallback("/api/{**catch-all}", () => Results.NotFound());

app.MapFallbackToFile("index.html");

app.Run();
