using System.Xml.Linq;
using Google;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace MentalMetal.Web.Auth;

/// <summary>
/// Persists ASP.NET Core DataProtection keys to Google Cloud Storage as one
/// object per key (mirroring the behaviour of <c>FileSystemXmlRepository</c>).
/// Used in environments (e.g. Cloud Run) where the local filesystem is ephemeral
/// and keys must survive container restarts so OAuth state cookies issued by one
/// instance can be validated by another (#75 Bug 4).
///
/// Each <see cref="StoreElement"/> call writes a distinct object named
/// <c>{prefix}{friendlyName}.xml</c>, so concurrent writes from multiple Cloud
/// Run instances cannot clobber each other (the previous single-object,
/// read-modify-write design could drop keys under a last-writer-wins race).
/// </summary>
internal sealed class GoogleCloudStorageXmlRepository : IXmlRepository
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;
    private readonly string _objectPrefix;
    private readonly ILogger<GoogleCloudStorageXmlRepository> _logger;

    public GoogleCloudStorageXmlRepository(
        StorageClient storageClient,
        string bucketName,
        string objectPrefix,
        ILogger<GoogleCloudStorageXmlRepository> logger)
    {
        _storageClient = storageClient;
        _bucketName = bucketName;
        // Normalise the prefix so callers may pass "keys" or "keys/" interchangeably;
        // empty means "store at the bucket root".
        _objectPrefix = string.IsNullOrEmpty(objectPrefix) || objectPrefix.EndsWith('/')
            ? objectPrefix ?? string.Empty
            : objectPrefix + "/";
        _logger = logger;
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var elements = new List<XElement>();
        try
        {
            foreach (var obj in _storageClient.ListObjects(_bucketName, _objectPrefix))
            {
                if (!obj.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                using var stream = new MemoryStream();
                _storageClient.DownloadObject(_bucketName, obj.Name, stream);
                if (stream.Length == 0)
                {
                    continue;
                }

                stream.Position = 0;
                var doc = XDocument.Load(stream);
                if (doc.Root is not null)
                {
                    elements.Add(doc.Root);
                }
            }
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Array.Empty<XElement>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list DataProtection keys from gs://{Bucket}/{Prefix}", _bucketName, _objectPrefix);
            throw;
        }

        return elements.AsReadOnly();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        // One object per key — avoids the read-modify-write race that would
        // otherwise let two concurrent writers drop each other's keys.
        var safeName = SanitiseFriendlyName(friendlyName);
        var objectName = $"{_objectPrefix}{safeName}.xml";

        using var stream = new MemoryStream();
        new XDocument(element).Save(stream);
        stream.Position = 0;

        try
        {
            _storageClient.UploadObject(_bucketName, objectName, "application/xml", stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write DataProtection key to gs://{Bucket}/{Object}", _bucketName, objectName);
            throw;
        }
    }

    private static string SanitiseFriendlyName(string friendlyName)
    {
        if (string.IsNullOrWhiteSpace(friendlyName))
        {
            return Guid.NewGuid().ToString("N");
        }

        // Replace characters that would produce awkward object names; the DataProtection
        // framework typically passes the key's GUID, so this is a belt-and-braces guard.
        var invalid = new[] { '/', '\\', '\0', '\r', '\n' };
        var cleaned = friendlyName;
        foreach (var c in invalid)
        {
            cleaned = cleaned.Replace(c, '_');
        }
        return cleaned;
    }
}
