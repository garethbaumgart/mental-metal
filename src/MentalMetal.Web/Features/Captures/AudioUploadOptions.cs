using System.ComponentModel.DataAnnotations;

namespace MentalMetal.Web.Features.Captures;

public sealed class AudioUploadOptions
{
    public const string SectionName = "AudioUpload";

    /// <summary>
    /// Maximum upload size in bytes. Default 50 MB. Bounded to prevent
    /// accidental production misconfiguration.
    /// </summary>
    [Range(1, 500_000_000)]
    public long MaxSizeBytes { get; set; } = 50L * 1024 * 1024;

    [Required]
    [MinLength(1)]
    public List<string> AllowedMimeTypes { get; set; } = new()
    {
        "audio/webm",
        "audio/mp4",
        "audio/mpeg",
        "audio/wav",
        "audio/ogg",
        "video/webm",  // getDisplayMedia (tab capture) produces video/webm even for audio-only
    };
}
