using HtmlAgilityPack;
using MentalMetal.Application.Captures.ImportCapture;

namespace MentalMetal.Infrastructure.Parsers;

public sealed class HtmlTranscriptParser : ITranscriptFileParser
{
    public bool CanHandle(string contentType, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLower();
        return contentType == "text/html" || ext is ".html" or ".htm";
    }

    public async Task<string> ExtractTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        var doc = new HtmlDocument();
        doc.Load(stream);
        var text = doc.DocumentNode.InnerText;
        return await Task.FromResult(System.Net.WebUtility.HtmlDecode(text).Trim());
    }
}
