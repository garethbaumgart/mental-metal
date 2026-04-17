using System.Security.Claims;
using System.Text.Encodings.Web;
using MentalMetal.Application.PersonalAccessTokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MentalMetal.Web.Auth;

public sealed class PatAuthenticationSchemeOptions : AuthenticationSchemeOptions;

public sealed class PatAuthenticationHandler(
    IOptionsMonitor<PatAuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IServiceProvider serviceProvider)
    : AuthenticationHandler<PatAuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "PersonalAccessToken";
    private const string Prefix = "mm_pat_";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = authHeader["Bearer ".Length..].Trim();
        if (!token.StartsWith(Prefix, StringComparison.Ordinal))
            return AuthenticateResult.NoResult();

        // Resolve using a new scope so the ICurrentUserService closure is not polluted
        using var scope = serviceProvider.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ResolvePatBearerService>();
        var resolution = await resolver.ResolveAsync(token, Context.RequestAborted);

        if (resolution is null)
            return AuthenticateResult.Fail("Invalid or revoked personal access token.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, resolution.UserId.ToString()),
        };

        foreach (var s in resolution.Scopes)
        {
            claims.Add(new Claim("scope", s));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }
}
