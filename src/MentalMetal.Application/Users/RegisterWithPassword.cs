using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Auth;
using MentalMetal.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace MentalMetal.Application.Users;

public sealed record RegisterWithPasswordCommand(
    string Email,
    string Password,
    string Name);

public sealed record RegisterWithPasswordResult(
    string AccessToken,
    string RefreshToken,
    UserProfileResponse User);

public sealed class EmailAlreadyInUseException(string email)
    : InvalidOperationException($"A user with email '{email}' already exists.");

public sealed class RegisterWithPasswordHandler(
    IUserRepository userRepository,
    ITokenService tokenService,
    IPasswordHasher<User> passwordHasher,
    IUnitOfWork unitOfWork)
{
    public async Task<RegisterWithPasswordResult> HandleAsync(
        RegisterWithPasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Email, nameof(command.Email));
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name, nameof(command.Name));
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Password, nameof(command.Password));

        if (command.Password.Length < Domain.Users.Password.MinimumLength)
            throw new ArgumentException(
                $"Password must be at least {Domain.Users.Password.MinimumLength} characters.",
                nameof(command.Password));

        // Email.Create validates format
        var email = Email.Create(command.Email);

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new EmailAlreadyInUseException(email.Value);

        var password = Domain.Users.Password.Create(command.Password, passwordHasher);
        var user = User.RegisterWithPassword(email.Value, command.Name, password, null);

        await userRepository.AddAsync(user, cancellationToken);

        var tokens = tokenService.GenerateTokens(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterWithPasswordResult(
            tokens.AccessToken,
            tokens.RefreshToken,
            UserProfileResponse.FromDomain(user));
    }
}
