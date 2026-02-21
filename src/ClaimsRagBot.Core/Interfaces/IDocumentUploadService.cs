using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

public interface IDocumentUploadService
{
    Task<DocumentUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType, string userId);
    Task<DocumentUploadResult> GetDocumentAsync(string documentId);
    Task<Stream> DownloadAsync(string documentId);
    Task<string> GetSecureDownloadUrlAsync(string documentId, int expirationMinutes = 60);
    Task DeleteAsync(string documentId);
    Task<bool> ExistsAsync(string documentId);
}
