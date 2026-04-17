namespace MentalMetal.Application.Captures.ImportCapture;

public interface ITranscriptFileParser
{
    bool CanHandle(string contentType, string fileName);
    Task<string> ExtractTextAsync(Stream stream, CancellationToken cancellationToken);
}
