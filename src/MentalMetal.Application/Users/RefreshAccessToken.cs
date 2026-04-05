using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Auth;

namespace MentalMetal.Application.Users;

public sealed class RefreshAccessTokenHandler(
    ITokenService tokenService,
    IUnitOfWork unitOfWork)
{
    public async Task<AuthTokenResponse?> HandleAsync(
        string refreshToken, CancellationToken cancellationToken)
    {
        var result = await tokenService.RefreshAsync(refreshToken, cancellationToken);

        if (result is null)
            return null;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokenResponse(result.AccessToken);
    }
}
