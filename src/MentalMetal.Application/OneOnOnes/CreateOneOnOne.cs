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
        if (request.OccurredAt is null)
            throw new ArgumentException("OccurredAt is required.", nameof(request.OccurredAt));

        var oneOnOne = OneOnOne.Create(
            currentUserService.UserId,
            request.PersonId,
            request.OccurredAt.Value,
            request.Notes,
            request.Topics,
            request.MoodRating);

        await repository.AddAsync(oneOnOne, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OneOnOneResponse.From(oneOnOne);
    }
}
