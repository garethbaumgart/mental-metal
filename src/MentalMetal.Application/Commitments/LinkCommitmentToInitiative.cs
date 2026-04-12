using MentalMetal.Application.Common;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Commitments;

public sealed class LinkCommitmentToInitiativeHandler(
    ICommitmentRepository commitmentRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<CommitmentResponse> HandleAsync(
        Guid commitmentId, LinkCommitmentToInitiativeRequest request, CancellationToken cancellationToken)
    {
        var commitment = await commitmentRepository.GetByIdAsync(commitmentId, cancellationToken)
            ?? throw new InvalidOperationException("Commitment not found.");

        if (commitment.UserId != currentUserService.UserId)
            throw new InvalidOperationException("Commitment not found.");

        commitment.LinkToInitiative(request.InitiativeId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CommitmentResponse.From(commitment);
    }
}
