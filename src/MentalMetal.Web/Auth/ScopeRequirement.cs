using Microsoft.AspNetCore.Authorization;

namespace MentalMetal.Web.Auth;

public sealed class ScopeRequirement(string scope) : IAuthorizationRequirement
{
    public string Scope { get; } = scope;
}

public sealed class ScopeRequirementHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        // If the user authenticated via a non-PAT scheme (e.g., JWT cookie),
        // they have all scopes implicitly — they're a full session user.
        if (context.User.Identity?.AuthenticationType != PatAuthenticationHandler.SchemeName)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // PAT-authenticated users must have the required scope claim.
        if (context.User.HasClaim("scope", requirement.Scope))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
