using MentalMetal.Application.People.Dossier;

namespace MentalMetal.Application.Tests.People.Dossier;

public class DossierPromptBuilderTests
{
    [Fact]
    public void SystemPrompt_Default_ContainsExpectedContent()
    {
        var prompt = DossierPromptBuilder.SystemPrompt("default");

        Assert.Contains("What should I know about this person", prompt);
        Assert.Contains("Contradictions", prompt);
        Assert.Contains("Signals", prompt);
    }

    [Fact]
    public void SystemPrompt_Prep_ContainsPreMeetingContent()
    {
        var prompt = DossierPromptBuilder.SystemPrompt("prep");

        Assert.Contains("pre-meeting", prompt);
        Assert.Contains("Talking points", prompt);
        Assert.Contains("Watch for", prompt);
    }

    [Fact]
    public void BuildUserPrompt_IncludesPersonNameAndRole()
    {
        var prompt = DossierPromptBuilder.BuildUserPrompt(
            "Alice",
            "Engineering Lead",
            "Platform",
            Array.Empty<MentionContextForPrompt>(),
            Array.Empty<CommitmentContextForPrompt>());

        Assert.Contains("Alice", prompt);
        Assert.Contains("Engineering Lead", prompt);
        Assert.Contains("Platform", prompt);
    }

    [Fact]
    public void BuildUserPrompt_IncludesMentionsAndCommitments()
    {
        var mentions = new[]
        {
            new MentionContextForPrompt("Sprint Planning", DateTimeOffset.UtcNow, "Discussed roadmap.", "Led the discussion")
        };

        var commitments = new[]
        {
            new CommitmentContextForPrompt("Send updated spec", "MineToThem", DateOnly.FromDateTime(DateTime.UtcNow), false)
        };

        var prompt = DossierPromptBuilder.BuildUserPrompt(
            "Bob", null, null, mentions, commitments);

        Assert.Contains("Sprint Planning", prompt);
        Assert.Contains("Led the discussion", prompt);
        Assert.Contains("Send updated spec", prompt);
        Assert.Contains("I owe them", prompt);
    }

    [Fact]
    public void BuildUserPrompt_EmptyData_ShowsNoDataMessages()
    {
        var prompt = DossierPromptBuilder.BuildUserPrompt(
            "Charlie", null, null,
            Array.Empty<MentionContextForPrompt>(),
            Array.Empty<CommitmentContextForPrompt>());

        Assert.Contains("No recent mentions found", prompt);
        Assert.Contains("No open commitments", prompt);
    }
}
