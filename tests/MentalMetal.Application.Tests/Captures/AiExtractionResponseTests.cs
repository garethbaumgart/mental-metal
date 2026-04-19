using MentalMetal.Application.Captures;
using MentalMetal.Domain.Captures;

namespace MentalMetal.Application.Tests.Captures;

public class AiExtractionResponseTests
{
    [Fact]
    public void From_NullExtraction_ReturnsNull()
    {
        var result = AiExtractionResponse.From(null);

        Assert.Null(result);
    }

    [Fact]
    public void From_FullyPopulated_MapsAllFields()
    {
        var extraction = new AiExtraction
        {
            Summary = "Team sync meeting.",
            PeopleMentioned = new List<PersonMention>
            {
                new() { RawName = "Alice", PersonId = Guid.NewGuid(), Context = "owner" }
            },
            Commitments = new List<ExtractedCommitment>
            {
                new()
                {
                    Description = "Ship feature",
                    Direction = Domain.Commitments.CommitmentDirection.MineToThem,
                    Confidence = Domain.Commitments.CommitmentConfidence.High
                }
            },
            Decisions = new List<string> { "Approved timeline" },
            Risks = new List<string> { "Budget overrun" },
            InitiativeTags = new List<InitiativeTag>
            {
                new() { RawName = "Project X", InitiativeId = Guid.NewGuid() }
            },
            ExtractedAt = DateTimeOffset.UtcNow
        };

        var result = AiExtractionResponse.From(extraction)!;

        Assert.Equal("Team sync meeting.", result.Summary);
        Assert.Single(result.PeopleMentioned);
        Assert.Single(result.Commitments);
        Assert.Equal(["Approved timeline"], result.Decisions);
        Assert.Equal(["Budget overrun"], result.Risks);
        Assert.Single(result.InitiativeTags);
    }

    [Fact]
    public void From_NullCollections_ReturnsEmptyLists()
    {
        // EF Core JSON deserialization can produce null for missing JSON keys,
        // even when C# defaults are set. This test guards against that regression.
        var extraction = new AiExtraction
        {
            Summary = "Minimal extraction.",
            PeopleMentioned = null!,
            Commitments = null!,
            Decisions = null!,
            Risks = null!,
            InitiativeTags = null!,
            ExtractedAt = DateTimeOffset.UtcNow
        };

        var result = AiExtractionResponse.From(extraction)!;

        Assert.Equal("Minimal extraction.", result.Summary);
        Assert.Empty(result.PeopleMentioned);
        Assert.Empty(result.Commitments);
        Assert.Empty(result.Decisions);
        Assert.Empty(result.Risks);
        Assert.Empty(result.InitiativeTags);
    }
}
