using MentalMetal.Application.Common.Ai;

namespace MentalMetal.Application.Tests.Common.Ai;

public class JsonResponseParserTests
{
    [Fact]
    public void StripCodeFences_JsonLanguageFence_StripsFence()
    {
        const string input = "```json\n{\"summary\":\"hi\"}\n```";

        var result = JsonResponseParser.StripCodeFences(input);

        Assert.Equal("{\"summary\":\"hi\"}", result);
    }

    [Fact]
    public void StripCodeFences_PlainFence_StripsFence()
    {
        const string input = "```\n{\"summary\":\"hi\"}\n```";

        var result = JsonResponseParser.StripCodeFences(input);

        Assert.Equal("{\"summary\":\"hi\"}", result);
    }

    [Fact]
    public void StripCodeFences_UppercaseLanguageTag_StripsFence()
    {
        const string input = "```JSON\n{\"a\":1}\n```";

        var result = JsonResponseParser.StripCodeFences(input);

        Assert.Equal("{\"a\":1}", result);
    }

    [Fact]
    public void StripCodeFences_SurroundingWhitespace_Trims()
    {
        const string input = "   \n```json\n{\"a\":1}\n```\n   ";

        var result = JsonResponseParser.StripCodeFences(input);

        Assert.Equal("{\"a\":1}", result);
    }

    [Fact]
    public void StripCodeFences_UnfencedJson_LeavesContentUnchangedExceptTrim()
    {
        const string input = "{\"summary\":\"hi\"}";

        var result = JsonResponseParser.StripCodeFences(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void StripCodeFences_UnfencedJsonWithSurroundingWhitespace_Trims()
    {
        const string input = "  \n{\"summary\":\"hi\"}\n  ";

        var result = JsonResponseParser.StripCodeFences(input);

        Assert.Equal("{\"summary\":\"hi\"}", result);
    }

    [Fact]
    public void StripCodeFences_FenceWithTrailingNewlineAfterClose_StripsCleanly()
    {
        const string input = "```json\n{\"a\":1}\n```\n";

        var result = JsonResponseParser.StripCodeFences(input);

        Assert.Equal("{\"a\":1}", result);
    }

    [Fact]
    public void StripCodeFences_EmptyString_ReturnsEmpty()
    {
        var result = JsonResponseParser.StripCodeFences(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void StripCodeFences_MultilineJsonInsideFence_PreservesInternalNewlines()
    {
        const string input = "```json\n{\n  \"a\": 1,\n  \"b\": 2\n}\n```";

        var result = JsonResponseParser.StripCodeFences(input);

        Assert.Equal("{\n  \"a\": 1,\n  \"b\": 2\n}", result);
    }
}
