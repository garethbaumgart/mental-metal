namespace MentalMetal.Infrastructure.Ai;

public class DeepgramSettings
{
    public const string SectionName = "Deepgram";

    public string BaseUrl { get; set; } = "api.deepgram.com";
    public string Model { get; set; } = "nova-3";
    public bool Punctuate { get; set; } = true;
    public bool InterimResults { get; set; } = true;
    public string Language { get; set; } = "en-US";
    public bool Diarize { get; set; } = true;
    public int KeepAliveIntervalSeconds { get; set; } = 10;
}
