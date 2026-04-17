using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.PersonalAccessTokens;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.PersonalAccessTokens;

public sealed class RevokePersonalAccessTokenHandler(
    IPersonalAccessTokenRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        var token = await repository.GetByIdAsync(tokenId, cancellationToken);
        if (token is null || token.UserId != currentUserService.UserId)
            throw new NotFoundException($"Personal access token '{tokenId}' not found.");

        token.Revoke();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
