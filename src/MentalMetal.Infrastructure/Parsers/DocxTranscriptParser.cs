using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MentalMetal.Application.Captures.ImportCapture;

namespace MentalMetal.Infrastructure.Parsers;

public sealed class DocxTranscriptParser : ITranscriptFileParser
{
    public bool CanHandle(string contentType, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLower();
        return contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            || ext == ".docx"
            || (contentType == "application/octet-stream" && ext == ".docx");
    }

    public Task<string> ExtractTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null)
            return Task.FromResult(string.Empty);

        var sb = new StringBuilder();
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            sb.AppendLine(paragraph.InnerText);
        }

        return Task.FromResult(sb.ToString().Trim());
    }
}
