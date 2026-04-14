using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Interviews;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Options;

namespace MentalMetal.Application.Interviews;

public sealed class SetInterviewTranscriptHandler(
    IInterviewRepository repository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IOptions<InterviewAnalysisOptions> options,
    TimeProvider timeProvider)
{
    public async Task<InterviewResponse> HandleAsync(
        Guid interviewId, SetTranscriptRequest request, CancellationToken cancellationToken)
    {
        var interview = await repository.GetByIdAsync(interviewId, cancellationToken);
        if (interview is null || interview.UserId != currentUserService.UserId)
            throw new NotFoundException("Interview", interviewId);

        if (string.IsNullOrWhiteSpace(request.RawText))
            throw new ArgumentException("Transcript text must not be empty.", nameof(request.RawText));

        var maxChars = options.Value.MaxPromptChars;
        if (request.RawText.Length > maxChars)
            throw new TranscriptTooLongException(
                $"Transcript length {request.RawText.Length} exceeds the configured max of {maxChars} characters.");

        interview.SetTranscript(request.RawText, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return InterviewResponse.From(interview);
    }
}
