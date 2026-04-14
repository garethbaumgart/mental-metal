using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Auth;
using MentalMetal.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace MentalMetal.Application.Users;

public sealed record LoginWithPasswordCommand(string Email, string Password);

public sealed record LoginWithPasswordResult(
    string AccessToken,
    string RefreshToken,
    UserProfileResponse User);

/// <summary>
/// Thrown when email/password credentials are invalid. The caller SHOULD translate
/// this to HTTP 401 without disclosing which aspect failed.
/// </summary>
public sealed class InvalidCredentialsException() : Exception("Invalid credentials.");

public sealed class LoginWithPasswordHandler(
    IUserRepository userRepository,
    ITokenService tokenService,
    IPasswordHasher<User> passwordHasher,
    IUnitOfWork unitOfWork)
{
    public async Task<LoginWithPasswordResult> HandleAsync(
        LoginWithPasswordCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
            throw new InvalidCredentialsException();

        Email email;
        try
        {
            email = Email.Create(command.Email);
        }
        catch (ArgumentException)
        {
            throw new InvalidCredentialsException();
        }

        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null || user.PasswordHash is null)
            throw new InvalidCredentialsException();

        if (!user.VerifyPassword(command.Password, passwordHasher))
            throw new InvalidCredentialsException();

        user.RecordLogin();
        var tokens = tokenService.GenerateTokens(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginWithPasswordResult(
            tokens.AccessToken,
            tokens.RefreshToken,
            UserProfileResponse.FromDomain(user));
    }
}
