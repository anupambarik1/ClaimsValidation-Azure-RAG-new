# Document-Based Claim Extraction - Technical Implementation Plan

## Executive Summary

This document outlines a comprehensive technical plan to enhance the Claims RAG Bot by adding **intelligent document processing** capabilities. Instead of manually entering claim details, users will upload documents (claim forms, police reports, repair estimates, photos) and the system will automatically extract structured data using AWS AI services.

---

## Current State vs. Proposed State

### Current Flow
```
User → Manual JSON Entry → API → RAG Pipeline → Decision
```

### Proposed Flow
```
User → Document Upload → AI Extraction → Validation → API → RAG Pipeline → Decision
```

---

## Solution Architecture

### High-Level Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                     User Interface Layer                          │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  Web UI / Mobile App / API Client                          │  │
│  │  - File upload widget (PDF, images, scanned docs)          │  │
│  │  - Preview extracted data                                  │  │
│  │  - Confirm/edit before submission                          │  │
│  └────────────────┬───────────────────────────────────────────┘  │
└───────────────────┼──────────────────────────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────────────────┐
│              NEW: Document Processing Layer                       │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  API Endpoint: POST /api/claims/extract                    │  │
│  │  Input: Multipart file upload (PDF, JPG, PNG)              │  │
│  │  Output: Structured ClaimRequest JSON                      │  │
│  └────────────────┬───────────────────────────────────────────┘  │
└───────────────────┼──────────────────────────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────────────────┐
│          NEW: Document Processing Orchestrator                    │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  1. Document Classification                               │    │
│  │  2. Text/Data Extraction                                  │    │
│  │  3. Entity Recognition & Structuring                      │    │
│  │  4. Validation & Confidence Scoring                       │    │
│  └────┬──────────┬─────────┬──────────┬─────────────────────┘    │
└───────┼──────────┼─────────┼──────────┼──────────────────────────┘
        │          │         │          │
        ▼          ▼         ▼          ▼
┌───────────┐ ┌────────┐ ┌──────┐ ┌─────────┐
│  Amazon   │ │Amazon  │ │Amazon│ │ Amazon  │
│ Textract  │ │Bedrock │ │Compre│ │   S3    │
│           │ │        │ │-hend │ │         │
└───────────┘ └────────┘ └──────┘ └─────────┘
        │          │         │          │
        └──────────┴─────────┴──────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────────────────┐
│             Extracted & Validated ClaimRequest                    │
│  {                                                                │
│    "policyNumber": "POL-12345",                                  │
│    "claimDescription": "Car accident - front bumper damage",     │
│    "claimAmount": 2500,                                          │
│    "policyType": "Motor",                                        │
│    "confidence": 0.92,                                           │
│    "extractedFields": {...}                                      │
│  }                                                                │
└────────────────────┬─────────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────────┐
│          EXISTING: Claims Validation Pipeline                     │
│          (No changes to existing RAG flow)                        │
└──────────────────────────────────────────────────────────────────┘
```

---

## AWS Services Selection & Rationale

### 1. **Amazon S3** - Document Storage
**Purpose:** Temporary and permanent storage of uploaded documents

**Usage:**
- Store original uploaded documents
- Archive processed documents for audit trail
- Pre-signed URLs for secure uploads from frontend

**Configuration:**
```json
{
  "Bucket": "claims-documents-{account-id}",
  "Lifecycle": {
    "RawUploads": "30 days retention",
    "ProcessedDocs": "7 years (compliance)"
  },
  "Encryption": "SSE-S3 or SSE-KMS",
  "Versioning": "Enabled"
}
```

**Cost:** ~$0.023/GB/month + request costs

---

### 2. **Amazon Textract** - OCR & Document Analysis
**Purpose:** Extract text and structured data from documents

**Capabilities:**
- ✅ **Text Detection:** Extract all text from scanned documents/photos
- ✅ **Form/Table Extraction:** Identify key-value pairs (e.g., "Policy Number: POL-12345")
- ✅ **Handwriting Recognition:** Process handwritten claim forms
- ✅ **Layout Analysis:** Understand document structure
- ✅ **Multi-page PDFs:** Process entire claim packets

**API Calls:**
```csharp
// For simple documents (single page, images)
DetectDocumentText - Async analysis

// For complex forms with tables
AnalyzeDocument - with FeatureTypes: ["FORMS", "TABLES"]

// For insurance claim forms specifically
AnalyzeExpense - Optimized for forms with line items
```

**Use Cases:**
- Scanned claim forms (PDF)
- Photos of accident scenes with damage descriptions
- Repair estimate documents
- Police reports
- Insurance cards

**Cost:** 
- $1.50 per 1,000 pages (DetectDocumentText)
- $50 per 1,000 pages (AnalyzeDocument with FORMS/TABLES)

---

### 3. **Amazon Comprehend** - Natural Language Processing
**Purpose:** Extract entities and classify text

**Capabilities:**
- ✅ **Entity Recognition:** Identify policy numbers, amounts, dates, locations
- ✅ **Custom Entity Recognition:** Train on insurance-specific entities
- ✅ **Sentiment Analysis:** Detect fraudulent or suspicious language patterns
- ✅ **Key Phrase Extraction:** Identify claim circumstances

**Custom Entity Types to Train:**
```
- POLICY_NUMBER (pattern: POL-XXXXX)
- CLAIM_AMOUNT (monetary values)
- POLICY_TYPE (Motor, Home, Health, Life)
- DAMAGE_TYPE (Collision, Theft, Fire, etc.)
- VEHICLE_INFO (Make, Model, VIN)
- DATE_OF_LOSS
- LOCATION
```

**API Usage:**
```csharp
var request = new DetectEntitiesRequest
{
    Text = extractedText,
    LanguageCode = "en",
    EndpointArn = "arn:aws:comprehend:us-east-1:xxx:entity-recognizer-endpoint/claims"
};
```

**Cost:** 
- $0.0001 per unit (100 chars)
- Custom entity recognizer: $3/hour training + $0.50/hour inference

---

### 4. **Amazon Bedrock (Claude)** - Intelligent Extraction & Reasoning
**Purpose:** Advanced document understanding and structured data extraction

**Why Bedrock for Extraction:**
- ✅ **Contextual Understanding:** Claude can understand insurance domain terminology
- ✅ **Multi-document Synthesis:** Combine info from multiple uploaded documents
- ✅ **Ambiguity Resolution:** Handle unclear or contradictory information
- ✅ **Structured Output:** Generate properly formatted JSON
- ✅ **Validation Logic:** Apply business rules during extraction

**Prompt Strategy:**
```
System: You are an insurance claims data extraction specialist.

User: Extract claim information from this document:

[TEXTRACT_OUTPUT]

Required fields:
1. policyNumber (format: POL-XXXXX)
2. claimDescription (detailed incident description)
3. claimAmount (numeric value in dollars)
4. policyType (Motor, Home, Health, or Life)

Additional context from images:
- Damage photos: [REKOGNITION_LABELS]

Respond in JSON:
{
  "policyNumber": "...",
  "claimDescription": "...",
  "claimAmount": 0,
  "policyType": "...",
  "confidence": 0.0-1.0,
  "extractedFrom": ["claim_form", "police_report"],
  "ambiguousFields": [],
  "additionalInfo": {}
}
```

**Cost:** Same as existing LLM usage (~$3 per 1M input tokens)

---

### 5. **Amazon Rekognition** - Image Analysis (Optional Enhancement)
**Purpose:** Analyze damage photos uploaded with claims

**Capabilities:**
- ✅ **Object Detection:** Identify vehicle parts (bumper, door, windshield)
- ✅ **Damage Assessment:** Detect visible damage in photos
- ✅ **Custom Labels:** Train on vehicle damage types
- ✅ **Scene Detection:** Verify accident scene authenticity

**Use Cases:**
- Validate damage photos match claim description
- Detect potential fraud (stock photos, unrelated images)
- Estimate damage severity
- Extract vehicle details (make, model, color)

**API Usage:**
```csharp
var request = new DetectLabelsRequest
{
    Image = new Image { S3Object = new S3Object { Bucket = "...", Name = "..." } },
    MaxLabels = 20,
    MinConfidence = 70F
};

// Custom model for damage detection
var customRequest = new DetectCustomLabelsRequest
{
    ProjectVersionArn = "arn:aws:rekognition:us-east-1:xxx:project/vehicle-damage/version/1",
    Image = image
};
```

**Cost:** $1 per 1,000 images (standard labels), $4 per 1,000 (custom labels)

---

## Detailed Component Design

### Component 1: Document Upload Service

**Responsibility:** Handle file uploads securely

**Implementation:**

```csharp
// New service interface
public interface IDocumentUploadService
{
    Task<DocumentUploadResult> UploadAsync(IFormFile file, string userId);
    Task<Stream> DownloadAsync(string documentId);
    Task DeleteAsync(string documentId);
}

// DTO
public record DocumentUploadResult(
    string DocumentId,
    string S3Key,
    string ContentType,
    long FileSize,
    DateTime UploadedAt
);
```

**API Endpoint:**
```csharp
[HttpPost("upload")]
[RequestSizeLimit(10_485_760)] // 10MB limit
public async Task<ActionResult<DocumentUploadResult>> UploadDocument(
    IFormFile file)
{
    // Validate file type
    var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png" };
    if (!allowedTypes.Contains(file.ContentType))
        return BadRequest("Invalid file type");
    
    // Upload to S3
    var result = await _uploadService.UploadAsync(file, User.Identity.Name);
    
    return Ok(result);
}
```

**S3 Configuration:**
- Bucket: `claims-documents-{environment}`
- Folder structure: `uploads/{userId}/{documentId}/`
- Pre-signed URLs for direct browser upload (alternative approach)
- Server-side encryption enabled
- Lifecycle policy: Move to Glacier after 90 days

---

### Component 2: Document Extraction Orchestrator

**Responsibility:** Coordinate extraction across multiple AWS services

**Implementation:**

```csharp
public interface IDocumentExtractionService
{
    Task<ClaimExtractionResult> ExtractClaimDataAsync(
        string documentId, 
        DocumentType documentType);
}

public record ClaimExtractionResult(
    ClaimRequest ExtractedClaim,
    float OverallConfidence,
    Dictionary<string, float> FieldConfidences,
    List<string> AmbiguousFields,
    Dictionary<string, object> RawExtractedData
);

public enum DocumentType
{
    ClaimForm,           // Standard insurance form
    PoliceReport,        // Accident report
    RepairEstimate,      // Mechanic's estimate
    DamagePhotos,        // Images of damage
    MedicalRecords,      // For injury claims
    Mixed                // Multiple document types
}
```

**Orchestration Flow:**

```csharp
public class DocumentExtractionOrchestrator : IDocumentExtractionService
{
    private readonly ITextractService _textract;
    private readonly IComprehendService _comprehend;
    private readonly IBedrockService _bedrock;
    private readonly IRekognitionService _rekognition;
    
    public async Task<ClaimExtractionResult> ExtractClaimDataAsync(
        string documentId, 
        DocumentType docType)
    {
        // Step 1: Classify document type (if Mixed)
        var classification = await ClassifyDocumentAsync(documentId);
        
        // Step 2: Extract text using Textract
        var textractResult = await _textract.AnalyzeDocumentAsync(
            documentId, 
            new[] { "FORMS", "TABLES" }
        );
        
        // Step 3: Extract entities using Comprehend
        var entities = await _comprehend.DetectEntitiesAsync(
            textractResult.ExtractedText
        );
        
        // Step 4: If images present, analyze with Rekognition
        List<ImageAnalysisResult> imageAnalysis = null;
        if (HasImages(documentId))
        {
            imageAnalysis = await _rekognition.AnalyzeImagesAsync(documentId);
        }
        
        // Step 5: Use Bedrock Claude to intelligently combine all data
        var claimData = await _bedrock.ExtractStructuredClaimAsync(
            textractResult,
            entities,
            imageAnalysis,
            docType
        );
        
        // Step 6: Validate and score confidence
        var validated = await ValidateExtractedData(claimData);
        
        return validated;
    }
}
```

---

### Component 3: Textract Integration Service

**Responsibility:** OCR and form extraction

**Implementation:**

```csharp
public interface ITextractService
{
    Task<TextractResult> AnalyzeDocumentAsync(
        string documentId, 
        string[] featureTypes);
}

public record TextractResult(
    string ExtractedText,
    Dictionary<string, string> KeyValuePairs,  // Form fields
    List<TableData> Tables,
    float Confidence
);

public class TextractService : ITextractService
{
    private readonly AmazonTextractClient _client;
    private readonly IS3Service _s3;
    
    public async Task<TextractResult> AnalyzeDocumentAsync(
        string documentId,
        string[] featureTypes)
    {
        var s3Object = await _s3.GetObjectLocationAsync(documentId);
        
        // Start async job for multi-page documents
        var startRequest = new StartDocumentAnalysisRequest
        {
            DocumentLocation = new DocumentLocation
            {
                S3Object = new Amazon.Textract.Model.S3Object
                {
                    Bucket = s3Object.Bucket,
                    Name = s3Object.Key
                }
            },
            FeatureTypes = featureTypes.ToList()
        };
        
        var startResponse = await _client.StartDocumentAnalysisAsync(startRequest);
        
        // Poll for completion
        var jobId = startResponse.JobId;
        GetDocumentAnalysisResponse result = null;
        
        while (true)
        {
            var getRequest = new GetDocumentAnalysisRequest { JobId = jobId };
            result = await _client.GetDocumentAnalysisAsync(getRequest);
            
            if (result.JobStatus == "SUCCEEDED")
                break;
            else if (result.JobStatus == "FAILED")
                throw new Exception($"Textract job failed: {result.StatusMessage}");
            
            await Task.Delay(5000); // Poll every 5 seconds
        }
        
        // Parse results
        return ParseTextractResponse(result);
    }
    
    private TextractResult ParseTextractResponse(GetDocumentAnalysisResponse response)
    {
        var text = new StringBuilder();
        var kvPairs = new Dictionary<string, string>();
        var tables = new List<TableData>();
        
        foreach (var block in response.Blocks)
        {
            if (block.BlockType == "LINE")
            {
                text.AppendLine(block.Text);
            }
            else if (block.BlockType == "KEY_VALUE_SET" && block.EntityTypes.Contains("KEY"))
            {
                var key = GetKeyText(block, response.Blocks);
                var value = GetValueText(block, response.Blocks);
                kvPairs[key] = value;
            }
            // Parse tables similarly...
        }
        
        return new TextractResult(
            ExtractedText: text.ToString(),
            KeyValuePairs: kvPairs,
            Tables: tables,
            Confidence: response.Blocks.Average(b => b.Confidence)
        );
    }
}
```

---

### Component 4: Comprehend Entity Recognition

**Responsibility:** Extract insurance-specific entities

**Custom Entity Training:**

**Training Data Format (CSV):**
```csv
Text,Entity
"Policy Number: POL-2024-12345",POLICY_NUMBER
"Claim amount of $2,500 for repairs",CLAIM_AMOUNT
"Motor insurance policy for vehicle damage",POLICY_TYPE
"Collision occurred on Highway 101",DAMAGE_TYPE
```

**Training Process:**
1. Prepare 1,000+ annotated examples
2. Upload to S3
3. Train custom entity recognizer via AWS Console or SDK
4. Deploy to inference endpoint

**Implementation:**

```csharp
public interface IComprehendService
{
    Task<List<Entity>> DetectEntitiesAsync(string text);
    Task<Dictionary<string, string>> ExtractClaimFieldsAsync(string text);
}

public class ComprehendService : IComprehendService
{
    private readonly AmazonComprehendClient _client;
    private readonly string _customEndpointArn;
    
    public async Task<Dictionary<string, string>> ExtractClaimFieldsAsync(string text)
    {
        var request = new DetectEntitiesRequest
        {
            Text = text,
            LanguageCode = "en",
            EndpointArn = _customEndpointArn // Custom insurance entity model
        };
        
        var response = await _client.DetectEntitiesAsync(request);
        
        var fields = new Dictionary<string, string>();
        
        foreach (var entity in response.Entities)
        {
            switch (entity.Type)
            {
                case "POLICY_NUMBER":
                    if (entity.Score > 0.8)
                        fields["policyNumber"] = entity.Text;
                    break;
                    
                case "CLAIM_AMOUNT":
                    if (entity.Score > 0.8)
                        fields["claimAmount"] = ExtractNumericValue(entity.Text);
                    break;
                    
                case "POLICY_TYPE":
                    if (entity.Score > 0.8)
                        fields["policyType"] = NormalizePolicyType(entity.Text);
                    break;
            }
        }
        
        return fields;
    }
    
    private string ExtractNumericValue(string text)
    {
        // Extract numeric value from "$2,500" -> "2500"
        var match = Regex.Match(text, @"[\d,]+\.?\d*");
        return match.Value.Replace(",", "");
    }
}
```

---

### Component 5: Bedrock Intelligent Extraction

**Responsibility:** Synthesize all extracted data into structured claim

**Prompt Engineering:**

```csharp
public class BedrockExtractionService
{
    private readonly ILlmService _llm;
    
    public async Task<ClaimRequest> ExtractStructuredClaimAsync(
        TextractResult textract,
        List<Entity> entities,
        List<ImageAnalysisResult> images,
        DocumentType docType)
    {
        var prompt = BuildExtractionPrompt(textract, entities, images, docType);
        
        var requestBody = new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 2048,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            system = @"You are an expert insurance claims data extraction system.
Extract and structure claim information from provided documents.
Apply domain knowledge to resolve ambiguities.
Ensure all monetary amounts are in USD.
Normalize policy types to: Motor, Home, Health, Life.
Generate detailed claim descriptions from available information."
        };
        
        var response = await _llm.InvokeModelAsync(requestBody);
        
        return ParseClaimFromResponse(response);
    }
    
    private string BuildExtractionPrompt(
        TextractResult textract,
        List<Entity> entities,
        List<ImageAnalysisResult> images,
        DocumentType docType)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine("Extract claim information from the following data:");
        prompt.AppendLine();
        
        // Add Textract extracted text
        prompt.AppendLine("=== DOCUMENT TEXT ===");
        prompt.AppendLine(textract.ExtractedText);
        prompt.AppendLine();
        
        // Add form fields if available
        if (textract.KeyValuePairs.Any())
        {
            prompt.AppendLine("=== FORM FIELDS ===");
            foreach (var kvp in textract.KeyValuePairs)
            {
                prompt.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            prompt.AppendLine();
        }
        
        // Add Comprehend entities
        prompt.AppendLine("=== IDENTIFIED ENTITIES ===");
        foreach (var entity in entities)
        {
            prompt.AppendLine($"{entity.Type}: {entity.Text} (confidence: {entity.Score:F2})");
        }
        prompt.AppendLine();
        
        // Add image analysis if available
        if (images?.Any() == true)
        {
            prompt.AppendLine("=== DAMAGE PHOTO ANALYSIS ===");
            foreach (var img in images)
            {
                prompt.AppendLine($"Image {img.ImageId}:");
                prompt.AppendLine($"  Detected objects: {string.Join(", ", img.Labels)}");
                prompt.AppendLine($"  Damage detected: {img.DamageType} (confidence: {img.Confidence:F2})");
            }
            prompt.AppendLine();
        }
        
        prompt.AppendLine("=== REQUIRED OUTPUT ===");
        prompt.AppendLine(@"
{
  ""policyNumber"": ""POL-XXXXX"",
  ""claimDescription"": ""Detailed description synthesized from all sources"",
  ""claimAmount"": 0,
  ""policyType"": ""Motor|Home|Health|Life"",
  ""confidence"": 0.0-1.0,
  ""extractedFrom"": [""source1"", ""source2""],
  ""fieldConfidence"": {
    ""policyNumber"": 0.0-1.0,
    ""claimDescription"": 0.0-1.0,
    ""claimAmount"": 0.0-1.0,
    ""policyType"": 0.0-1.0
  },
  ""ambiguousFields"": [""field names if uncertain""],
  ""additionalInfo"": {
    ""dateOfLoss"": ""YYYY-MM-DD"",
    ""location"": ""...""",
    ""vehicleInfo"": ""..."""
  }
}");
        
        return prompt.ToString();
    }
}
```

---

### Component 6: Validation & Confidence Scoring

**Responsibility:** Validate extracted data and calculate confidence scores

**Implementation:**

```csharp
public class ExtractionValidationService
{
    public async Task<ClaimExtractionResult> ValidateAsync(
        ClaimRequest extractedClaim,
        Dictionary<string, float> fieldConfidences)
    {
        var validationIssues = new List<string>();
        var ambiguousFields = new List<string>();
        
        // Validate policy number format
        if (!Regex.IsMatch(extractedClaim.PolicyNumber ?? "", @"^POL-\d{4,}-\d+$"))
        {
            if (fieldConfidences["policyNumber"] < 0.9)
            {
                ambiguousFields.Add("policyNumber");
                validationIssues.Add("Policy number format uncertain");
            }
        }
        
        // Validate claim amount
        if (extractedClaim.ClaimAmount <= 0)
        {
            ambiguousFields.Add("claimAmount");
            validationIssues.Add("Claim amount not found or invalid");
        }
        
        // Validate policy type
        var validTypes = new[] { "Motor", "Home", "Health", "Life" };
        if (!validTypes.Contains(extractedClaim.PolicyType))
        {
            ambiguousFields.Add("policyType");
            validationIssues.Add("Policy type unclear or not standard");
        }
        
        // Validate claim description completeness
        if (string.IsNullOrWhiteSpace(extractedClaim.ClaimDescription) ||
            extractedClaim.ClaimDescription.Length < 20)
        {
            ambiguousFields.Add("claimDescription");
            validationIssues.Add("Claim description too brief or missing");
        }
        
        // Calculate overall confidence
        var overallConfidence = fieldConfidences.Values.Average();
        
        // Adjust for validation issues
        if (validationIssues.Any())
        {
            overallConfidence *= 0.8f; // Penalize for issues
        }
        
        return new ClaimExtractionResult(
            ExtractedClaim: extractedClaim,
            OverallConfidence: overallConfidence,
            FieldConfidences: fieldConfidences,
            AmbiguousFields: ambiguousFields,
            RawExtractedData: new Dictionary<string, object>()
        );
    }
}
```

---

## API Design

### New Endpoints

#### 1. Upload Document
```http
POST /api/claims/documents/upload
Content-Type: multipart/form-data

Response:
{
  "documentId": "doc-uuid-12345",
  "s3Key": "uploads/user123/doc-uuid-12345/claim_form.pdf",
  "fileSize": 245678,
  "uploadedAt": "2026-01-25T10:30:00Z"
}
```

#### 2. Extract Claim Data from Document
```http
POST /api/claims/extract
Content-Type: application/json

{
  "documentId": "doc-uuid-12345",
  "documentType": "ClaimForm"
}

Response:
{
  "extractedClaim": {
    "policyNumber": "POL-2024-12345",
    "claimDescription": "Vehicle collision on Highway 101...",
    "claimAmount": 2500,
    "policyType": "Motor"
  },
  "confidence": 0.92,
  "fieldConfidences": {
    "policyNumber": 0.95,
    "claimDescription": 0.88,
    "claimAmount": 0.97,
    "policyType": 0.91
  },
  "ambiguousFields": [],
  "suggestedEdits": {
    "claimDescription": "Consider adding more detail about damage location"
  }
}
```

#### 3. Extract & Validate Combined (One-Step)
```http
POST /api/claims/submit-document
Content-Type: multipart/form-data

Files: claim_form.pdf, damage_photo1.jpg, damage_photo2.jpg

Response:
{
  "extractedClaim": { ... },
  "confidence": 0.89,
  "validationStatus": "ReadyForReview",
  "nextAction": "ReviewAndConfirm"
}
```

#### 4. User Review & Correction
```http
POST /api/claims/validate-extraction
Content-Type: application/json

{
  "documentId": "doc-uuid-12345",
  "extractedClaim": {
    "policyNumber": "POL-2024-12345",  // Corrected by user
    "claimDescription": "...",
    "claimAmount": 2600,  // Updated by user
    "policyType": "Motor"
  },
  "userConfirmed": true
}

Response:
{
  "claimDecision": { ... }  // Standard claim validation response
}
```

---

## Data Flow - Complete Example

### Scenario: User uploads a claim form PDF

**Step 1: Document Upload**
```
User → Frontend → POST /api/claims/documents/upload
Frontend → S3 (multipart upload)
Response: { documentId: "doc-123" }
```

**Step 2: Automatic Extraction Triggered**
```
Frontend → POST /api/claims/extract { documentId: "doc-123" }
API → DocumentExtractionOrchestrator
```

**Step 3: Parallel AWS Service Calls**
```
Orchestrator → Textract.AnalyzeDocument (S3: doc-123)
  ↓
Textract Response:
{
  "text": "Policy Number: POL-2024-12345\nClaim Amount: $2,500...",
  "formFields": {
    "Policy Number": "POL-2024-12345",
    "Claim Amount": "$2,500",
    "Type of Loss": "Collision"
  }
}

Orchestrator → Comprehend.DetectEntities (text)
  ↓
Comprehend Response:
{
  "entities": [
    { "type": "POLICY_NUMBER", "text": "POL-2024-12345", "score": 0.96 },
    { "type": "CLAIM_AMOUNT", "text": "$2,500", "score": 0.94 },
    { "type": "POLICY_TYPE", "text": "Motor", "score": 0.89 }
  ]
}
```

**Step 4: Intelligent Synthesis with Bedrock**
```
Orchestrator → Bedrock Claude

Prompt:
"Extract claim from:
TEXT: Policy Number: POL-2024-12345...
ENTITIES: POLICY_NUMBER=POL-2024-12345 (0.96)..."

Claude Response:
{
  "policyNumber": "POL-2024-12345",
  "claimDescription": "Motor vehicle collision resulting in front bumper damage...",
  "claimAmount": 2500,
  "policyType": "Motor",
  "confidence": 0.92
}
```

**Step 5: Validation**
```
Validator checks:
✓ Policy number format valid
✓ Claim amount > 0
✓ Policy type in allowed list
✓ Description > 20 chars

Final confidence: 0.92
Ambiguous fields: []
```

**Step 6: User Review**
```
Frontend displays:
┌──────────────────────────────────────┐
│ Extracted Claim Data (92% confident)│
├──────────────────────────────────────┤
│ Policy Number: POL-2024-12345  ✓     │
│ Claim Amount: $2,500           ✓     │
│ Policy Type: Motor             ✓     │
│ Description: [editable field]  ⚠️    │
├──────────────────────────────────────┤
│ [Edit] [Confirm & Submit]            │
└──────────────────────────────────────┘
```

**Step 7: Final Submission**
```
User clicks "Confirm & Submit"
Frontend → POST /api/claims/validate (existing endpoint)
Body: { policyNumber: "...", claimAmount: 2500, ... }

→ Existing RAG Pipeline → Decision
```

---

## Database Schema Changes

### New Table: DocumentExtractions

```sql
CREATE TABLE DocumentExtractions (
    ExtractionId UUID PRIMARY KEY,
    DocumentId UUID NOT NULL,
    UserId VARCHAR(100) NOT NULL,
    S3Bucket VARCHAR(255),
    S3Key VARCHAR(500),
    DocumentType VARCHAR(50),  -- ClaimForm, PoliceReport, etc.
    
    -- Extraction results
    ExtractedPolicyNumber VARCHAR(50),
    ExtractedClaimAmount DECIMAL(18,2),
    ExtractedPolicyType VARCHAR(50),
    ExtractedDescription TEXT,
    
    -- Confidence scores
    OverallConfidence FLOAT,
    PolicyNumberConfidence FLOAT,
    ClaimAmountConfidence FLOAT,
    PolicyTypeConfidence FLOAT,
    DescriptionConfidence FLOAT,
    
    -- Metadata
    TextractJobId VARCHAR(100),
    ProcessingStatus VARCHAR(50),  -- Pending, Processing, Completed, Failed
    ProcessingStarted TIMESTAMP,
    ProcessingCompleted TIMESTAMP,
    
    -- User review
    UserReviewed BOOLEAN DEFAULT FALSE,
    UserModified BOOLEAN DEFAULT FALSE,
    FinalClaimData JSONB,  -- After user corrections
    
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_document_extractions_user ON DocumentExtractions(UserId);
CREATE INDEX idx_document_extractions_status ON DocumentExtractions(ProcessingStatus);
```

### DynamoDB Alternative (Serverless-friendly)

```json
{
  "TableName": "ClaimDocumentExtractions",
  "KeySchema": [
    { "AttributeName": "ExtractionId", "KeyType": "HASH" }
  ],
  "AttributeDefinitions": [
    { "AttributeName": "ExtractionId", "AttributeType": "S" },
    { "AttributeName": "UserId", "AttributeType": "S" },
    { "AttributeName": "ProcessingStatus", "AttributeType": "S" }
  ],
  "GlobalSecondaryIndexes": [
    {
      "IndexName": "UserIdIndex",
      "KeySchema": [
        { "AttributeName": "UserId", "KeyType": "HASH" }
      ]
    },
    {
      "IndexName": "StatusIndex",
      "KeySchema": [
        { "AttributeName": "ProcessingStatus", "KeyType": "HASH" }
      ]
    }
  ]
}
```

---

## Configuration Updates

### appsettings.json

```json
{
  "AWS": {
    "Region": "us-east-1",
    "AccessKeyId": "...",
    "SecretAccessKey": "...",
    
    "Textract": {
      "Enabled": true,
      "MaxPages": 50,
      "SupportedFormats": ["pdf", "png", "jpg", "jpeg"],
      "TimeoutSeconds": 300
    },
    
    "Comprehend": {
      "Enabled": true,
      "CustomEntityRecognizerArn": "arn:aws:comprehend:...",
      "LanguageCode": "en"
    },
    
    "Rekognition": {
      "Enabled": true,
      "CustomModelArn": "arn:aws:rekognition:...",
      "MinConfidence": 70.0
    },
    
    "S3": {
      "DocumentBucket": "claims-documents-prod",
      "UploadPrefix": "uploads/",
      "ProcessedPrefix": "processed/",
      "PresignedUrlExpiration": 3600
    }
  },
  
  "DocumentProcessing": {
    "MaxFileSizeMB": 10,
    "AllowedContentTypes": [
      "application/pdf",
      "image/jpeg",
      "image/png"
    ],
    "ExtractionTimeoutMinutes": 5,
    "MinimumConfidenceThreshold": 0.7,
    "RequireUserReviewIfConfidenceBelow": 0.85
  }
}
```

---

## Implementation Phases

### Phase 1: Foundation (Weeks 1-2)
**Goal:** Basic document upload and storage

**Tasks:**
- [ ] Create S3 bucket and configure lifecycle policies
- [ ] Implement DocumentUploadService
- [ ] Create API endpoint for file upload
- [ ] Build frontend file upload component
- [ ] Add DocumentExtractions database table
- [ ] Implement basic validation (file type, size)

**Deliverable:** Users can upload documents, stored in S3

---

### Phase 2: Text Extraction (Weeks 3-4)
**Goal:** Extract text from documents using Textract

**Tasks:**
- [ ] Implement TextractService wrapper
- [ ] Handle async job polling for multi-page docs
- [ ] Parse Textract responses (text, forms, tables)
- [ ] Create extraction status tracking
- [ ] Build webhook for Textract completion (optional)
- [ ] Add error handling and retry logic

**Deliverable:** Raw text extraction from uploaded documents

---

### Phase 3: Entity Recognition (Weeks 5-6)
**Goal:** Identify insurance-specific entities

**Tasks:**
- [ ] Prepare training data for custom Comprehend model
- [ ] Train custom entity recognizer (1,000+ examples)
- [ ] Deploy entity recognizer to endpoint
- [ ] Implement ComprehendService
- [ ] Map entities to claim fields
- [ ] Build confidence scoring logic

**Deliverable:** Structured field extraction with confidence scores

---

### Phase 4: Intelligent Synthesis (Weeks 7-8)
**Goal:** Use Bedrock to create coherent claim data

**Tasks:**
- [ ] Design extraction prompts for Claude
- [ ] Implement BedrockExtractionService
- [ ] Build multi-source data synthesis logic
- [ ] Create validation rules
- [ ] Implement ambiguity detection
- [ ] Add user review workflow

**Deliverable:** Complete claim extraction with high accuracy

---

### Phase 5: Image Analysis (Weeks 9-10) - Optional
**Goal:** Analyze damage photos using Rekognition

**Tasks:**
- [ ] Collect and label vehicle damage images
- [ ] Train custom Rekognition model
- [ ] Implement RekognitionService
- [ ] Integrate image analysis into extraction flow
- [ ] Build damage assessment logic
- [ ] Create fraud detection rules

**Deliverable:** Enhanced claim validation with image evidence

---

### Phase 6: User Experience (Weeks 11-12)
**Goal:** Polished UI for review and correction

**Tasks:**
- [ ] Build extraction results preview UI
- [ ] Implement field-level editing
- [ ] Add confidence score visualization
- [ ] Create guided correction workflow
- [ ] Build document viewer with highlights
- [ ] Add batch upload support

**Deliverable:** Production-ready document extraction UI

---

## Cost Analysis

### Monthly Cost Estimates (1,000 claims/month)

| Service | Usage | Unit Cost | Monthly Cost |
|---------|-------|-----------|--------------|
| **S3 Storage** | 10GB stored + 1,000 uploads | $0.023/GB + requests | **$5** |
| **Textract** | 1,000 pages analyzed (FORMS) | $50/1,000 pages | **$50** |
| **Comprehend** | 1,000 documents × 2KB avg | $0.0001/100 chars | **$4** |
| **Bedrock Claude** | 1,000 extractions × 2K tokens | $3/1M tokens | **$6** |
| **Rekognition** | 500 images analyzed | $1/1,000 images | **$0.50** |
| **DynamoDB** | 1,000 writes + storage | On-demand pricing | **$2** |
| **Data Transfer** | Minimal (within region) | $0.09/GB | **$1** |
| **TOTAL** | | | **~$70/month** |

**Cost per claim:** **$0.07** (excluding existing RAG pipeline costs)

### Cost Optimization Strategies

1. **Use Textract DetectDocumentText** for simple forms (80% cheaper)
2. **Cache Comprehend entity recognizer** (avoid hourly inference costs)
3. **Batch process documents** (reduce API calls)
4. **S3 Intelligent Tiering** (auto-move old docs to cheaper storage)
5. **Pre-filter images** before Rekognition (skip non-damage photos)

---

## Risk Assessment & Mitigation

### Technical Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **OCR accuracy < 90%** | High | Medium | Use Textract FORMS feature; manual review for low confidence |
| **Claude hallucination** | High | Low | Confidence thresholds; require user review; validate against Textract |
| **Textract async timeout** | Medium | Low | Implement proper polling; use SNS notifications; retry logic |
| **High AWS costs** | Medium | Medium | Set budget alerts; optimize feature selection; cache results |
| **Multi-language support** | Medium | Medium | Use Comprehend language detection; train multi-lingual models |
| **Handwriting recognition** | High | High | Textract supports handwriting; prompt users for typed forms when possible |

### Business Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **User mistrust of AI** | High | Medium | Show confidence scores; allow full review/editing; audit trail |
| **Regulatory compliance** | High | Low | Store original documents; log all extractions; human oversight |
| **Data privacy** | High | Low | Encrypt at rest/transit; delete after processing; GDPR compliance |
| **Adoption resistance** | Medium | Medium | Make extraction optional; show time savings; gradual rollout |

---

## Success Metrics (KPIs)

### Accuracy Metrics
- **Field Extraction Accuracy:** > 95% for policy numbers, amounts
- **Overall Confidence Score:** Average > 0.85
- **Manual Correction Rate:** < 20% of extractions need user edits
- **False Positive Rate:** < 5% (incorrect data passed validation)

### Performance Metrics
- **Extraction Time:** < 30 seconds for simple docs, < 2 minutes for complex
- **End-to-End Time:** < 3 minutes (upload → extracted claim)
- **API Response Time:** 95th percentile < 5 seconds
- **Textract Success Rate:** > 99% job completion

### Business Metrics
- **User Adoption:** > 60% of users choose document upload over manual entry
- **Time Savings:** 70% reduction in claim submission time
- **Customer Satisfaction:** NPS > 8/10 for document extraction feature
- **Cost per Claim:** < $0.10 processing cost

---

## Security Considerations

### Data Protection
1. **Encryption:**
   - S3 server-side encryption (SSE-KMS)
   - TLS 1.3 for all API calls
   - Encrypted DynamoDB tables

2. **Access Control:**
   - IAM roles with least privilege
   - User-scoped document access (can't access others' uploads)
   - Pre-signed S3 URLs with expiration

3. **Data Retention:**
   - Delete raw uploads after 30 days (configurable)
   - Archive processed documents for 7 years (compliance)
   - Soft delete with recovery period

4. **Audit Trail:**
   - Log all document uploads and extractions
   - Track user modifications to extracted data
   - CloudTrail for AWS API calls

### Compliance
- **HIPAA:** Potential requirement for health insurance claims
- **PCI DSS:** If processing payment card data in documents
- **GDPR:** Right to deletion, data portability
- **SOC 2:** Regular security audits

---

## Alternative Approaches Considered

### Approach 1: AWS Lambda + Step Functions (Serverless)
**Pros:**
- Auto-scaling
- Pay per execution
- No server management

**Cons:**
- 15-minute Lambda timeout (may be too short for large docs)
- Cold start latency
- More complex debugging

**Verdict:** Good for high-volume, production use; overkill for PoC

---

### Approach 2: Amazon Textract + Simple Regex
**Pros:**
- Much simpler implementation
- Lower cost (no Bedrock/Comprehend)
- Faster processing

**Cons:**
- Poor accuracy for varied document formats
- Can't handle ambiguity
- No intelligent synthesis

**Verdict:** Not suitable for production quality

---

### Approach 3: Third-party OCR (Google Vision, Azure Form Recognizer)
**Pros:**
- Potentially higher accuracy
- Better UI tools

**Cons:**
- Vendor lock-in to non-AWS
- Data leaves AWS ecosystem
- Integration complexity

**Verdict:** Not recommended for AWS-native solution

---

## Testing Strategy

### Unit Tests
- TextractService response parsing
- Entity mapping logic
- Confidence score calculation
- Validation rules

### Integration Tests
- End-to-end document upload → extraction
- Multi-document processing
- Error handling and retries
- AWS service mocking (LocalStack)

### User Acceptance Tests
- Upload various document formats (PDF, images, scanned)
- Test with real claim forms from different insurers
- Handwritten vs. typed forms
- Multi-page documents
- Damaged/poor quality scans

### Load Tests
- 100 concurrent uploads
- 1,000 documents in batch
- Large file handling (10MB PDFs)
- Measure Textract queue times

---

## Monitoring & Observability

### CloudWatch Metrics
- Document upload count
- Extraction success/failure rate
- Average processing time
- Textract job duration
- Confidence score distribution
- User correction rate

### CloudWatch Alarms
- Extraction failure rate > 5%
- Average confidence < 0.8
- Textract timeout rate > 2%
- S3 storage > 100GB

### Logging
```csharp
_logger.LogInformation(
    "Document extraction completed: {DocumentId}, Confidence: {Confidence}, Fields: {Fields}",
    documentId,
    result.OverallConfidence,
    result.ExtractedClaim
);
```

### Dashboards
- Real-time extraction metrics
- Cost tracking per service
- User adoption funnel
- Error rate trends

---

## Future Enhancements

### Phase 7: Advanced Features (6+ months)
1. **Multi-document Intelligence**
   - Combine police report + claim form + estimates automatically
   - Cross-reference data across documents
   - Detect inconsistencies

2. **Intelligent Document Classification**
   - Auto-detect document type without user input
   - Route to appropriate extraction pipeline
   - Suggest missing required documents

3. **Real-time Streaming**
   - Show extraction progress live
   - Stream Textract results as they arrive
   - WebSocket updates to frontend

4. **Mobile App Integration**
   - Native mobile camera integration
   - On-device image preprocessing
   - Offline document queue

5. **Fraud Detection**
   - Detect duplicate claims from images
   - Identify manipulated documents
   - Anomaly detection in patterns

6. **Voice Input**
   - Amazon Transcribe for spoken claim descriptions
   - Convert call center recordings to claims
   - Voice verification

---

## Conclusion

### Implementation Summary

**Total Development Time:** 12-16 weeks  
**Team Size:** 2-3 developers + 1 ML engineer  
**Infrastructure Cost:** ~$70-100/month (1,000 claims)  
**Expected Accuracy:** 90-95% field extraction accuracy  
**User Impact:** 70% reduction in claim submission time  

### Recommended Approach

1. **Start with Phase 1-4** (core extraction) - 8 weeks
2. **Pilot with 100 test users** - 2 weeks
3. **Iterate based on feedback** - 2 weeks
4. **Add image analysis (Phase 5)** if needed - 2 weeks
5. **Full production rollout** - 2 weeks

### Key Success Factors

✅ **Gradual rollout:** Keep manual entry as fallback  
✅ **User trust:** Show confidence scores, allow review  
✅ **Monitoring:** Track accuracy and costs closely  
✅ **Iteration:** Continuously improve prompts and models  
✅ **Training data:** Invest in high-quality entity examples  

---

**Document Version:** 1.0  
**Author:** Technical Architecture Team  
**Last Updated:** January 2026  
**Status:** Proposal - Pending Approval
