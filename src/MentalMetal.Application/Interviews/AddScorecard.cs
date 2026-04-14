using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Interviews;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Interviews;

public sealed class AddInterviewScorecardHandler(
    IInterviewRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<InterviewScorecardResponse> HandleAsync(
        Guid interviewId, UpsertScorecardRequest request, CancellationToken cancellationToken)
    {
        var interview = await repository.GetByIdAsync(interviewId, cancellationToken);
        if (interview is null || interview.UserId != currentUserService.UserId)
            throw new NotFoundException("Interview", interviewId);

        var scorecard = interview.AddScorecard(
            request.Competency, request.Rating, request.Notes, timeProvider.GetUtcNow());
        repository.MarkOwnedAdded(scorecard);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return InterviewScorecardResponse.From(scorecard);
    }
}
