namespace MentalMetal.Application.Common;

/// <summary>
/// Abstraction over audio-blob persistence. Default implementation is the
/// filesystem store in Infrastructure; cloud implementations are future work.
/// </summary>
public interface IAudioBlobStore
{
    /// <summary>
    /// Persists the audio bytes and returns an opaque reference that can be
    /// fed back to <see cref="OpenReadAsync"/> / <see cref="DeleteAsync"/>.
    /// </summary>
    Task<string> SaveAsync(Guid userId, Stream audio, string mimeType, CancellationToken cancellationToken);

    /// <summary>
    /// Opens the blob for reading. Callers must dispose the returned stream.
    /// </summary>
    Task<Stream> OpenReadAsync(string blobRef, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the blob. Silently returns if the blob does not exist.
    /// </summary>
    Task DeleteAsync(string blobRef, CancellationToken cancellationToken);
}
