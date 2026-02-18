using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.Azure;

/// <summary>
/// Cosmos DB repository for blob metadata
/// </summary>
public class CosmosBlobMetadataRepository : IBlobMetadataRepository
{
    private readonly Container _AuditTrailContainer;
    private readonly Container _BlobMetadataContainer;

    public CosmosBlobMetadataRepository(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:CosmosDB:Endpoint"]
            ?? throw new ArgumentException("Azure:CosmosDB:Endpoint not configured");
        var key = configuration["Azure:CosmosDB:Key"]
            ?? throw new ArgumentException("Azure:CosmosDB:Key not configured");
        
        var databaseName = configuration["Azure:CosmosDB:DatabaseId"] ?? "ClaimsDatabase";
        var AuditTrailContainer = configuration["Azure:CosmosDB:ContainerId"] ?? "AuditTrail";
        var BlobMetadataContainer = configuration["Azure:CosmosDB:BlobMetadataContainer"] ?? "blob-metadata";
        
        Console.WriteLine($"[CosmosBlobMetadata] Connecting to database: {databaseName}");

        var client = new CosmosClient(endpoint, key);
        _AuditTrailContainer = client.GetContainer(databaseName, AuditTrailContainer);
        _BlobMetadataContainer = client.GetContainer(databaseName, BlobMetadataContainer);

        //Console.WriteLine($"[CosmosBlobMetadata] Connected to {databaseName}/{AuditTrailContainer}");
    }

    public async Task<BlobMetadata> SaveAsync(BlobMetadata metadata)
    {
        try
        {
            Console.WriteLine($"[CosmosBlobMetadata] Attempting to save:");
            Console.WriteLine($"  Id: '{metadata.Id}' (Length: {metadata.Id?.Length ?? 0})");
            Console.WriteLine($"  DocumentId: '{metadata.DocumentId}' (Length: {metadata.DocumentId?.Length ?? 0})");
            Console.WriteLine($"  BlobName: '{metadata.BlobName}'");
            Console.WriteLine($"  ContainerName: '{metadata.ContainerName}'");
            
            // Validate required fields
            if (string.IsNullOrWhiteSpace(metadata.Id))
                throw new ArgumentException("Id cannot be null or empty");
            if (string.IsNullOrWhiteSpace(metadata.DocumentId))
                throw new ArgumentException("DocumentId cannot be null or empty");
            
            var response = await _BlobMetadataContainer.UpsertItemAsync(
                metadata,
                new PartitionKey(metadata.DocumentId)
            );
            
            Console.WriteLine($"[CosmosBlobMetadata] ✓ Saved mapping: {metadata.DocumentId} -> {metadata.BlobName}");
            return response.Resource;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CosmosBlobMetadata] ✗ Save error: {ex.Message}");
            Console.WriteLine($"[CosmosBlobMetadata] ✗ Stack trace: {ex.StackTrace}");
            throw new InvalidOperationException($"Failed to save blob metadata: {ex.Message}", ex);
        }
    }

    public async Task<BlobMetadata?> GetByDocumentIdAsync(string documentId)
    {
        try
        {
            // Query by DocumentId since the Cosmos DB 'id' is a GUID, not the documentId
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.DocumentId = @documentId")
                .WithParameter("@documentId", documentId);
            
            var iterator = _BlobMetadataContainer.GetItemQueryIterator<BlobMetadata>(query);
            
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }
            
            Console.WriteLine($"[CosmosBlobMetadata] Not found: {documentId}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CosmosBlobMetadata] Get error: {ex.Message}");
            throw new InvalidOperationException($"Failed to get blob metadata: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteAsync(string documentId)
    {
        try
        {
            await _BlobMetadataContainer.DeleteItemAsync<BlobMetadata>(
                documentId,
                new PartitionKey(documentId)
            );
            
            Console.WriteLine($"[CosmosBlobMetadata] Deleted: {documentId}");
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine($"[CosmosBlobMetadata] Already deleted: {documentId}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CosmosBlobMetadata] Delete error: {ex.Message}");
            throw new InvalidOperationException($"Failed to delete blob metadata: {ex.Message}", ex);
        }
    }

    public async Task<List<BlobMetadata>> GetByUserIdAsync(string userId)
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.UserId = @userId AND c.DeletedAt = null"
            ).WithParameter("@userId", userId);

            var iterator = _BlobMetadataContainer.GetItemQueryIterator<BlobMetadata>(query);
            var results = new List<BlobMetadata>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            Console.WriteLine($"[CosmosBlobMetadata] Found {results.Count} documents for user: {userId}");
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CosmosBlobMetadata] Query error: {ex.Message}");
            throw new InvalidOperationException($"Failed to query blob metadata: {ex.Message}", ex);
        }
    }
}
