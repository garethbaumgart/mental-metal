using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Common.Auth;

public record TokenResult(string AccessToken, string RefreshToken);

public interface ITokenService
{
    TokenResult GenerateTokens(User user);
    Task<TokenResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken);
}
