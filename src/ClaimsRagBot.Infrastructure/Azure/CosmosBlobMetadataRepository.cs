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
    private readonly Container _container;

    public CosmosBlobMetadataRepository(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:CosmosDB:Endpoint"]
            ?? throw new ArgumentException("Azure:CosmosDB:Endpoint not configured");
        var key = configuration["Azure:CosmosDB:Key"]
            ?? throw new ArgumentException("Azure:CosmosDB:Key not configured");
        
        var databaseName = configuration["Azure:CosmosDB:DatabaseName"] ?? "ClaimsRagBot";
        var containerName = configuration["Azure:CosmosDB:BlobMetadataContainer"] ?? "blob-metadata";

        var client = new CosmosClient(endpoint, key);
        _container = client.GetContainer(databaseName, containerName);
        
        Console.WriteLine($"[CosmosBlobMetadata] Connected to {databaseName}/{containerName}");
    }

    public async Task<BlobMetadata> SaveAsync(BlobMetadata metadata)
    {
        try
        {
            var response = await _container.UpsertItemAsync(
                metadata,
                new PartitionKey(metadata.DocumentId)
            );
            
            Console.WriteLine($"[CosmosBlobMetadata] Saved mapping: {metadata.DocumentId} -> {metadata.BlobName}");
            return response.Resource;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CosmosBlobMetadata] Save error: {ex.Message}");
            throw new InvalidOperationException($"Failed to save blob metadata: {ex.Message}", ex);
        }
    }

    public async Task<BlobMetadata?> GetByDocumentIdAsync(string documentId)
    {
        try
        {
            var response = await _container.ReadItemAsync<BlobMetadata>(
                documentId,
                new PartitionKey(documentId)
            );
            
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
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
            await _container.DeleteItemAsync<BlobMetadata>(
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

            var iterator = _container.GetItemQueryIterator<BlobMetadata>(query);
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
