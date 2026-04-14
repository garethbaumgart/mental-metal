using MentalMetal.Application.Common;
using MentalMetal.Domain.OneOnOnes;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.OneOnOnes;

public sealed class AddActionItemHandler(
    IOneOnOneRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<OneOnOneResponse> HandleAsync(
        Guid oneOnOneId, AddActionItemRequest request, CancellationToken cancellationToken)
    {
        var oneOnOne = await repository.GetByIdAsync(oneOnOneId, cancellationToken)
            ?? throw new InvalidOperationException("OneOnOne not found.");

        if (oneOnOne.UserId != currentUserService.UserId)
            throw new InvalidOperationException("OneOnOne not found.");

        var item = oneOnOne.AddActionItem(request.Description);
        repository.MarkOwnedAdded(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OneOnOneResponse.From(oneOnOne);
    }
}

public sealed class CompleteActionItemHandler(
    IOneOnOneRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<OneOnOneResponse> HandleAsync(
        Guid oneOnOneId, Guid actionItemId, CancellationToken cancellationToken)
    {
        var oneOnOne = await repository.GetByIdAsync(oneOnOneId, cancellationToken)
            ?? throw new InvalidOperationException("OneOnOne not found.");

        if (oneOnOne.UserId != currentUserService.UserId)
            throw new InvalidOperationException("OneOnOne not found.");

        oneOnOne.CompleteActionItem(actionItemId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OneOnOneResponse.From(oneOnOne);
    }
}

public sealed class RemoveActionItemHandler(
    IOneOnOneRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<OneOnOneResponse> HandleAsync(
        Guid oneOnOneId, Guid actionItemId, CancellationToken cancellationToken)
    {
        var oneOnOne = await repository.GetByIdAsync(oneOnOneId, cancellationToken)
            ?? throw new InvalidOperationException("OneOnOne not found.");

        if (oneOnOne.UserId != currentUserService.UserId)
            throw new InvalidOperationException("OneOnOne not found.");

        // Capture the item BEFORE mutation so we can mark it Deleted after removal from list.
        var item = oneOnOne.ActionItems.FirstOrDefault(a => a.Id == actionItemId);
        oneOnOne.RemoveActionItem(actionItemId);
        if (item is not null)
            repository.MarkOwnedRemoved(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OneOnOneResponse.From(oneOnOne);
    }
}
