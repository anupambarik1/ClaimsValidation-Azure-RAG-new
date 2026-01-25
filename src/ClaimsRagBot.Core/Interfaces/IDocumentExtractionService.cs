using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

public interface IDocumentExtractionService
{
    Task<ClaimExtractionResult> ExtractClaimDataAsync(string documentId, DocumentType documentType);
    Task<ClaimExtractionResult> ExtractFromMultipleDocumentsAsync(List<string> documentIds, DocumentType documentType);
}
