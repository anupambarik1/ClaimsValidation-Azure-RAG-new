using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

public interface IDocumentExtractionService
{
    Task<ClaimExtractionResult> ExtractClaimDataAsync(string documentId, DocumentType documentType);
    Task<ClaimExtractionResult> ExtractClaimDataAsync(DocumentUploadResult uploadResult, DocumentType documentType);
    Task<ClaimExtractionResult> ExtractFromMultipleDocumentsAsync(List<string> documentIds, DocumentType documentType);
    Task<string> ExtractDocumentContentAsync(string documentId);
}
