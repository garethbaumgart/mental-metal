namespace MentalMetal.Domain.Users;

public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}

/// <summary>
/// Allows background workers (no HttpContext) to set the ambient user id for the
/// current DI scope, so EF query filters depending on <see cref="ICurrentUserService.UserId"/>
/// resolve correctly.
/// </summary>
public interface IBackgroundUserScope
{
    /// <summary>Set the ambient user id for this DI scope. Must be called before resolving scoped services that read it.</summary>
    void SetUserId(Guid userId);
}
