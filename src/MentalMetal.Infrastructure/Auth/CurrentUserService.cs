using System.Security.Claims;
using MentalMetal.Domain.Users;
using Microsoft.AspNetCore.Http;

namespace MentalMetal.Infrastructure.Auth;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("User is not authenticated.");

            return Guid.Parse(claim.Value);
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
