using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Users;

public sealed class GetCurrentUserHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService)
{
    public async Task<UserProfileResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            currentUserService.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user not found.");

        return UserProfileResponse.FromDomain(user);
    }
}
