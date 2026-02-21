using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

/// <summary>
/// Repository for blob metadata operations
/// </summary>
public interface IBlobMetadataRepository
{
    Task<BlobMetadata> SaveAsync(BlobMetadata metadata);
    Task<BlobMetadata?> GetByDocumentIdAsync(string documentId);
    Task<List<BlobMetadata>> GetByDocumentIdsAsync(List<string> documentIds);
    Task<bool> DeleteAsync(string documentId);
    Task<List<BlobMetadata>> GetByUserIdAsync(string userId);
}
