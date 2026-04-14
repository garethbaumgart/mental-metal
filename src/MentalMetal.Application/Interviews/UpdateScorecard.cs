using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Interviews;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Interviews;

public sealed class UpdateInterviewScorecardHandler(
    IInterviewRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<InterviewScorecardResponse> HandleAsync(
        Guid interviewId, Guid scorecardId, UpsertScorecardRequest request, CancellationToken cancellationToken)
    {
        var interview = await repository.GetByIdAsync(interviewId, cancellationToken);
        if (interview is null || interview.UserId != currentUserService.UserId)
            throw new NotFoundException("Interview", interviewId);

        if (!interview.Scorecards.Any(s => s.Id == scorecardId))
            throw new ScorecardNotFoundException($"Scorecard '{scorecardId}' not found on interview '{interviewId}'.");

        interview.UpdateScorecard(
            scorecardId, request.Competency, request.Rating, request.Notes, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = interview.Scorecards.First(s => s.Id == scorecardId);
        return InterviewScorecardResponse.From(updated);
    }
}
