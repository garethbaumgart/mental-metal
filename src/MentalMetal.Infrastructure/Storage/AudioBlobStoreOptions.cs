using System.ComponentModel.DataAnnotations;

namespace MentalMetal.Infrastructure.Storage;

public sealed class AudioBlobStoreOptions
{
    public const string SectionName = "AudioBlobStore";

    /// <summary>
    /// Root directory for the filesystem-backed audio blob store. Ephemeral on
    /// Cloud Run — acceptable because blobs are discarded within seconds on
    /// the happy path.
    /// </summary>
    [Required]
    public string RootPath { get; set; } = string.Empty;
}
