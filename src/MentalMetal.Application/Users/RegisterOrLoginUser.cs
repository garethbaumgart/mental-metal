using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Auth;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Users;

public sealed record RegisterOrLoginCommand(
    string ExternalAuthId,
    string Email,
    string Name,
    string? AvatarUrl);

public sealed record RegisterOrLoginResult(
    string AccessToken,
    string RefreshToken,
    bool IsNewUser);

public sealed class RegisterOrLoginUserHandler(
    IUserRepository userRepository,
    ITokenService tokenService,
    IUnitOfWork unitOfWork)
{
    public async Task<RegisterOrLoginResult> HandleAsync(
        RegisterOrLoginCommand command, CancellationToken cancellationToken)
    {
        var existingUser = await userRepository.GetByExternalAuthIdAsync(
            command.ExternalAuthId, cancellationToken);

        if (existingUser is not null)
        {
            existingUser.RecordLogin();
            var tokens = tokenService.GenerateTokens(existingUser);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new RegisterOrLoginResult(tokens.AccessToken, tokens.RefreshToken, false);
        }

        // Check for email collision with a different auth provider
        if (await userRepository.ExistsByEmailAsync(command.Email, cancellationToken))
            throw new InvalidOperationException(
                $"A user with email '{command.Email}' already exists.");

        var user = User.Register(
            command.ExternalAuthId,
            command.Email,
            command.Name,
            command.AvatarUrl);

        await userRepository.AddAsync(user, cancellationToken);

        var newTokens = tokenService.GenerateTokens(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterOrLoginResult(newTokens.AccessToken, newTokens.RefreshToken, true);
    }
}
