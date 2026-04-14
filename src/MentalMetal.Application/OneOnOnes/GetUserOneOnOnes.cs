using MentalMetal.Domain.OneOnOnes;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.OneOnOnes;

public sealed class GetUserOneOnOnesHandler(
    IOneOnOneRepository repository,
    ICurrentUserService currentUserService)
{
    public async Task<List<OneOnOneResponse>> HandleAsync(
        Guid? personIdFilter, CancellationToken cancellationToken)
    {
        var items = await repository.GetAllAsync(
            currentUserService.UserId,
            personIdFilter,
            cancellationToken);

        return items.Select(OneOnOneResponse.From).ToList();
    }
}
