using MentalMetal.Application.Common;
using MentalMetal.Domain.OneOnOnes;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.OneOnOnes;

public sealed class UpdateOneOnOneHandler(
    IOneOnOneRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<OneOnOneResponse> HandleAsync(
        Guid id, UpdateOneOnOneRequest request, CancellationToken cancellationToken)
    {
        var oneOnOne = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("OneOnOne not found.");

        if (oneOnOne.UserId != currentUserService.UserId)
            throw new InvalidOperationException("OneOnOne not found.");

        oneOnOne.Update(request.Notes, request.Topics, request.MoodRating);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OneOnOneResponse.From(oneOnOne);
    }
}
