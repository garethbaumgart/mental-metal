using MentalMetal.Domain.OneOnOnes;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.OneOnOnes;

public sealed class GetOneOnOneByIdHandler(
    IOneOnOneRepository repository,
    ICurrentUserService currentUserService)
{
    public async Task<OneOnOneResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var oneOnOne = await repository.GetByIdAsync(id, cancellationToken);
        if (oneOnOne is null || oneOnOne.UserId != currentUserService.UserId)
            return null;

        return OneOnOneResponse.From(oneOnOne);
    }
}
