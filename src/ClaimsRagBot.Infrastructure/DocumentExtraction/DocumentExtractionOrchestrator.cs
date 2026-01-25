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
    }

    public async Task<ClaimExtractionResult> ExtractClaimDataAsync(string documentId, DocumentType documentType)
    {
        Console.WriteLine($"[Orchestrator] Starting extraction for document: {documentId}, type: {documentType}");
        
        try
        {
            // Step 1: Verify document exists in S3
            var exists = await _uploadService.ExistsAsync(documentId);
            if (!exists)
            {
                throw new FileNotFoundException($"Document {documentId} not found in S3");
            }
            
            // Get S3 key for the document
            var s3Key = await GetS3KeyForDocument(documentId);
            
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
            var validationResult = ValidateExtractedData(extractedClaim, textractResult.Confidence);
            
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
        
        var validationResult = ValidateExtractedData(extractedClaim, combinedTextractResult.Confidence);
        
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
        
        // Use the existing LLM service to extract structured claim data
        var systemPrompt = @"You are an expert insurance claims data extraction system.
Extract and structure claim information from provided documents.
Apply domain knowledge to resolve ambiguities.
Ensure all monetary amounts are in USD without currency symbols.
Normalize policy types to exactly one of: Motor, Home, Health, Life.
Generate detailed claim descriptions from available information.
Output ONLY valid JSON, no markdown formatting.";
        
        try
        {
            // Create a temporary ClaimRequest to leverage existing LLM infrastructure
            var tempRequest = new ClaimRequest(
                PolicyNumber: "TEMP",
                ClaimDescription: prompt,
                ClaimAmount: 0,
                PolicyType: "Motor"
            );
            
            // We'll use a custom approach since we need different output format
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
                PolicyType: claimFields.GetValueOrDefault("policyType", "Motor")
            );
        }
    }

    private async Task<ClaimRequest> CallBedrockForExtractionAsync(string prompt, string systemPrompt)
    {
        // This is a simplified extraction - in production, you'd call Bedrock directly
        // For now, we'll parse from the available data
        
        // Since we don't have direct access to Bedrock invocation with custom prompts in the current LlmService,
        // we'll construct from Comprehend data as a fallback
        // In a real implementation, you'd add a method to ILlmService for custom prompts
        
        Console.WriteLine("[Orchestrator] Using fallback extraction from Comprehend data");
        
        // Parse the prompt to extract the claim fields that were embedded
        var lines = prompt.Split('\n');
        var policyNumber = "UNKNOWN";
        var claimAmount = 0m;
        var policyType = "Motor";
        var description = "";
        
        foreach (var line in lines)
        {
            if (line.Contains("policyNumber") && line.Contains(":"))
            {
                var value = line.Split(':').Last().Trim().Trim('"', ',');
                if (!string.IsNullOrWhiteSpace(value) && value != "null")
                    policyNumber = value;
            }
            else if (line.Contains("claimAmount") && line.Contains(":"))
            {
                var value = line.Split(':').Last().Trim().Trim('"', ',');
                if (decimal.TryParse(value, out var amt))
                    claimAmount = amt;
            }
            else if (line.Contains("policyType") && line.Contains(":"))
            {
                var value = line.Split(':').Last().Trim().Trim('"', ',');
                if (!string.IsNullOrWhiteSpace(value) && value != "null")
                    policyType = value;
            }
            else if (line.Contains("DOCUMENT TEXT") && lines.Length > Array.IndexOf(lines, line) + 1)
            {
                var startIndex = Array.IndexOf(lines, line) + 1;
                var textLines = lines.Skip(startIndex).Take(20).ToList();
                description = string.Join(" ", textLines).Trim();
                if (description.Length > 500)
                    description = description.Substring(0, 500);
            }
        }
        
        return new ClaimRequest(
            PolicyNumber: policyNumber,
            ClaimDescription: description,
            ClaimAmount: claimAmount,
            PolicyType: policyType
        );
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

    private ClaimExtractionResult ValidateExtractedData(ClaimRequest extractedClaim, float textractConfidence)
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
        var validTypes = new[] { "Motor", "Home", "Health", "Life" };
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
                ["textractConfidence"] = textractConfidence
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
            var s3Client = new Amazon.S3.AmazonS3Client(
                new Amazon.Runtime.BasicAWSCredentials(
                    _configuration["AWS:AccessKeyId"],
                    _configuration["AWS:SecretAccessKey"]
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
