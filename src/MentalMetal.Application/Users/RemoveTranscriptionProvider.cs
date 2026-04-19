using MentalMetal.Application.Common;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Users;

public sealed class RemoveTranscriptionProviderHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            currentUserService.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user not found.");

        user.RemoveTranscriptionProvider();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
