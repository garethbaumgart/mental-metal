using MentalMetal.Domain.Interviews;

namespace MentalMetal.Application.Interviews;

public sealed record InterviewAnalysisResult(
    string Summary,
    InterviewDecision? RecommendedDecision,
    IReadOnlyList<string> RiskSignals,
    string Model,
    DateTimeOffset AnalyzedAtUtc,
    string? Warning);

public interface IInterviewAnalysisService
{
    Task<InterviewAnalysisResult> AnalyzeAsync(Interview interview, CancellationToken cancellationToken);
}
