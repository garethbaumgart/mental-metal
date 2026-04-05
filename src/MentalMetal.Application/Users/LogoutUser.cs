using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Auth;

namespace MentalMetal.Application.Users;

public sealed class LogoutUserHandler(
    ITokenService tokenService,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(string refreshToken, CancellationToken cancellationToken)
    {
        await tokenService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
