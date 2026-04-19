using MentalMetal.Application.Common;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Commitments;

public sealed class UpdateCommitmentHandler(
    ICommitmentRepository commitmentRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<CommitmentResponse> HandleAsync(
        Guid commitmentId, UpdateCommitmentRequest request, CancellationToken cancellationToken)
    {
        var commitment = await commitmentRepository.GetByIdAsync(commitmentId, cancellationToken)
            ?? throw new InvalidOperationException("Commitment not found.");

        if (commitment.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Commitment not found.");

        if (request.Description is not null)
            commitment.UpdateDescription(request.Description);

        if (request.Direction is { } direction)
            commitment.UpdateDirection(direction);

        if (request.ClearDueDate)
            commitment.UpdateDueDate(null);
        else if (request.DueDate is { } dueDate)
            commitment.UpdateDueDate(dueDate);

        if (request.ClearNotes)
            commitment.UpdateNotes(null);
        else if (request.Notes is not null)
            commitment.UpdateNotes(request.Notes);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CommitmentResponse.From(commitment);
    }
}
