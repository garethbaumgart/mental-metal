using MentalMetal.Application.Common;
using MentalMetal.Domain.OneOnOnes;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.OneOnOnes;

public sealed class CreateOneOnOneHandler(
    IOneOnOneRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<OneOnOneResponse> HandleAsync(
        CreateOneOnOneRequest request, CancellationToken cancellationToken)
    {
        var oneOnOne = OneOnOne.Create(
            currentUserService.UserId,
            request.PersonId,
            request.OccurredAt,
            request.Notes,
            request.Topics,
            request.MoodRating);

        await repository.AddAsync(oneOnOne, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OneOnOneResponse.From(oneOnOne);
    }
}
