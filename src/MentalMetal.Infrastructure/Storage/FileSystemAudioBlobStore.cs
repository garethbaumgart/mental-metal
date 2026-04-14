using MentalMetal.Application.Common;
using Microsoft.Extensions.Options;

namespace MentalMetal.Infrastructure.Storage;

/// <summary>
/// Default <see cref="IAudioBlobStore"/> implementation backed by the local
/// filesystem. Blobs live at <c>{RootPath}/{userId}/{guid}.{ext}</c>.
/// </summary>
public sealed class FileSystemAudioBlobStore(IOptions<AudioBlobStoreOptions> options) : IAudioBlobStore
{
    private readonly string _rootPath = options.Value.RootPath;

    public async Task<string> SaveAsync(
        Guid userId, Stream audio, string mimeType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audio);

        var ext = MimeToExtension(mimeType);
        var id = Guid.NewGuid();
        var relative = Path.Combine(userId.ToString(), $"{id}{ext}");
        var absolute = Path.Combine(_rootPath, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);

        await using var file = File.Create(absolute);
        await audio.CopyToAsync(file, cancellationToken);

        // Return the relative ref so the on-disk layout can move without
        // rewriting stored references.
        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    public Task<Stream> OpenReadAsync(string blobRef, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobRef);
        var absolute = Path.Combine(_rootPath, blobRef.Replace('/', Path.DirectorySeparatorChar));
        Stream stream = File.OpenRead(absolute);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(blobRef))
            return Task.CompletedTask;
        var absolute = Path.Combine(_rootPath, blobRef.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolute))
            File.Delete(absolute);
        return Task.CompletedTask;
    }

    private static string MimeToExtension(string? mimeType) => mimeType switch
    {
        "audio/webm" => ".webm",
        "audio/mp4" or "audio/x-m4a" => ".m4a",
        "audio/mpeg" => ".mp3",
        "audio/wav" or "audio/x-wav" => ".wav",
        "audio/ogg" => ".ogg",
        _ => ".bin",
    };
}
