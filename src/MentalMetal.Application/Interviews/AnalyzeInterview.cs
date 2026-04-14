using MentalMetal.Application.Common;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Interviews;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Interviews;

public sealed class AnalyzeInterviewHandler(
    IInterviewRepository repository,
    ICurrentUserService currentUserService,
    IInterviewAnalysisService analysisService,
    IUnitOfWork unitOfWork)
{
    public async Task<InterviewAnalysisResponse> HandleAsync(Guid interviewId, CancellationToken cancellationToken)
    {
        var interview = await repository.GetByIdAsync(interviewId, cancellationToken);
        if (interview is null || interview.UserId != currentUserService.UserId)
            throw new NotFoundException("Interview", interviewId);

        if (interview.Transcript is null || string.IsNullOrWhiteSpace(interview.Transcript.RawText))
            throw new DomainException("Interview has no transcript to analyze.", Interview.TranscriptMissingCode);

        var result = await analysisService.AnalyzeAsync(interview, cancellationToken);

        interview.ApplyAnalysis(
            result.Summary,
            result.RecommendedDecision,
            result.RiskSignals,
            result.Model,
            result.AnalyzedAtUtc);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new InterviewAnalysisResponse(
            result.Summary,
            result.RecommendedDecision,
            result.RiskSignals,
            result.Model,
            result.AnalyzedAtUtc,
            result.Warning);
    }
}
