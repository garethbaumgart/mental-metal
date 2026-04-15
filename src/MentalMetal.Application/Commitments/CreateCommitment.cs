using MentalMetal.Application.Common;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Commitments;

public sealed class CreateCommitmentHandler(
    ICommitmentRepository commitmentRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task<CommitmentResponse> HandleAsync(
        CreateCommitmentRequest request, CancellationToken cancellationToken)
    {
        if (request.Direction is null)
            throw new ArgumentException("Direction is required.", nameof(request));

        var commitment = Commitment.Create(
            currentUserService.UserId,
            request.Description,
            request.Direction.Value,
            request.PersonId,
            request.DueDate,
            request.InitiativeId,
            request.SourceCaptureId);

        if (!string.IsNullOrWhiteSpace(request.Notes))
            commitment.UpdateNotes(request.Notes);

        await commitmentRepository.AddAsync(commitment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CommitmentResponse.From(commitment);
    }
}
