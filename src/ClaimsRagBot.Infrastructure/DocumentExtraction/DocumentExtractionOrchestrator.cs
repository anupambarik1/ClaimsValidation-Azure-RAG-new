using Amazon.S3;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace ClaimsRagBot.Infrastructure.DocumentExtraction;

public class DocumentExtractionOrchestrator : IDocumentExtractionService
{
    private readonly IDocumentUploadService _uploadService;
    private readonly ITextractService _textractService;
    private readonly IComprehendService _comprehendService;
    private readonly ILlmService _llmService;
    private readonly IRekognitionService? _rekognitionService;
    private readonly IConfiguration _configuration;
    private readonly string _s3Bucket;

    private readonly string? _accessKeyId;
    private readonly string? _secretAccessKey;

    public DocumentExtractionOrchestrator(
        IDocumentUploadService uploadService,
        ITextractService textractService,
        IComprehendService comprehendService,
        ILlmService llmService,
        IRekognitionService rekognitionService,
        IConfiguration configuration)
    {
        _uploadService = uploadService;
        _textractService = textractService;
        _comprehendService = comprehendService;
        _llmService = llmService;
        _rekognitionService = rekognitionService;
        _configuration = configuration;
        _s3Bucket = configuration["AWS:S3:DocumentBucket"] ?? throw new InvalidOperationException("AWS:S3:DocumentBucket not configured");

        _accessKeyId = configuration?["AWS:AccessKeyId"] ?? string.Empty;
        _secretAccessKey = configuration?["AWS:SecretAccessKey"] ?? string.Empty;
    }

    // Overload that accepts DocumentUploadResult to avoid S3 lookups
    public async Task<ClaimExtractionResult> ExtractClaimDataAsync(DocumentUploadResult uploadResult, DocumentType documentType)
    {
        Console.WriteLine($"[Orchestrator] Starting extraction for document: {uploadResult.DocumentId}, type: {documentType}");
        Console.WriteLine($"[Orchestrator] Using S3 key from upload result: {uploadResult.S3Key}");
        
        try
        {
            var s3Key = uploadResult.S3Key;
            var documentId = uploadResult.DocumentId;
            
            // Step 2: Extract text using Textract
            Console.WriteLine($"[Orchestrator] Step 1: Extracting text with Textract");
            TextractResult textractResult;
            
            if (documentType == DocumentType.ClaimForm || documentType == DocumentType.PoliceReport)
            {
                // Use form/table analysis for structured documents
                textractResult = await _textractService.AnalyzeDocumentAsync(_s3Bucket, s3Key, new[] { "FORMS", "TABLES" });
            }
            else
            {
                // Use simple text detection for other types
                textractResult = await _textractService.DetectDocumentTextAsync(_s3Bucket, s3Key);
            }
            
            // Step 3: Extract entities using Comprehend
            Console.WriteLine($"[Orchestrator] Step 2: Extracting entities with Comprehend");
            var entities = await _comprehendService.DetectEntitiesAsync(textractResult.ExtractedText);
            var claimFields = await _comprehendService.ExtractClaimFieldsAsync(textractResult.ExtractedText);
            
            // Step 4: Analyze images if present (for damage photos)
            List<ImageAnalysisResult>? imageAnalysis = null;
            if (documentType == DocumentType.DamagePhotos && _rekognitionService != null)
            {
                Console.WriteLine($"[Orchestrator] Step 3: Analyzing images with Rekognition");
                imageAnalysis = await _rekognitionService.AnalyzeImagesAsync(_s3Bucket, new List<string> { s3Key });
            }
            
            // Step 5: Use Bedrock Claude to intelligently synthesize all data
            Console.WriteLine($"[Orchestrator] Step 4: Synthesizing data with Bedrock Claude");
            var extractedClaim = await SynthesizeClaimDataAsync(textractResult, entities, claimFields, imageAnalysis, documentType);
            
            // Step 6: Validate and score confidence
            Console.WriteLine($"[Orchestrator] Step 5: Validating extracted data");
            var validationResult = ValidateExtractedData(extractedClaim, textractResult.Confidence, textractResult.ExtractedText);
            
            Console.WriteLine($"[Orchestrator] Extraction complete. Overall confidence: {validationResult.OverallConfidence:F2}");
            
            return validationResult;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Orchestrator] Error during extraction: {ex.Message}");
            throw;
        }
    }

    // Legacy method - kept for backward compatibility but uses S3 lookup
    public async Task<ClaimExtractionResult> ExtractClaimDataAsync(string documentId, DocumentType documentType)
    {
        Console.WriteLine($"[Orchestrator] Starting extraction for document: {documentId}, type: {documentType}");
        
        try
        {
            // Get document metadata
            var uploadResult = await _uploadService.GetDocumentAsync(documentId);
            
            // Use the overload that takes DocumentUploadResult
            return await ExtractClaimDataAsync(uploadResult, documentType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Orchestrator] Error during extraction: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// NEW METHOD: Extract raw text content from supporting documents
    /// This is for extracting CONTENT, not claim data
    /// </summary>
    public async Task<string> ExtractDocumentContentAsync(string documentId)
    {
        Console.WriteLine($"[Orchestrator] Extracting content from supporting document: {documentId}");
        
        try
        {
            // Step 1: Get document metadata from upload service
            var uploadResult = await _uploadService.GetDocumentAsync(documentId);
            Console.WriteLine($"[Orchestrator] Retrieved document: {uploadResult.BlobName ?? uploadResult.S3Key}");
            
            // Step 2: Extract text using Textract/Document Intelligence
            TextractResult textractResult;
            var storageKey = uploadResult.BlobName ?? uploadResult.S3Key ?? throw new InvalidOperationException("No storage key found");
            var storageLocation = uploadResult.ContainerName ?? uploadResult.S3Bucket ?? _s3Bucket;
            
            // Use simple text detection - we just want the content, not structured forms
            textractResult = await _textractService.DetectDocumentTextAsync(storageLocation, storageKey);
            
            Console.WriteLine($"[Orchestrator] Extracted {textractResult.ExtractedText.Length} characters from {documentId}");
            
            return textractResult.ExtractedText;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Orchestrator] Error extracting document content: {ex.Message}");
            throw new InvalidOperationException($"Failed to extract content from document {documentId}: {ex.Message}", ex);
        }
    }

    // Legacy method - kept for backward compatibility but uses Blob/S3 lookup
    private async Task<ClaimExtractionResult> ExtractClaimDataAsyncLegacy(string documentId, DocumentType documentType)
    {
        Console.WriteLine($"[Orchestrator] Starting extraction for document: {documentId}, type: {documentType}");
        Console.WriteLine($"[Orchestrator] WARNING: Using legacy method with storage lookup - consider passing DocumentUploadResult instead");
        
        try
        {
            // Step 1: Verify document exists
            var exists = await _uploadService.ExistsAsync(documentId);
            if (!exists)
            {
                throw new FileNotFoundException($"Document {documentId} not found in storage");
            }
            
            // Get blob/S3 key for the document
            var uploadResult = await _uploadService.GetDocumentAsync(documentId);
            var storageKey = uploadResult.BlobName ?? uploadResult.S3Key ?? throw new InvalidOperationException("No storage key found");
            var storageLocation = uploadResult.ContainerName ?? uploadResult.S3Bucket ?? _s3Bucket;
            
            // Step 2: Extract text using Textract
            Console.WriteLine($"[Orchestrator] Step 1: Extracting text with Textract");
            TextractResult textractResult;
            
            if (documentType == DocumentType.ClaimForm || documentType == DocumentType.PoliceReport)
            {
                // Use form/table analysis for structured documents
                textractResult = await _textractService.AnalyzeDocumentAsync(storageLocation, storageKey, new[] { "FORMS", "TABLES" });
            }
            else
            {
                // Use simple text detection for other types
                textractResult = await _textractService.DetectDocumentTextAsync(storageLocation, storageKey);
            }
            
            // Step 3: Extract entities using Comprehend
            Console.WriteLine($"[Orchestrator] Step 2: Extracting entities with Comprehend");
            var entities = await _comprehendService.DetectEntitiesAsync(textractResult.ExtractedText);
            var claimFields = await _comprehendService.ExtractClaimFieldsAsync(textractResult.ExtractedText);
            
            // Step 4: Analyze images if present (for damage photos)
            List<ImageAnalysisResult>? imageAnalysis = null;
            if (documentType == DocumentType.DamagePhotos && _rekognitionService != null)
            {
                Console.WriteLine($"[Orchestrator] Step 3: Analyzing images with Rekognition");
                imageAnalysis = await _rekognitionService.AnalyzeImagesAsync(storageLocation, new List<string> { storageKey });
            }
            
            // Step 5: Use Bedrock Claude to intelligently synthesize all data
            Console.WriteLine($"[Orchestrator] Step 4: Synthesizing data with Bedrock Claude");
            var extractedClaim = await SynthesizeClaimDataAsync(textractResult, entities, claimFields, imageAnalysis, documentType);
            
            // Step 6: Validate and score confidence
            Console.WriteLine($"[Orchestrator] Step 5: Validating extracted data");
            var validationResult = ValidateExtractedData(extractedClaim, textractResult.Confidence, textractResult.ExtractedText);
            
            Console.WriteLine($"[Orchestrator] Extraction complete. Overall confidence: {validationResult.OverallConfidence:F2}");
            
            return validationResult;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Orchestrator] Error during extraction: {ex.Message}");
            throw;
        }
    }

    public async Task<ClaimExtractionResult> ExtractFromMultipleDocumentsAsync(List<string> documentIds, DocumentType documentType)
    {
        Console.WriteLine($"[Orchestrator] Starting multi-document extraction for {documentIds.Count} documents");
        
        var allTextractResults = new List<TextractResult>();
        var allEntities = new List<ComprehendEntity>();
        var allImageAnalysis = new List<ImageAnalysisResult>();
        var allClaimFields = new Dictionary<string, string>();
        
        foreach (var documentId in documentIds)
        {
            try
            {
                var s3Key = await GetS3KeyForDocument(documentId);
                
                // Extract from each document
                var textractResult = await _textractService.AnalyzeDocumentAsync(_s3Bucket, s3Key, new[] { "FORMS", "TABLES" });
                allTextractResults.Add(textractResult);
                
                var entities = await _comprehendService.DetectEntitiesAsync(textractResult.ExtractedText);
                allEntities.AddRange(entities);
                
                var fields = await _comprehendService.ExtractClaimFieldsAsync(textractResult.ExtractedText);
                foreach (var field in fields)
                {
                    if (!allClaimFields.ContainsKey(field.Key))
                    {
                        allClaimFields[field.Key] = field.Value;
                    }
                }
                
                // Check if it's an image file
                if (s3Key.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                    s3Key.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                    s3Key.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    if (_rekognitionService != null)
                    {
                        var imageResult = await _rekognitionService.AnalyzeImageAsync(_s3Bucket, s3Key);
                        allImageAnalysis.Add(imageResult);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Orchestrator] Error processing document {documentId}: {ex.Message}");
                // Continue with other documents
            }
        }
        
        // Combine all text
        var combinedText = string.Join("\n\n", allTextractResults.Select(r => r.ExtractedText));
        var combinedTextractResult = new TextractResult(
            combinedText,
            allTextractResults.SelectMany(r => r.KeyValuePairs).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            allTextractResults.SelectMany(r => r.Tables).ToList(),
            allTextractResults.Average(r => r.Confidence)
        );
        
        // Synthesize combined data
        var extractedClaim = await SynthesizeClaimDataAsync(
            combinedTextractResult, 
            allEntities, 
            allClaimFields, 
            allImageAnalysis.Any() ? allImageAnalysis : null, 
            documentType);
        
        var validationResult = ValidateExtractedData(extractedClaim, combinedTextractResult.Confidence, combinedTextractResult.ExtractedText);
        
        Console.WriteLine($"[Orchestrator] Multi-document extraction complete. Overall confidence: {validationResult.OverallConfidence:F2}");
        
        return validationResult;
    }

    private async Task<ClaimRequest> SynthesizeClaimDataAsync(
        TextractResult textractResult,
        List<ComprehendEntity> entities,
        Dictionary<string, string> claimFields,
        List<ImageAnalysisResult>? imageAnalysis,
        DocumentType documentType)
    {
        var prompt = BuildExtractionPrompt(textractResult, entities, claimFields, imageAnalysis, documentType);
        
        // Enhanced system prompt for better extraction
        var systemPrompt = @"You are an expert insurance claims data extraction system.

EXTRACTION RULES:
1. Look for policy numbers in formats like: POL-YYYY-NNNNN, POLICY#, Policy No., etc.
2. Extract claim amounts from currency values (look for $, USD, dollar amounts)
3. Identify policy type from context (Health, Life, Home, Motor, Auto, etc.)
4. Extract claimant/policyholder names from Person entities
5. Find dates of loss/incident from Date entities
6. Generate a concise claim description summarizing the incident
7. If a field is not found, use null (not empty string or UNKNOWN)

OUTPUT FORMAT (REQUIRED JSON STRUCTURE):
{
  PolicyNumber: exact policy number from document or null,
  PolicyholderName: persons full name or null,
  PolicyType: Health or Life or Home or Motor or Auto or null,
  ClaimType: specific claim type like Hospitalization or Accident etc. or null,
  ClaimAmount: numeric value without currency symbols (0 if not found),
  ClaimDescription: brief description of the claim,
  ClaimDate: YYYY-MM-DD format or null
}

Return ONLY valid JSON with no additional text.";
        
        try
        {
            var response = await CallBedrockForExtractionAsync(prompt, systemPrompt);
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Orchestrator] Error in LLM synthesis: {ex.Message}");
            
            // Fallback: construct claim from extracted fields
            return new ClaimRequest(
                PolicyNumber: claimFields.GetValueOrDefault("policyNumber", "UNKNOWN"),
                ClaimDescription: textractResult.ExtractedText.Length > 500 
                    ? textractResult.ExtractedText.Substring(0, 500) 
                    : textractResult.ExtractedText,
                ClaimAmount: decimal.TryParse(claimFields.GetValueOrDefault("claimAmount", "0"), out var amt) ? amt : 0,
                PolicyType: claimFields.GetValueOrDefault("policyType", "Life")
            );
        }
    }

    private async Task<ClaimRequest> CallBedrockForExtractionAsync(string prompt, string systemPrompt)
    {
        var maxRetries = int.Parse(_configuration["LLMExtraction:MaxRetries"] ?? "2");
        var retryDelaySeconds = double.Parse(_configuration["LLMExtraction:RetryDelaySeconds"] ?? "1");
        
        Console.WriteLine($"[Orchestrator] Using LLM for intelligent extraction (max {maxRetries} retries)");
        
        Exception? lastException = null;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                Console.WriteLine($"[Orchestrator] Attempt {attempt}/{maxRetries}");
                
                // Call LLM with custom extraction prompt
                var jsonResponse = await _llmService.GenerateResponseAsync(systemPrompt, prompt);
                
                // CRITICAL: Log raw response for debugging
                Console.WriteLine($"[Orchestrator] Raw LLM Response:");
                Console.WriteLine(jsonResponse.Length > 500 ? jsonResponse.Substring(0, 500) + "..." : jsonResponse);
                
                // Validate it's JSON
                var trimmed = jsonResponse.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || !trimmed.StartsWith("{"))
                {
                    throw new InvalidOperationException($"LLM returned non-JSON response: {trimmed.Substring(0, Math.Min(100, trimmed.Length))}");
                }
                
                // Parse JSON with error handling
                ClaimExtractionResponse? extractionResult;
                try
                {
                    extractionResult = JsonSerializer.Deserialize<ClaimExtractionResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    });
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"[Orchestrator] JSON parsing failed: {jsonEx.Message}");
                    Console.WriteLine($"[Orchestrator] Error at line {jsonEx.LineNumber}, position {jsonEx.BytePositionInLine}");
                    throw new InvalidOperationException($"Failed to parse LLM JSON response: {jsonEx.Message}", jsonEx);
                }
                
                if (extractionResult == null)
                {
                    throw new InvalidOperationException("Deserialization returned null");
                }
                
                // Validate extracted data
                var validationErrors = ValidateExtraction(extractionResult);
                if (validationErrors.Count > 0)
                {
                    Console.WriteLine($"[Orchestrator] Validation errors: {string.Join(", ", validationErrors)}");
                    
                    // If we have retries left and there are critical errors, retry
                    if (attempt < maxRetries && HasCriticalErrors(validationErrors))
                    {
                        Console.WriteLine($"[Orchestrator] Critical errors found, retrying...");
                        await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds * attempt));
                        continue;
                    }
                    
                    // Non-critical errors or last attempt - proceed with warnings
                    Console.WriteLine($"[Orchestrator] Proceeding with warnings");
                }
                
                // Success - log what we extracted
                Console.WriteLine($"[Orchestrator] ✓ Extraction successful:");
                Console.WriteLine($"  Policy: {MaskSensitiveData(extractionResult.PolicyNumber)}");
                Console.WriteLine($"  Amount: ${extractionResult.ClaimAmount:N2}");
                Console.WriteLine($"  Type: {extractionResult.PolicyType ?? "null"}");
                Console.WriteLine($"  Claimant: {(extractionResult.PolicyholderName != null ? "[present]" : "null")}");
                
                return new ClaimRequest(
                    PolicyNumber: extractionResult.PolicyNumber ?? "UNKNOWN",
                    ClaimDescription: extractionResult.ClaimDescription ?? "No description available",
                    ClaimAmount: extractionResult.ClaimAmount,
                    PolicyType: NormalizePolicyType(extractionResult.PolicyType)
                );
            }
            catch (Exception ex)
            {
                lastException = ex;
                Console.WriteLine($"[Orchestrator] Attempt {attempt} failed: {ex.GetType().Name}: {ex.Message}");
                
                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(retryDelaySeconds * attempt);
                    Console.WriteLine($"[Orchestrator] Waiting {delay.TotalSeconds}s before retry...");
                    await Task.Delay(delay);
                }
            }
        }
        
        // All retries failed - use intelligent fallback
        Console.WriteLine($"[Orchestrator] ⚠ LLM extraction failed after {maxRetries} attempts");
        Console.WriteLine($"[Orchestrator] Last error: {lastException?.Message}");
        Console.WriteLine($"[Orchestrator] Attempting fallback extraction from OCR/NER data...");
        
        return CreateFallbackExtraction(prompt);
    }
    
    private List<string> ValidateExtraction(ClaimExtractionResponse result)
    {
        var errors = new List<string>();
        var minPolicyLength = int.Parse(_configuration["LLMExtraction:MinPolicyNumberLength"] ?? "3");
        var maxClaimAmount = decimal.Parse(_configuration["LLMExtraction:MaxClaimAmount"] ?? "10000000");
        var minDescLength = int.Parse(_configuration["LLMExtraction:MinDescriptionLength"] ?? "10");
        
        if (string.IsNullOrWhiteSpace(result.PolicyNumber))
            errors.Add("PolicyNumber is missing");
        else if (result.PolicyNumber.Length < minPolicyLength)
            errors.Add($"PolicyNumber too short: {result.PolicyNumber}");
        
        if (result.ClaimAmount <= 0)
            errors.Add("ClaimAmount is zero or negative");
        else if (result.ClaimAmount > maxClaimAmount)
            errors.Add($"ClaimAmount suspiciously high: {result.ClaimAmount}");
        
        if (string.IsNullOrWhiteSpace(result.PolicyType))
            errors.Add("PolicyType is missing");
        
        if (string.IsNullOrWhiteSpace(result.ClaimDescription))
            errors.Add("ClaimDescription is missing");
        else if (result.ClaimDescription.Length < minDescLength)
            errors.Add("ClaimDescription too short");
        
        return errors;
    }
    
    private bool HasCriticalErrors(List<string> errors)
    {
        // Critical errors that should trigger retry
        return errors.Any(e => 
            e.Contains("PolicyNumber is missing") || 
            e.Contains("ClaimAmount is zero"));
    }
    
    private string NormalizePolicyType(string? policyType)
    {
        if (string.IsNullOrWhiteSpace(policyType)) return "Life";
        
        return policyType.ToLower() switch
        {
            "health" or "medical" or "healthcare" => "Health",
            "life" or "term" or "whole life" => "Life",
            "home" or "property" or "homeowners" => "Home",
            "motor" or "auto" or "vehicle" or "car" or "automobile" => "Motor",
            _ => char.ToUpper(policyType[0]) + policyType.Substring(1).ToLower()
        };
    }
    
    private string MaskSensitiveData(string? data)
    {
        if (string.IsNullOrWhiteSpace(data)) return "null";
        if (data.Length <= 4) return "***";
        return $"***{data.Substring(data.Length - 4)}";
    }
    
    private ClaimRequest CreateFallbackExtraction(string prompt)
    {
        // Extract from the prompt structure we built
        var policyNumber = ExtractFieldFromPrompt(prompt, "policyNumber");
        var claimAmountStr = ExtractFieldFromPrompt(prompt, "claimAmount");
        var policyType = ExtractFieldFromPrompt(prompt, "policyType");
        
        decimal claimAmount = 0;
        if (!string.IsNullOrEmpty(claimAmountStr))
        {
            decimal.TryParse(claimAmountStr.Replace(",", "").Replace("$", ""), out claimAmount);
        }
        
        // Extract description from document text
        var description = ExtractDescriptionFromPrompt(prompt);
        
        Console.WriteLine($"[Orchestrator] Fallback extraction results:");
        Console.WriteLine($"  Policy: {policyNumber ?? "not found"}");
        Console.WriteLine($"  Amount: {claimAmount}");
        Console.WriteLine($"  Type: {policyType ?? "not found"}");
        
        var hasValidData = !string.IsNullOrEmpty(policyNumber) || claimAmount > 0;
        
        return new ClaimRequest(
            PolicyNumber: policyNumber ?? "PLEASE_VERIFY",
            ClaimDescription: description ?? "Please review and complete the claim details from the uploaded document.",
            ClaimAmount: claimAmount,
            PolicyType: policyType ?? "Life"
        );
    }
    
    private string? ExtractFieldFromPrompt(string prompt, string fieldName)
    {
        // Look for pattern like "policyNumber: POL-123" in the EXTRACTED CLAIM FIELDS section
        var pattern = $@"{fieldName}:\s*([^\n]+)";
        var match = System.Text.RegularExpressions.Regex.Match(prompt, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            var value = match.Groups[1].Value.Trim();
            // Filter out "null" string values
            if (value != "null" && !string.IsNullOrWhiteSpace(value))
                return value;
        }
        
        return null;
    }
    
    private string? ExtractDescriptionFromPrompt(string prompt)
    {
        // Extract from DOCUMENT TEXT section
        var match = System.Text.RegularExpressions.Regex.Match(
            prompt,
            @"=== DOCUMENT TEXT ===\s*([\s\S]+?)(?:===|$)",
            System.Text.RegularExpressions.RegexOptions.Singleline
        );
        
        if (match.Success)
        {
            var text = match.Groups[1].Value.Trim();
            // Remove "..." if text was truncated
            text = text.TrimEnd('.').Trim();
            return text.Length > 500 ? text.Substring(0, 500) : text;
        }
        
        return null;
    }
    
    // Helper class for JSON deserialization
    internal class ClaimExtractionResponse
    {
        public string? PolicyNumber { get; set; }
        public string? PolicyholderName { get; set; }
        public string? PolicyType { get; set; }
        public string? ClaimType { get; set; }
        public decimal ClaimAmount { get; set; }
        public string? ClaimDescription { get; set; }
        public DateTime? ClaimDate { get; set; }
    }

    private string BuildExtractionPrompt(
        TextractResult textractResult,
        List<ComprehendEntity> entities,
        Dictionary<string, string> claimFields,
        List<ImageAnalysisResult>? imageAnalysis,
        DocumentType documentType)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine("Extract claim information from the following data:");
        prompt.AppendLine();
        
        // Add Textract extracted text
        prompt.AppendLine("=== DOCUMENT TEXT ===");
        prompt.AppendLine(textractResult.ExtractedText.Length > 2000 
            ? textractResult.ExtractedText.Substring(0, 2000) + "..." 
            : textractResult.ExtractedText);
        prompt.AppendLine();
        
        // Add form fields if available
        if (textractResult.KeyValuePairs.Any())
        {
            prompt.AppendLine("=== FORM FIELDS ===");
            foreach (var kvp in textractResult.KeyValuePairs.Take(20))
            {
                prompt.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            prompt.AppendLine();
        }
        
        // Add Comprehend extracted fields
        if (claimFields.Any())
        {
            prompt.AppendLine("=== EXTRACTED CLAIM FIELDS ===");
            prompt.AppendLine($"policyNumber: {claimFields.GetValueOrDefault("policyNumber", "null")}");
            prompt.AppendLine($"claimAmount: {claimFields.GetValueOrDefault("claimAmount", "null")}");
            prompt.AppendLine($"policyType: {claimFields.GetValueOrDefault("policyType", "null")}");
            prompt.AppendLine($"dateOfLoss: {claimFields.GetValueOrDefault("dateOfLoss", "null")}");
            prompt.AppendLine($"location: {claimFields.GetValueOrDefault("location", "null")}");
            prompt.AppendLine();
        }
        
        // Add Comprehend entities
        if (entities.Any())
        {
            prompt.AppendLine("=== IDENTIFIED ENTITIES ===");
            foreach (var entity in entities.Take(15))
            {
                prompt.AppendLine($"{entity.Type}: {entity.Text} (confidence: {entity.Score:F2})");
            }
            prompt.AppendLine();
        }
        
        // Add image analysis if available
        if (imageAnalysis?.Any() == true)
        {
            prompt.AppendLine("=== DAMAGE PHOTO ANALYSIS ===");
            foreach (var img in imageAnalysis)
            {
                prompt.AppendLine($"Image {img.ImageId}:");
                prompt.AppendLine($"  Detected objects: {string.Join(", ", img.Labels.Take(10))}");
                prompt.AppendLine($"  Damage type: {img.DamageType} (confidence: {img.Confidence:F2})");
            }
            prompt.AppendLine();
        }
        
        return prompt.ToString();
    }

    private ClaimExtractionResult ValidateExtractedData(ClaimRequest extractedClaim, float textractConfidence, string? extractedText = null)
    {
        var fieldConfidences = new Dictionary<string, float>();
        var ambiguousFields = new List<string>();
        
        // Validate policy number format
        if (string.IsNullOrWhiteSpace(extractedClaim.PolicyNumber) || extractedClaim.PolicyNumber == "UNKNOWN")
        {
            fieldConfidences["policyNumber"] = 0.3f;
            ambiguousFields.Add("policyNumber");
        }
        else if (System.Text.RegularExpressions.Regex.IsMatch(extractedClaim.PolicyNumber, @"^POL-\d{4,}-\d+$"))
        {
            fieldConfidences["policyNumber"] = 0.95f;
        }
        else
        {
            fieldConfidences["policyNumber"] = 0.7f;
        }
        
        // Validate claim amount
        if (extractedClaim.ClaimAmount <= 0)
        {
            fieldConfidences["claimAmount"] = 0.3f;
            ambiguousFields.Add("claimAmount");
        }
        else if (extractedClaim.ClaimAmount > 1000000)
        {
            fieldConfidences["claimAmount"] = 0.6f; // Very high amounts are suspicious
        }
        else
        {
            fieldConfidences["claimAmount"] = 0.9f;
        }
        
        // Validate policy type
        var validTypes = new[] { "Health", "Life" };
        if (validTypes.Contains(extractedClaim.PolicyType))
        {
            fieldConfidences["policyType"] = 0.95f;
        }
        else
        {
            fieldConfidences["policyType"] = 0.5f;
            ambiguousFields.Add("policyType");
        }
        
        // Validate claim description
        if (string.IsNullOrWhiteSpace(extractedClaim.ClaimDescription) || extractedClaim.ClaimDescription.Length < 20)
        {
            fieldConfidences["claimDescription"] = 0.4f;
            ambiguousFields.Add("claimDescription");
        }
        else
        {
            fieldConfidences["claimDescription"] = 0.85f;
        }
        
        // Calculate overall confidence (weighted average)
        var overallConfidence = (
            fieldConfidences.GetValueOrDefault("policyNumber", 0.5f) * 0.3f +
            fieldConfidences.GetValueOrDefault("claimAmount", 0.5f) * 0.3f +
            fieldConfidences.GetValueOrDefault("policyType", 0.5f) * 0.2f +
            fieldConfidences.GetValueOrDefault("claimDescription", 0.5f) * 0.2f
        );
        
        // Factor in Textract confidence
        overallConfidence = (overallConfidence + textractConfidence / 100f) / 2f;
        
        // Penalize for ambiguous fields
        if (ambiguousFields.Count > 0)
        {
            overallConfidence *= (1f - (ambiguousFields.Count * 0.1f));
        }
        
        return new ClaimExtractionResult(
            ExtractedClaim: extractedClaim,
            OverallConfidence: Math.Max(0, Math.Min(1, overallConfidence)),
            FieldConfidences: fieldConfidences,
            AmbiguousFields: ambiguousFields,
            RawExtractedData: new Dictionary<string, object>
            {
                ["textractConfidence"] = textractConfidence,
                ["extractedText"] = extractedText ?? extractedClaim.ClaimDescription
            }
        );
    }

    private async Task<string> GetS3KeyForDocument(string documentId)
    {
        // Search S3 for the document by listing objects that contain the documentId
        // The upload service stores documents with pattern: uploads/{userId}/{documentId}/{fileName}
        var uploadPrefix = _configuration["AWS:S3:UploadPrefix"] ?? "uploads/";
        
        try
        {
            var s3Client = new AmazonS3Client(
                 new Amazon.Runtime.BasicAWSCredentials(
                    _accessKeyId,
                     _secretAccessKey
                 ),
                Amazon.RegionEndpoint.GetBySystemName(_configuration["AWS:Region"] ?? "us-east-1")
            );
            
            var listRequest = new Amazon.S3.Model.ListObjectsV2Request
            {
                BucketName = _s3Bucket,
                Prefix = uploadPrefix
            };
            
            var listResponse = await s3Client.ListObjectsV2Async(listRequest);
            var matchingObject = listResponse.S3Objects.FirstOrDefault(obj => obj.Key.Contains(documentId));
            
            if (matchingObject == null)
            {
                throw new FileNotFoundException($"Document {documentId} not found in S3 bucket {_s3Bucket}");
            }
            
            Console.WriteLine($"[Orchestrator] Found S3 key for document {documentId}: {matchingObject.Key}");
            return matchingObject.Key;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Orchestrator] Error finding S3 key for document {documentId}: {ex.Message}");
            throw;
        }
    }
}
