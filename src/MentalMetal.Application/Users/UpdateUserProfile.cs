using MentalMetal.Application.Common;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Users;

public sealed class UpdateUserProfileHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            currentUserService.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user not found.");

        user.UpdateProfile(request.Name, request.AvatarUrl, request.Timezone);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
