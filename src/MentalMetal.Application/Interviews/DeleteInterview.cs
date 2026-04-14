using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Interviews;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Interviews;

public sealed class DeleteInterviewHandler(
    IInterviewRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var interview = await repository.GetByIdAsync(id, cancellationToken);
        if (interview is null || interview.UserId != currentUserService.UserId)
            throw new NotFoundException("Interview", id);

        interview.MarkDeleted();
        repository.Remove(interview);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
