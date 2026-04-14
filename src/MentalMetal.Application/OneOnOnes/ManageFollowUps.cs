using MentalMetal.Application.Common;
using MentalMetal.Domain.OneOnOnes;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.OneOnOnes;

public sealed class AddFollowUpHandler(
    IOneOnOneRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<OneOnOneResponse> HandleAsync(
        Guid oneOnOneId, AddFollowUpRequest request, CancellationToken cancellationToken)
    {
        var oneOnOne = await repository.GetByIdAsync(oneOnOneId, cancellationToken)
            ?? throw new InvalidOperationException("OneOnOne not found.");

        if (oneOnOne.UserId != currentUserService.UserId)
            throw new InvalidOperationException("OneOnOne not found.");

        var fu = oneOnOne.AddFollowUp(request.Description);
        repository.MarkOwnedAdded(fu);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OneOnOneResponse.From(oneOnOne);
    }
}

public sealed class ResolveFollowUpHandler(
    IOneOnOneRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<OneOnOneResponse> HandleAsync(
        Guid oneOnOneId, Guid followUpId, CancellationToken cancellationToken)
    {
        var oneOnOne = await repository.GetByIdAsync(oneOnOneId, cancellationToken)
            ?? throw new InvalidOperationException("OneOnOne not found.");

        if (oneOnOne.UserId != currentUserService.UserId)
            throw new InvalidOperationException("OneOnOne not found.");

        oneOnOne.ResolveFollowUp(followUpId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OneOnOneResponse.From(oneOnOne);
    }
}
