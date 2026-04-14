using MentalMetal.Application.Common;
using MentalMetal.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace MentalMetal.Application.Users;

public sealed record SetPasswordCommand(string NewPassword);

public sealed class SetPasswordHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IPasswordHasher<User> passwordHasher,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(SetPasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.NewPassword, nameof(command.NewPassword));

        if (command.NewPassword.Length < Password.MinimumLength)
            throw new ArgumentException(
                $"Password must be at least {Password.MinimumLength} characters.",
                nameof(command.NewPassword));

        var user = await userRepository.GetByIdAsync(currentUserService.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user not found.");

        user.SetPassword(command.NewPassword, passwordHasher);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
