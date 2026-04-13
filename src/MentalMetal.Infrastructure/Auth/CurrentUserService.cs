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

    public void SetUserId(Guid userId) => _backgroundUserId = userId;
}
