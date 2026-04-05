using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Auth;
using MentalMetal.Application.Users;
using NSubstitute;

namespace MentalMetal.Application.Tests.Users;

public class RefreshAccessTokenTests
{
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RefreshAccessTokenHandler _handler;

    public RefreshAccessTokenTests()
    {
        _handler = new RefreshAccessTokenHandler(_tokenService, _unitOfWork);
    }

    [Fact]
    public async Task ValidToken_ReturnsNewAccessToken()
    {
        _tokenService.RefreshAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(new TokenResult("new-access-token", "new-refresh-token"));

        var result = await _handler.HandleAsync("valid-token", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("new-access-token", result.AccessToken);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExpiredToken_ReturnsNull()
    {
        _tokenService.RefreshAsync("expired-token", Arg.Any<CancellationToken>())
            .Returns((TokenResult?)null);

        var result = await _handler.HandleAsync("expired-token", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ReusedToken_ReturnsNull()
    {
        _tokenService.RefreshAsync("reused-token", Arg.Any<CancellationToken>())
            .Returns((TokenResult?)null);

        var result = await _handler.HandleAsync("reused-token", CancellationToken.None);

        Assert.Null(result);
    }
}
