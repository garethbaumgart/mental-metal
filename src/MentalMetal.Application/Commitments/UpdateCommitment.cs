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

        commitment.UpdateDescription(request.Description);
        commitment.UpdateNotes(request.Notes);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CommitmentResponse.From(commitment);
    }
}
