using Microsoft.AspNetCore.Mvc;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Api.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentUploadService _uploadService;
    private readonly IDocumentExtractionService _extractionService;
    private readonly ILogger<DocumentsController> _logger;
    private readonly long _maxFileSizeBytes;
    private readonly string[] _allowedContentTypes;

    public DocumentsController(
        IDocumentUploadService uploadService,
        IDocumentExtractionService extractionService,
        ILogger<DocumentsController> logger,
        IConfiguration configuration)
    {
        _uploadService = uploadService;
        _extractionService = extractionService;
        _logger = logger;
        
        _maxFileSizeBytes = long.Parse(configuration["DocumentProcessing:MaxFileSizeMB"] ?? "10") * 1024 * 1024;
        _allowedContentTypes = configuration.GetSection("DocumentProcessing:AllowedContentTypes").Get<string[]>() 
            ?? new[] { "application/pdf", "image/jpeg", "image/png" };
    }

    /// <summary>
    /// Upload a document for claim extraction
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10_485_760)] // 10MB
    public async Task<ActionResult<DocumentUploadResult>> UploadDocument(IFormFile file, [FromForm] string? userId = null)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }
            
            if (file.Length > _maxFileSizeBytes)
            {
                return BadRequest(new { error = $"File size exceeds maximum allowed size of {_maxFileSizeBytes / (1024 * 1024)}MB" });
            }
            
            if (!_allowedContentTypes.Contains(file.ContentType))
            {
                return BadRequest(new { error = $"File type {file.ContentType} not allowed. Supported types: {string.Join(", ", _allowedContentTypes)}" });
            }
            
            var effectiveUserId = userId ?? "anonymous";
            
            _logger.LogInformation("Uploading document: {FileName} ({Size} bytes) for user: {UserId}", 
                file.FileName, file.Length, effectiveUserId);
            
            using var stream = file.OpenReadStream();
            var result = await _uploadService.UploadAsync(stream, file.FileName, file.ContentType, effectiveUserId);
            
            _logger.LogInformation("Document uploaded successfully: {DocumentId}", result.DocumentId);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, new { error = "Failed to upload document", details = ex.Message });
        }
    }

    /// <summary>
    /// Extract claim data from an uploaded document
    /// </summary>
    [HttpPost("extract")]
    public async Task<ActionResult<ClaimExtractionResult>> ExtractClaimData(
        [FromBody] ExtractClaimRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
            {
                return BadRequest(new { error = "DocumentId is required" });
            }
            
            _logger.LogInformation("Starting claim extraction for document: {DocumentId}, type: {DocumentType}", 
                request.DocumentId, request.DocumentType);
            
            var result = await _extractionService.ExtractClaimDataAsync(
                request.DocumentId, 
                request.DocumentType);
            
            _logger.LogInformation("Extraction completed for document: {DocumentId}, confidence: {Confidence:F2}", 
                request.DocumentId, result.OverallConfidence);
            
            return Ok(result);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Document not found: {DocumentId}", request.DocumentId);
            return NotFound(new { error = "Document not found", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting claim data from document: {DocumentId}", request.DocumentId);
            return StatusCode(500, new { error = "Failed to extract claim data", details = ex.Message });
        }
    }

    /// <summary>
    /// Extract claim data from multiple documents (combined analysis)
    /// </summary>
    [HttpPost("extract-multiple")]
    public async Task<ActionResult<ClaimExtractionResult>> ExtractFromMultipleDocuments(
        [FromBody] ExtractMultipleClaimRequest request)
    {
        try
        {
            if (request.DocumentIds == null || !request.DocumentIds.Any())
            {
                return BadRequest(new { error = "At least one DocumentId is required" });
            }
            
            _logger.LogInformation("Starting multi-document extraction for {Count} documents", request.DocumentIds.Count);
            
            var result = await _extractionService.ExtractFromMultipleDocumentsAsync(
                request.DocumentIds, 
                request.DocumentType);
            
            _logger.LogInformation("Multi-document extraction completed, confidence: {Confidence:F2}", 
                result.OverallConfidence);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting claim data from multiple documents");
            return StatusCode(500, new { error = "Failed to extract claim data", details = ex.Message });
        }
    }

    /// <summary>
    /// Upload document and extract claim data in one step
    /// </summary>
    [HttpPost("submit")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<SubmitDocumentResponse>> SubmitDocument(
        IFormFile file, 
        [FromForm] string? userId = null,
        [FromForm] string documentType = "ClaimForm")
    {
        try
        {
            // Step 1: Upload
            var uploadResult = await UploadDocument(file, userId);
            if (uploadResult.Result is not OkObjectResult uploadOk)
            {
                return uploadResult.Result!;
            }
            
            var uploadData = (DocumentUploadResult)uploadOk.Value!;
            
            // Step 2: Extract
            if (!Enum.TryParse<DocumentType>(documentType, out var docType))
            {
                docType = DocumentType.ClaimForm;
            }
            
            // Pass the entire upload result to avoid redundant S3 lookups
            var extractionResult = await _extractionService.ExtractClaimDataAsync(
                uploadData, 
                docType);
            
            _logger.LogInformation("Document submitted and extracted: {DocumentId}, confidence: {Confidence:F2}", 
                uploadData.DocumentId, extractionResult.OverallConfidence);
            
            return Ok(new SubmitDocumentResponse(
                UploadResult: uploadData,
                ExtractionResult: extractionResult,
                ValidationStatus: DetermineValidationStatus(extractionResult),
                NextAction: DetermineNextAction(extractionResult)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting document");
            return StatusCode(500, new { error = "Failed to submit document", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete an uploaded document
    /// </summary>
    [HttpDelete("{documentId}")]
    public async Task<ActionResult> DeleteDocument(string documentId)
    {
        try
        {
            var exists = await _uploadService.ExistsAsync(documentId);
            if (!exists)
            {
                return NotFound(new { error = "Document not found" });
            }
            
            await _uploadService.DeleteAsync(documentId);
            
            _logger.LogInformation("Document deleted: {DocumentId}", documentId);
            
            return Ok(new { message = "Document deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document: {DocumentId}", documentId);
            return StatusCode(500, new { error = "Failed to delete document", details = ex.Message });
        }
    }

    private string DetermineValidationStatus(ClaimExtractionResult result)
    {
        if (result.OverallConfidence >= 0.85)
            return "ReadyForSubmission";
        else if (result.OverallConfidence >= 0.7)
            return "ReadyForReview";
        else
            return "RequiresCorrection";
    }

    private string DetermineNextAction(ClaimExtractionResult result)
    {
        if (result.OverallConfidence >= 0.85)
            return "ReviewAndConfirm";
        else if (result.AmbiguousFields.Any())
            return $"CorrectFields: {string.Join(", ", result.AmbiguousFields)}";
        else
            return "ManualEntry";
    }
}

// Request/Response DTOs
public record ExtractClaimRequest(
    string DocumentId,
    DocumentType DocumentType = DocumentType.ClaimForm
);

public record ExtractMultipleClaimRequest(
    List<string> DocumentIds,
    DocumentType DocumentType = DocumentType.Mixed
);

public record SubmitDocumentResponse(
    DocumentUploadResult UploadResult,
    ClaimExtractionResult ExtractionResult,
    string ValidationStatus,
    string NextAction
);
