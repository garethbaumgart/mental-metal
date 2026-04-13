using System.Security.Claims;
using MentalMetal.Domain.Users;
using Microsoft.AspNetCore.Http;

namespace MentalMetal.Infrastructure.Auth;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService, IBackgroundUserScope
{
    private Guid? _backgroundUserId;

    public Guid UserId
    {
        get
        {
            if (_backgroundUserId is { } overridden) return overridden;

            var claim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("User is not authenticated.");

            return Guid.Parse(claim.Value);
        }
    }

    public bool IsAuthenticated =>
        _backgroundUserId.HasValue ||
        (httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false);

    public void SetUserId(Guid userId)
    {
        // Tenant-isolation safety: the ambient user id is set once per background scope
        // and must never change mid-scope. Allow idempotent re-sets with the same id
        // (e.g. if a framework or test re-invokes during setup) but refuse to rebind
        // the scope to a different tenant.
        if (_backgroundUserId is { } existing)
        {
            if (existing != userId)
                throw new InvalidOperationException(
                    "Background user scope is already bound to a different user and cannot be rebound.");
            return;
        }
        _backgroundUserId = userId;
    }
}
