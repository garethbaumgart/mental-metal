using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.Interviews;

/// <summary>
/// Owned value object on <see cref="Interview"/>. Holds the pasted raw transcript plus
/// any AI-generated analysis (summary, recommended decision, risk signals). Treating it
/// as a single cohesive VO ensures that clearing / replacing the transcript also clears
/// the stale analysis in one atomic step.
/// </summary>
public sealed class InterviewTranscript : ValueObject
{
    private readonly List<string> _riskSignals = [];

    public string RawText { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public InterviewDecision? RecommendedDecision { get; private set; }
    public IReadOnlyList<string> RiskSignals => _riskSignals;
    public DateTimeOffset? AnalyzedAtUtc { get; private set; }
    public string? Model { get; private set; }

    private InterviewTranscript() { } // EF Core

    public static InterviewTranscript Create(string rawText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawText);
        return new InterviewTranscript { RawText = rawText };
    }

    internal void WithAnalysis(
        string summary,
        InterviewDecision? recommendedDecision,
        IEnumerable<string> riskSignals,
        string model,
        DateTimeOffset analyzedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        Summary = summary.Trim();
        RecommendedDecision = recommendedDecision;
        _riskSignals.Clear();
        foreach (var s in riskSignals)
        {
            if (!string.IsNullOrWhiteSpace(s))
                _riskSignals.Add(s.Trim());
        }
        Model = model.Trim();
        AnalyzedAtUtc = analyzedAtUtc;
    }

    internal void ClearAnalysis()
    {
        Summary = null;
        RecommendedDecision = null;
        _riskSignals.Clear();
        AnalyzedAtUtc = null;
        Model = null;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawText;
        yield return Summary;
        yield return RecommendedDecision;
        yield return Model;
        yield return AnalyzedAtUtc;
        foreach (var signal in _riskSignals)
            yield return signal;
    }
}
