using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

public interface IDocumentUploadService
{
    Task<DocumentUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType, string userId);
    Task<Stream> DownloadAsync(string documentId);
    Task DeleteAsync(string documentId);
    Task<bool> ExistsAsync(string documentId);
}
