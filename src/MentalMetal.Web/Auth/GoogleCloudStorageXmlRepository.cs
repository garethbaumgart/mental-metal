using System.Xml.Linq;
using Google;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace MentalMetal.Web.Auth;

/// <summary>
/// Persists ASP.NET Core DataProtection keys to a Google Cloud Storage object.
/// Used in environments (e.g. Cloud Run) where the local filesystem is ephemeral
/// and keys must survive container restarts so OAuth state cookies issued by one
/// instance can be validated by another (#75 Bug 4).
/// </summary>
internal sealed class GoogleCloudStorageXmlRepository : IXmlRepository
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;
    private readonly string _objectName;
    private readonly ILogger<GoogleCloudStorageXmlRepository> _logger;
    private readonly object _writeLock = new();

    public GoogleCloudStorageXmlRepository(
        StorageClient storageClient,
        string bucketName,
        string objectName,
        ILogger<GoogleCloudStorageXmlRepository> logger)
    {
        _storageClient = storageClient;
        _bucketName = bucketName;
        _objectName = objectName;
        _logger = logger;
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        try
        {
            using var stream = new MemoryStream();
            _storageClient.DownloadObject(_bucketName, _objectName, stream);
            stream.Position = 0;
            if (stream.Length == 0)
            {
                return Array.Empty<XElement>();
            }

            var doc = XDocument.Load(stream);
            return doc.Root?.Elements().ToList().AsReadOnly()
                ?? (IReadOnlyCollection<XElement>)Array.Empty<XElement>();
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Array.Empty<XElement>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load DataProtection keys from gs://{Bucket}/{Object}", _bucketName, _objectName);
            throw;
        }
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        // The DataProtection contract does not require atomic concurrent writes —
        // serialise locally to avoid clobbering when multiple keys rotate at once.
        lock (_writeLock)
        {
            var existing = GetAllElements();
            var root = new XElement("repository");
            foreach (var existingElement in existing)
            {
                root.Add(existingElement);
            }
            root.Add(element);

            using var stream = new MemoryStream();
            new XDocument(root).Save(stream);
            stream.Position = 0;

            _storageClient.UploadObject(_bucketName, _objectName, "application/xml", stream);
        }
    }
}
