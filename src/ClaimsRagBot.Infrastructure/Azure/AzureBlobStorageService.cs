using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.Azure;

/// <summary>
/// Azure Blob Storage implementation for document uploads with proper metadata tracking
/// </summary>
public class AzureBlobStorageService : IDocumentUploadService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _containerClient;
    private readonly IBlobMetadataRepository? _metadataRepository;
    private readonly string _containerName;
    private readonly string _uploadPrefix;
    private readonly int _sasTokenExpiration;

    public AzureBlobStorageService(
        IConfiguration configuration,
        IBlobMetadataRepository? metadataRepository = null)
    {
        var connectionString = configuration["Azure:BlobStorage:ConnectionString"] 
            ?? throw new ArgumentException("Azure:BlobStorage:ConnectionString not configured");
        
        _containerName = configuration["Azure:BlobStorage:ContainerName"] ?? "claims-documents";
        _uploadPrefix = configuration["Azure:BlobStorage:UploadPrefix"] ?? "uploads/";
        _sasTokenExpiration = int.Parse(configuration["Azure:BlobStorage:SasTokenExpiration"] ?? "3600");
        _metadataRepository = metadataRepository;

        _blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        
        if (_metadataRepository == null)
        {
            Console.WriteLine($"[BlobStorage] WARNING: Running without metadata repository - download/delete by documentId will not work");
        }
        Console.WriteLine($"[BlobStorage] Connected to container: {_containerName}");
    }

    public async Task<DocumentUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType, string userId)
    {
        var documentId = Guid.NewGuid().ToString();
        var blobName = $"{_uploadPrefix}{userId}/{Guid.NewGuid()}_{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        try
        {
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
                Metadata = new Dictionary<string, string>
                {
                    { "DocumentId", documentId },
                    { "FileName", fileName },
                    { "UserId", userId }
                }
            };

            var uploadResult = await blobClient.UploadAsync(fileStream, uploadOptions);
            var properties = await blobClient.GetPropertiesAsync();

            // Save blob metadata mapping if repository available
            if (_metadataRepository != null)
            {
                try
                {
                    var metadata = new BlobMetadata
                    {
                        DocumentId = documentId,
                        BlobName = blobName,
                        ContainerName = _containerName,
                        FileName = fileName,
                        ContentType = contentType,
                        FileSize = properties.Value.ContentLength,
                        UserId = userId,
                        UploadedAt = DateTime.UtcNow
                    };

                    await _metadataRepository.SaveAsync(metadata);
                }
                catch (Exception metaEx)
                {
                    Console.WriteLine($"[BlobStorage] Warning: Failed to save metadata: {metaEx.Message}");
                    // Continue - blob is uploaded, just metadata failed
                }
            }

            var result = new DocumentUploadResult(
                documentId,
                _containerName,
                blobName,
                contentType,
                properties.Value.ContentLength,
                DateTime.UtcNow
            );

            Console.WriteLine($"[BlobStorage] Uploaded: {documentId} -> {blobName}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BlobStorage] Upload error: {ex.Message}");
            throw new InvalidOperationException($"Blob storage upload failed: {ex.Message}", ex);
        }
    }

    public async Task<DocumentUploadResult> GetDocumentAsync(string documentId)
    {
        try
        {
            var metadata = await _metadataRepository!.GetByDocumentIdAsync(documentId);
            if (metadata == null)
            {
                throw new FileNotFoundException($"Document metadata not found: {documentId}");
            }

            var result = new DocumentUploadResult(
                metadata.DocumentId,
                metadata.ContainerName,
                metadata.BlobName,
                metadata.ContentType,
                metadata.FileSize,
                metadata.UploadedAt
            );

            Console.WriteLine($"[BlobStorage] Retrieved metadata for: {documentId}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BlobStorage] GetDocument error: {ex.Message}");
            throw new InvalidOperationException($"Failed to get document metadata: {ex.Message}", ex);
        }
    }

    public async Task<Stream> DownloadAsync(string documentId)
    {
        try
        {
            // Get blob name from metadata repository
            var metadata = await _metadataRepository!.GetByDocumentIdAsync(documentId);
            if (metadata == null)
            {
                throw new FileNotFoundException($"Document not found: {documentId}");
            }

            var blobClient = _containerClient.GetBlobClient(metadata.BlobName);
            var download = await blobClient.DownloadAsync();
            
            Console.WriteLine($"[BlobStorage] Downloaded: {documentId} ({metadata.BlobName})");
            return download.Value.Content;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BlobStorage] Download error: {ex.Message}");
            throw new InvalidOperationException($"Blob storage download failed: {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(string documentId)
    {
        try
        {
            // Get blob name from metadata repository
            var metadata = await _metadataRepository!.GetByDocumentIdAsync(documentId);
            if (metadata == null)
            {
                Console.WriteLine($"[BlobStorage] Document not found for deletion: {documentId}");
                return;
            }

            var blobClient = _containerClient.GetBlobClient(metadata.BlobName);
            await blobClient.DeleteIfExistsAsync();
            
            // Mark as deleted in metadata
            metadata.DeletedAt = DateTime.UtcNow;
            await _metadataRepository!.SaveAsync(metadata);
            
            Console.WriteLine($"[BlobStorage] Deleted: {documentId} ({metadata.BlobName})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BlobStorage] Delete error: {ex.Message}");
            throw new InvalidOperationException($"Blob storage delete failed: {ex.Message}", ex);
        }
    }

    public async Task<bool> ExistsAsync(string documentId)
    {
        try
        {
            var metadata = await _metadataRepository!.GetByDocumentIdAsync(documentId);
            if (metadata == null || metadata.DeletedAt != null)
            {
                return false;
            }

            var blobClient = _containerClient.GetBlobClient(metadata.BlobName);
            return await blobClient.ExistsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BlobStorage] Exists check error: {ex.Message}");
            return false;
        }
    }
}
