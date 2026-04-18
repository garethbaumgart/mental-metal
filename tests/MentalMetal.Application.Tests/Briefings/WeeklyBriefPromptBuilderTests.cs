using MentalMetal.Application.Briefings;

namespace MentalMetal.Application.Tests.Briefings;

public class WeeklyBriefPromptBuilderTests
{
    [Fact]
    public void SystemPrompt_ContainsExpectedContent()
    {
        var prompt = WeeklyBriefPromptBuilder.SystemPrompt;

        Assert.Contains("weekly briefing", prompt);
        Assert.Contains("Cross-Conversation Patterns", prompt);
        Assert.Contains("contradictions", prompt);
    }

    [Fact]
    public void BuildUserPrompt_IncludesCapturesAndCommitmentStats()
    {
        var captures = new[]
        {
            new CaptureContextForBrief(
                "Product Review",
                DateTimeOffset.UtcNow,
                "Reviewed Q3 goals.",
                new List<string> { "Approved roadmap changes" },
                new List<string> { "Resource constraints" })
        };

        var initiatives = new[]
        {
            new InitiativeContextForBrief("Project Alpha", 3, "On track for release.")
        };

        var weekStart = new DateTimeOffset(2026, 4, 13, 0, 0, 0, TimeSpan.Zero);
        var weekEnd = weekStart.AddDays(7);

        var prompt = WeeklyBriefPromptBuilder.BuildUserPrompt(
            captures, 5, 2, 1, initiatives, weekStart, weekEnd);

        Assert.Contains("Product Review", prompt);
        Assert.Contains("Reviewed Q3 goals", prompt);
        Assert.Contains("New this week: 5", prompt);
        Assert.Contains("Completed this week: 2", prompt);
        Assert.Contains("Currently overdue: 1", prompt);
        Assert.Contains("Project Alpha", prompt);
        Assert.Contains("3 capture(s) linked", prompt);
    }

    [Fact]
    public void BuildUserPrompt_EmptyData_ShowsNoActivityMessages()
    {
        var weekStart = new DateTimeOffset(2026, 4, 13, 0, 0, 0, TimeSpan.Zero);
        var weekEnd = weekStart.AddDays(7);

        var prompt = WeeklyBriefPromptBuilder.BuildUserPrompt(
            Array.Empty<CaptureContextForBrief>(), 0, 0, 0,
            Array.Empty<InitiativeContextForBrief>(), weekStart, weekEnd);

        Assert.Contains("No captures this week", prompt);
        Assert.Contains("No initiative activity", prompt);
    }
}
