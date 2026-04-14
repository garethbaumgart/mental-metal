using MentalMetal.Application.Briefings;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Application.Interviews;
using MentalMetal.Application.Tests.Briefings;
using MentalMetal.Domain.Interviews;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MentalMetal.Application.Tests.Interviews;

public class InterviewAnalysisServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _candidateId = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 4, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly IAiCompletionService _ai = Substitute.For<IAiCompletionService>();
    private readonly InterviewAnalysisOptions _options = new();
    private readonly FakeTimeProvider _time;
    private readonly InterviewAnalysisService _service;

    public InterviewAnalysisServiceTests()
    {
        _time = new FakeTimeProvider(_now);
        _service = new InterviewAnalysisService(_ai, Options.Create(_options), _time);
    }

    private Interview MakeInterview(string transcript = "Sample transcript")
    {
        var interview = Interview.Create(_userId, _candidateId, "Staff Engineer", _now);
        interview.AddScorecard("System Design", 4, "Strong fundamentals", _now);
        interview.SetTranscript(transcript, _now);
        return interview;
    }

    private static AiCompletionResult Completion(string content, string model = "gpt-test") =>
        new(content, 100, 50, model, AiProvider.OpenAI);

    [Fact]
    public async Task AnalyzeAsync_HappyPath_ReturnsParsedResult()
    {
        var interview = MakeInterview();
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Completion("{\"summary\":\"Solid hire\",\"recommendedDecision\":\"Hire\",\"riskSignals\":[\"thin on data\"]}"));

        var result = await _service.AnalyzeAsync(interview, CancellationToken.None);

        Assert.Equal("Solid hire", result.Summary);
        Assert.Equal(InterviewDecision.Hire, result.RecommendedDecision);
        Assert.Single(result.RiskSignals);
        Assert.Equal("gpt-test", result.Model);
        Assert.Equal(_now, result.AnalyzedAtUtc);
        Assert.Null(result.Warning);
    }

    [Fact]
    public async Task AnalyzeAsync_TranscriptWithBackticks_EscapedInPrompt()
    {
        var interview = MakeInterview("paste with `backtick` content");
        AiCompletionRequest? captured = null;
        _ai.CompleteAsync(Arg.Do<AiCompletionRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(Completion("{\"summary\":\"ok\",\"recommendedDecision\":\"Hire\",\"riskSignals\":[]}"));

        await _service.AnalyzeAsync(interview, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.DoesNotContain("`backtick`", captured!.UserPrompt);
        // The transcript backticks must not reach the prompt verbatim. We don't care about the
        // exact escape sequence (JSON may upper-case / lower-case unicode escapes, and some
        // encoders translate `\u0060` into `\u0060` vs `` ` `` differently) - what matters is
        // that the word "backtick" survives but no backtick character is adjacent to it on
        // either side (leading or trailing).
        var idx = captured.UserPrompt.IndexOf("backtick", StringComparison.Ordinal);
        Assert.True(idx > 0, "transcript text should still be present");
        Assert.NotEqual('`', captured.UserPrompt[idx - 1]);
        var trailingIdx = idx + "backtick".Length;
        Assert.True(trailingIdx < captured.UserPrompt.Length, "transcript should have content after 'backtick'");
        Assert.NotEqual('`', captured.UserPrompt[trailingIdx]);
    }

    [Fact]
    public async Task AnalyzeAsync_RecommendedDecisionLiteralNull_NoWarning()
    {
        var interview = MakeInterview();
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Completion("{\"summary\":\"ok\",\"recommendedDecision\":\"null\",\"riskSignals\":[]}"));

        var result = await _service.AnalyzeAsync(interview, CancellationToken.None);

        Assert.Null(result.RecommendedDecision);
        Assert.Null(result.Warning);
    }

    [Fact]
    public async Task AnalyzeAsync_UsesConfiguredMaxTokensAndTemperature()
    {
        _options.MaxAnalysisTokens = 1234;
        var interview = MakeInterview();
        AiCompletionRequest? captured = null;
        _ai.CompleteAsync(Arg.Do<AiCompletionRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(Completion("{\"summary\":\"ok\",\"recommendedDecision\":\"Hire\",\"riskSignals\":[]}"));

        await _service.AnalyzeAsync(interview, CancellationToken.None);

        Assert.Equal(1234, captured!.MaxTokens);
        Assert.Equal(0.3f, captured.Temperature);
    }

    [Fact]
    public async Task AnalyzeAsync_InvalidDecisionFromModel_StoresNullAndWarns()
    {
        var interview = MakeInterview();
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Completion("{\"summary\":\"ok\",\"recommendedDecision\":\"maybe\",\"riskSignals\":[]}"));

        var result = await _service.AnalyzeAsync(interview, CancellationToken.None);

        Assert.Null(result.RecommendedDecision);
        Assert.NotNull(result.Warning);
        Assert.Contains("maybe", result.Warning);
    }

    [Fact]
    public async Task AnalyzeAsync_ProviderNotConfigured_RethrowsAsAiProviderNotConfigured()
    {
        var interview = MakeInterview();
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("AI provider is not configured for user."));

        await Assert.ThrowsAsync<AiProviderNotConfiguredException>(
            () => _service.AnalyzeAsync(interview, CancellationToken.None));
    }

    [Fact]
    public async Task AnalyzeAsync_ProviderError_WrappedAsAnalysisFailed()
    {
        var interview = MakeInterview();
        _ai.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("boom"));

        await Assert.ThrowsAsync<InterviewAnalysisFailedException>(
            () => _service.AnalyzeAsync(interview, CancellationToken.None));
    }

    [Fact]
    public async Task AnalyzeAsync_FactsContainScorecardsAndTranscript()
    {
        var interview = MakeInterview("paste");
        AiCompletionRequest? captured = null;
        _ai.CompleteAsync(Arg.Do<AiCompletionRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(Completion("{\"summary\":\"ok\",\"recommendedDecision\":\"Hire\",\"riskSignals\":[]}"));

        await _service.AnalyzeAsync(interview, CancellationToken.None);

        Assert.Contains("System Design", captured!.UserPrompt);
        Assert.Contains("\"rating\":4", captured.UserPrompt);
        Assert.Contains("\"rawText\":\"paste\"", captured.UserPrompt);
    }
}
