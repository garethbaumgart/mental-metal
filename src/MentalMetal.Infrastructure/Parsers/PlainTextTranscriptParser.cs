using MentalMetal.Application.Captures.ImportCapture;

namespace MentalMetal.Infrastructure.Parsers;

public sealed class PlainTextTranscriptParser : ITranscriptFileParser
{
    public bool CanHandle(string contentType, string fileName) =>
        contentType.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase) ||
        Path.GetExtension(fileName).Equals(".txt", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExtractTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
