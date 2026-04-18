using MentalMetal.Application.Briefings;

namespace MentalMetal.Application.Tests.Briefings;

public class DailyBriefPromptBuilderTests
{
    [Fact]
    public void SystemPrompt_ContainsExpectedContent()
    {
        var prompt = DailyBriefPromptBuilder.SystemPrompt;

        Assert.Contains("daily briefing", prompt);
        Assert.Contains("Yesterday's Summary", prompt);
        Assert.Contains("Key Decisions", prompt);
    }

    [Fact]
    public void BuildUserPrompt_WithCaptures_IncludesSummaries()
    {
        var captures = new[]
        {
            new CaptureContextForBrief(
                "Team Standup",
                DateTimeOffset.UtcNow.AddDays(-1),
                "Discussed sprint progress.",
                new List<string> { "Approved the rollout plan" },
                new List<string> { "Dependency on external API" })
        };

        var prompt = DailyBriefPromptBuilder.BuildUserPrompt(
            captures,
            Array.Empty<CommitmentContextForBrief>(),
            Array.Empty<CommitmentContextForBrief>());

        Assert.Contains("Team Standup", prompt);
        Assert.Contains("Discussed sprint progress", prompt);
        Assert.Contains("Approved the rollout plan", prompt);
        Assert.Contains("Dependency on external API", prompt);
    }

    [Fact]
    public void BuildUserPrompt_EmptyData_ShowsNoneMessages()
    {
        var prompt = DailyBriefPromptBuilder.BuildUserPrompt(
            Array.Empty<CaptureContextForBrief>(),
            Array.Empty<CommitmentContextForBrief>(),
            Array.Empty<CommitmentContextForBrief>());

        Assert.Contains("No captures from yesterday", prompt);
        Assert.Contains("None.", prompt);
    }
}
