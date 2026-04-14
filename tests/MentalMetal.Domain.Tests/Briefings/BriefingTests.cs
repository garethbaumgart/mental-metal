using MentalMetal.Domain.Briefings;

namespace MentalMetal.Domain.Tests.Briefings;

public class BriefingTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 4, 14, 8, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Valid_PopulatesAllFields()
    {
        var briefing = Briefing.Create(
            UserId,
            BriefingType.Morning,
            "morning:2026-04-14",
            Now,
            "# Morning briefing\n\nFocus today on...",
            "{\"topCommitmentsDueToday\":[]}",
            "claude-sonnet-4-5",
            inputTokens: 1200,
            outputTokens: 320);

        Assert.NotEqual(Guid.Empty, briefing.Id);
        Assert.Equal(UserId, briefing.UserId);
        Assert.Equal(BriefingType.Morning, briefing.Type);
        Assert.Equal("morning:2026-04-14", briefing.ScopeKey);
        Assert.Equal(Now, briefing.GeneratedAtUtc);
        Assert.Equal("# Morning briefing\n\nFocus today on...", briefing.MarkdownBody);
        Assert.Equal("{\"topCommitmentsDueToday\":[]}", briefing.PromptFactsJson);
        Assert.Equal("claude-sonnet-4-5", briefing.Model);
        Assert.Equal(1200, briefing.InputTokens);
        Assert.Equal(320, briefing.OutputTokens);
    }

    [Fact]
    public void Create_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Briefing.Create(Guid.Empty, BriefingType.Weekly, "weekly:2026-W16", Now, "body", "{}", "model", 1, 1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyScopeKey_Throws(string? scopeKey)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Briefing.Create(UserId, BriefingType.Weekly, scopeKey!, Now, "body", "{}", "model", 1, 1));
    }

    [Fact]
    public void Create_ScopeKeyTooLong_Throws()
    {
        var tooLong = new string('a', 129);
        Assert.Throws<ArgumentException>(() =>
            Briefing.Create(UserId, BriefingType.Morning, tooLong, Now, "body", "{}", "model", 1, 1));
    }

    [Fact]
    public void Create_ModelTooLong_Throws()
    {
        var tooLong = new string('m', 65);
        Assert.Throws<ArgumentException>(() =>
            Briefing.Create(UserId, BriefingType.Morning, "morning:2026-04-14", Now, "body", "{}", tooLong, 1, 1));
    }

    [Fact]
    public void Create_NullMarkdownBody_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Briefing.Create(UserId, BriefingType.Morning, "morning:2026-04-14", Now, null!, "{}", "model", 1, 1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankPromptFactsJson_Throws(string? json)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Briefing.Create(UserId, BriefingType.Morning, "morning:2026-04-14", Now, "body", json!, "model", 1, 1));
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{")]
    [InlineData("{\"unclosed\":")]
    public void Create_MalformedPromptFactsJson_Throws(string json)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Briefing.Create(UserId, BriefingType.Morning, "morning:2026-04-14", Now, "body", json, "model", 1, 1));
        Assert.Equal("promptFactsJson", ex.ParamName);
    }

    [Fact]
    public void Create_EmptyMarkdownBody_Allowed()
    {
        // Body may be empty (degenerate AI response) - the factory allows it; only null is rejected.
        var briefing = Briefing.Create(UserId, BriefingType.Morning, "morning:2026-04-14", Now, string.Empty, "{}", "m", 0, 0);
        Assert.Equal(string.Empty, briefing.MarkdownBody);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void Create_NegativeTokens_Throws(int input, int output)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Briefing.Create(UserId, BriefingType.Morning, "morning:2026-04-14", Now, "body", "{}", "model", input, output));
    }
}
