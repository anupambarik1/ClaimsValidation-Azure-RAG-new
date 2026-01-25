# POST /api/documents/submit - Step-by-Step Flow Documentation

## Overview
The `/api/documents/submit` endpoint provides a **combined upload and extraction workflow** in a single HTTP request. It uploads a document to S3 and immediately extracts claim data from it, returning both results along with validation status and recommended next actions.

---

## Request Format

```
POST /api/documents/submit
Content-Type: multipart/form-data

Parameters:
- file (required): IFormFile - The document to upload (PDF, JPEG, PNG)
- userId (optional): string - User identifier (defaults to "anonymous")
- documentType (optional): string - Document type enum name (defaults to "ClaimForm")
```

### Example Request (cURL)
```bash
curl -X POST "https://localhost:5001/api/documents/submit" \
  -H "Accept: application/json" \
  -F "file=@claim_form.pdf" \
  -F "userId=user-12345" \
  -F "documentType=ClaimForm"
```

### Example Request (PowerShell)
```powershell
$form = @{
    file = Get-Item -Path "C:\claim_form.pdf"
    userId = "user-12345"
    documentType = "ClaimForm"
}

Invoke-RestMethod -Uri "https://localhost:5001/api/documents/submit" `
    -Method Post `
    -Form $form `
    -SkipCertificateCheck
```

---

## Step-by-Step Execution Flow

### **STEP 1: Input Validation**
**Location:** `DocumentsController.SubmitDocument()` - Line ~150
**Purpose:** Verify request integrity before processing

**Checks:**
1. **File validation:**
   - File exists and has content (`file != null && file.Length > 0`)
   - File size ≤ 10 MB
   - Content-Type is in allowed list (`application/pdf`, `image/jpeg`, `image/png`)

2. **Parameters:**
   - `userId`: Uses provided value or defaults to `"anonymous"`
   - `documentType`: Parses enum string (case-sensitive), defaults to `ClaimForm` if invalid

**Console Output:**
```
[2026-01-25 10:30:45] INFO Uploading document: claim_form.pdf (524288 bytes) for user: user-12345
```

**Error Response Example:**
```json
{
  "error": "File size exceeds maximum allowed size of 10MB"
}
```

---

### **STEP 2: Upload Document to S3**
**Location:** `DocumentsController.UploadDocument()` - Line ~35
**Service:** `DocumentUploadService.UploadAsync()`

**Process:**

#### 2.1 Generate Unique Document ID
```csharp
var documentId = Guid.NewGuid().ToString();
// Example: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

#### 2.2 Construct S3 Key
```csharp
var s3Key = $"{_uploadPrefix}{userId}/{documentId}/{fileName}";
// Example: "uploads/user-12345/a1b2c3d4-e5f6-7890-abcd-ef1234567890/claim_form.pdf"
```

#### 2.3 Upload to S3 with Encryption
**Endpoint:** S3 bucket from config: `AWS:S3:DocumentBucket`
**Default Bucket:** `claims-documents-rag-dev`

```csharp
PutObjectRequest:
{
  BucketName: "claims-documents-rag-dev",
  Key: "uploads/user-12345/a1b2c3d4-e5f6-7890-abcd-ef1234567890/claim_form.pdf",
  InputStream: <file stream>,
  ContentType: "application/pdf",
  ServerSideEncryptionMethod: AES256,
  Metadata: {
    "document-id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "user-id": "user-12345",
    "upload-timestamp": "2026-01-25T10:30:45.1234567Z"
  }
}
```

**Console Output:**
```
[S3] Successfully uploaded document: a1b2c3d4-e5f6-7890-abcd-ef1234567890 to uploads/user-12345/a1b2c3d4-e5f6-7890-abcd-ef1234567890/claim_form.pdf
[2026-01-25 10:30:46] INFO Document uploaded successfully: a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

**Response (Upload Success):**
```json
{
  "documentId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "s3Bucket": "claims-documents-rag-dev",
  "s3Key": "uploads/user-12345/a1b2c3d4-e5f6-7890-abcd-ef1234567890/claim_form.pdf",
  "contentType": "application/pdf",
  "fileSize": 524288,
  "uploadedAt": "2026-01-25T10:30:45.123Z"
}
```

**Error Response Example:**
```json
{
  "error": "Failed to upload document",
  "details": "S3 upload failed: InvalidAccessKeyId - The AWS Access Key Id you provided does not exist in our records."
}
```

---

### **STEP 3: Extract Claim Data from Document**
**Location:** `DocumentsController.SubmitDocument()` - Line ~165
**Service:** `DocumentExtractionOrchestrator.ExtractClaimDataAsync()`

**Process:**

#### 3.1 Verify Document Exists in S3
```csharp
var exists = await _uploadService.ExistsAsync(documentId);
// Lists all objects in S3 with upload prefix and checks if any contain the documentId
```

**Console Output:**
```
[Orchestrator] Starting extraction for document: a1b2c3d4-e5f6-7890-abcd-ef1234567890, type: ClaimForm
```

#### 3.2 Retrieve Actual S3 Key
**Location:** `DocumentExtractionOrchestrator.GetS3KeyForDocument()`
```csharp
// List all objects in S3:UploadPrefix
// Find object matching the documentId
// Return: uploads/user-12345/a1b2c3d4-e5f6-7890-abcd-ef1234567890/claim_form.pdf
```

**Console Output:**
```
[Orchestrator] Found S3 key for document a1b2c3d4-e5f6-7890-abcd-ef1234567890: uploads/user-12345/a1b2c3d4-e5f6-7890-abcd-ef1234567890/claim_form.pdf
```

#### 3.3 Extract Text Using AWS Textract
**Service:** `TextractService.AnalyzeDocumentAsync()`
**AWS Endpoint:** `textract.us-east-1.amazonaws.com`
**Feature Types:** `["FORMS", "TABLES"]` for ClaimForm, `["TEXT"]` for other types

```csharp
StartDocumentAnalysisRequest:
{
  DocumentLocation: {
    S3Object: {
      Bucket: "claims-documents-rag-dev",
      Name: "uploads/user-12345/a1b2c3d4-e5f6-7890-abcd-ef1234567890/claim_form.pdf"
    }
  },
  FeatureTypes: ["FORMS", "TABLES"]
}
```

**Textract Job Lifecycle:**
1. **StartDocumentAnalysis** - Initiates async job
   ```
   [Textract] Starting async document analysis for s3://claims-documents-rag-dev/uploads/user-12345/a1b2c3d4-e5f6-7890-abcd-ef1234567890/claim_form.pdf
   [Textract] Job started with ID: abc1234def5678
   ```

2. **Poll for Completion** - Up to 60 attempts (configurable)
   - Interval: 5000 ms (5 seconds between polls)
   - Max Attempts: 60 (5 minutes total timeout)
   
   ```
   [Textract] Job status: IN_PROGRESS (attempt 1)
   [Textract] Job status: IN_PROGRESS (attempt 2)
   ...
   [Textract] Job status: SUCCEEDED (attempt 8)
   ```

3. **GetDocumentAnalysis** - Retrieves results when SUCCEEDED
   - Extracts text blocks
   - Extracts key-value pairs from forms
   - Extracts tables with row/column structure

**Textract Output Parsing:**
```
Extracted Text: "Claim Form\nPolicy Number: POL-2024-12345\nClaimant: John Doe\n..."

Key-Value Pairs:
{
  "Policy Number": "POL-2024-12345",
  "Claim Amount": "$5,000.00",
  "Incident Date": "2024-01-15"
}

Tables:
[
  {
    Rows: [
      ["Date", "Description", "Amount"],
      ["2024-01-15", "Vehicle Damage", "$5,000.00"]
    ]
  }
]

Confidence: 92.5
```

**Console Output:**
```
[Orchestrator] Step 1: Extracting text with Textract
[Textract] ... (job polling logs)
```

#### 3.4 Extract Entities Using AWS Comprehend
**Service:** `ComprehendService.DetectEntitiesAsync()`
**AWS Endpoint:** `comprehend.us-east-1.amazonaws.com`

```csharp
DetectEntitiesRequest:
{
  Text: "Claim Form\nPolicy Number: POL-2024-12345\nClaimant: John Doe\n...",
  LanguageCode: "en"
}
```

**Entity Recognition:**
- Detects named entities: PERSON, LOCATION, ORGANIZATION, DATE, QUANTITY, MONEY
- Scores confidence for each entity

**Example Output:**
```
Entities:
[
  { Type: "PERSON", Text: "John Doe", Score: 0.97 },
  { Type: "DATE", Text: "2024-01-15", Score: 0.92 },
  { Type: "MONEY", Text: "$5,000.00", Score: 0.95 }
]
```

#### 3.5 Extract Insurance-Specific Fields
**Service:** `ComprehendService.ExtractClaimFieldsAsync()`

**Regex-based extraction for:**
- Policy Number: `\bPOL-[\d-]+\b`
- Claim Amount: Currency patterns
- Dates: ISO 8601 / US formats
- Locations: Geographic names

**Example Output:**
```csharp
ClaimFields:
{
  "policyNumber": "POL-2024-12345",
  "claimAmount": "5000.00",
  "claimType": "Motor",
  "incidentDate": "2024-01-15",
  "claimantLocation": "New York, NY"
}
```

**Console Output:**
```
[Orchestrator] Step 2: Extracting entities with Comprehend
```

#### 3.6 Analyze Images (if applicable)
**Condition:** Only if `documentType == DocumentType.DamagePhotos`
**Service:** `RekognitionService.AnalyzeImagesAsync()`
**AWS Endpoint:** `rekognition.us-east-1.amazonaws.com`

```csharp
// Skip for ClaimForm documents
// For damage photos, would detect:
// - Labels: "vehicle", "damage", "broken glass"
// - Damage Type: Minor, Moderate, Severe
// - Confidence scores
```

**Console Output:**
```
[Orchestrator] Step 3: Analyzing images with Rekognition
(skipped for ClaimForm)
```

#### 3.7 Synthesize Data with Bedrock Claude
**Service:** `DocumentExtractionOrchestrator.SynthesizeClaimDataAsync()`
**LLM Service:** Uses existing `ILlmService` (Bedrock)

**Prompt Construction:**
```
Build extraction prompt combining:
- Raw text from Textract
- Entities from Comprehend
- Key-value pairs and tables
- Image labels (if present)
- Document type hint

System Prompt:
"You are an expert insurance claims data extraction system.
Extract and structure claim information from provided documents.
Apply domain knowledge to resolve ambiguities.
Ensure all monetary amounts are in USD without currency symbols.
Normalize policy types to exactly one of: Motor, Home, Health, Life.
Generate detailed claim descriptions from available information.
Output ONLY valid JSON, no markdown formatting."
```

**LLM Output (Claude 3.5 Sonnet):**
```json
{
  "policyNumber": "POL-2024-12345",
  "claimDescription": "Motor vehicle collision on Route 95. Vehicle struck by other vehicle. Damage to front bumper and hood. No injuries reported.",
  "claimAmount": 5000.00,
  "policyType": "Motor"
}
```

**Console Output:**
```
[Orchestrator] Step 4: Synthesizing data with Bedrock Claude
```

#### 3.8 Validate Extracted Data & Calculate Confidence Scores
**Location:** `DocumentExtractionOrchestrator.ValidateExtractedData()`

**Validation Rules:**

| Field | Rule | Confidence Score |
|-------|------|-------------------|
| **Policy Number** | Format `POL-XXXX-NNNNN` | 0.95 (valid) / 0.70 (partial) / 0.30 (missing) |
| **Claim Amount** | > 0 and < $1M | 0.90 (valid) / 0.60 (suspicious) / 0.30 (missing) |
| **Policy Type** | One of [Motor, Home, Health, Life] | 0.95 (valid) / 0.50 (invalid) |
| **Claim Description** | Length ≥ 20 characters | 0.85 (valid) / 0.40 (missing) |

**Confidence Calculation:**
```csharp
// Weighted average of field confidences:
OverallConfidence = (
  fieldConfidences["policyNumber"] × 0.30 +          // 30% weight
  fieldConfidences["claimAmount"] × 0.30 +           // 30% weight
  fieldConfidences["policyType"] × 0.20 +            // 20% weight
  fieldConfidences["claimDescription"] × 0.20        // 20% weight
)

// Average with Textract confidence
OverallConfidence = (OverallConfidence + textractConfidence/100) / 2

// Penalize for ambiguous fields
OverallConfidence *= (1 - ambiguousFieldsCount × 0.1)

// Clamp to [0, 1]
OverallConfidence = Max(0, Min(1, OverallConfidence))

// Example: Overall Confidence = 0.876 (87.6%)
```

**Ambiguous Fields Detection:**
```csharp
if (policyNumber == "UNKNOWN" || null)
  → Add to ambiguousFields
  → Reduce confidence by 10%
```

**Console Output:**
```
[Orchestrator] Step 5: Validating extracted data
[Orchestrator] Extraction complete. Overall confidence: 0.88
```

---

### **STEP 4: Determine Validation Status**
**Location:** `DocumentsController.DetermineValidationStatus()`

```csharp
if (overallConfidence >= 0.85)
  return "ReadyForSubmission";
else if (overallConfidence >= 0.70)
  return "ReadyForReview";
else
  return "RequiresCorrection";
```

| Confidence Range | Status | Meaning |
|------------------|--------|---------|
| **≥ 0.85** | `ReadyForSubmission` | High confidence, minimal manual review needed |
| **0.70 - 0.84** | `ReadyForReview` | Moderate confidence, should be reviewed |
| **< 0.70** | `RequiresCorrection` | Low confidence, requires manual entry/correction |

---

### **STEP 5: Determine Next Action**
**Location:** `DocumentsController.DetermineNextAction()`

```csharp
if (overallConfidence >= 0.85)
  return "ReviewAndConfirm";
else if (ambiguousFields.Any())
  return $"CorrectFields: {string.Join(", ", ambiguousFields)}";
  // Example: "CorrectFields: policyNumber, claimAmount"
else
  return "ManualEntry";
```

| Condition | Next Action |
|-----------|------------|
| Confidence ≥ 0.85 | `ReviewAndConfirm` |
| Confidence < 0.85 + ambiguous fields | `CorrectFields: [list]` |
| Other cases | `ManualEntry` |

---

### **STEP 6: Return Combined Response**
**Location:** `DocumentsController.SubmitDocument()` - Line ~180

```json
{
  "uploadResult": {
    "documentId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "s3Bucket": "claims-documents-rag-dev",
    "s3Key": "uploads/user-12345/a1b2c3d4-e5f6-7890-abcd-ef1234567890/claim_form.pdf",
    "contentType": "application/pdf",
    "fileSize": 524288,
    "uploadedAt": "2026-01-25T10:30:45.123Z"
  },
  "extractionResult": {
    "extractedClaim": {
      "policyNumber": "POL-2024-12345",
      "claimDescription": "Motor vehicle collision on Route 95. Vehicle struck by other vehicle. Damage to front bumper and hood. No injuries reported.",
      "claimAmount": 5000.0,
      "policyType": "Motor"
    },
    "overallConfidence": 0.876,
    "fieldConfidences": {
      "policyNumber": 0.95,
      "claimAmount": 0.90,
      "policyType": 0.95,
      "claimDescription": 0.85
    },
    "ambiguousFields": [],
    "rawExtractedData": {
      "textractConfidence": 92.5
    }
  },
  "validationStatus": "ReadyForSubmission",
  "nextAction": "ReviewAndConfirm"
}
```

**Console Output:**
```
[2026-01-25 10:30:52] INFO Document submitted and extracted: a1b2c3d4-e5f6-7890-abcd-ef1234567890, confidence: 0.88
```

---

## Complete Timeline Example

```
10:30:45.000 → Request received: submit claim_form.pdf for user-12345
10:30:45.100 → File validation passed (524 KB, PDF)
10:30:45.200 → Unique DocumentId generated: a1b2c3d4-e5f6-7890-abcd-ef1234567890
10:30:45.300 → S3 upload initiated
10:30:45.400 → Document uploaded to S3 (encrypted, with metadata)
10:30:45.500 → S3 key retrieved: uploads/user-12345/a1b2c3d4-e5f6-7890-abcd-ef1234567890/claim_form.pdf
10:30:45.600 → Document existence verified in S3
10:30:45.700 → Textract job started (job ID: abc1234def5678)
10:30:50.700 → Textract polling (attempt 1: IN_PROGRESS)
10:30:55.700 → Textract polling (attempt 2: IN_PROGRESS)
10:31:00.700 → Textract polling (attempt 3: IN_PROGRESS)
10:31:05.700 → Textract polling (attempt 4: SUCCEEDED)
10:31:05.800 → Textract results retrieved (confidence: 92.5%)
10:31:06.000 → Comprehend entity extraction initiated
10:31:06.500 → Comprehend completed (5 entities found)
10:31:06.600 → Claim field extraction completed
10:31:06.700 → Bedrock Claude synthesis initiated
10:31:07.200 → Bedrock response received (structured JSON)
10:31:07.300 → Data validation completed
10:31:07.350 → Confidence calculation: 0.876 (87.6%)
10:31:07.400 → Validation status: ReadyForSubmission
10:31:07.450 → Next action: ReviewAndConfirm
10:31:07.500 → Response sent to client
```

**Total Time:** ~22.5 seconds (Textract job polling accounts for ~20 seconds)

---

## Error Scenarios

### **1. File Validation Fails**
```json
{
  "error": "File type image/bmp not allowed. Supported types: application/pdf, image/jpeg, image/png"
}
```

### **2. S3 Upload Fails (Invalid Credentials)**
```json
{
  "error": "Failed to upload document",
  "details": "S3 upload failed: InvalidAccessKeyId - The AWS Access Key Id you provided does not exist in our records."
}
```

### **3. Document Not Found in S3 (Upload succeeded but retrieval fails)**
```json
{
  "error": "Failed to extract claim data",
  "details": "Document a1b2c3d4-e5f6-7890-abcd-ef1234567890 not found in S3"
}
```

### **4. Textract Job Fails**
```json
{
  "error": "Failed to extract claim data",
  "details": "Textract job failed: InvalidS3ObjectException - Unable to get object metadata from S3. Check object key, region and/or access permissions."
}
```

### **5. Textract Timeout (exceeds 5 minutes)**
```json
{
  "error": "Failed to extract claim data",
  "details": "Textract job did not complete within 60 attempts"
}
```

### **6. Bedrock Service Error**
```json
{
  "error": "Failed to extract claim data",
  "details": "Bedrock API error: ThrottlingException - Rate exceeded."
}
```

---

## Configuration Parameters

All timing and behavior controlled via `appsettings.json`:

```json
{
  "AWS": {
    "Region": "us-east-1",
    "AccessKeyId": "AKIA...",
    "SecretAccessKey": "...",
    "S3": {
      "DocumentBucket": "claims-documents-rag-dev",
      "UploadPrefix": "uploads/",
      "ProcessedPrefix": "processed/"
    },
    "Textract": {
      "Enabled": true,
      "MaxPages": 50,
      "PollingIntervalMs": 5000,
      "MaxPollingAttempts": 60
    },
    "Comprehend": {
      "Enabled": true,
      "CustomEntityRecognizerArn": "",
      "LanguageCode": "en"
    },
    "Rekognition": {
      "Enabled": true,
      "CustomModelArn": "",
      "MinConfidence": 70.0
    }
  },
  "DocumentProcessing": {
    "MaxFileSizeMB": 10,
    "AllowedContentTypes": ["application/pdf", "image/jpeg", "image/png"],
    "ExtractionTimeoutMinutes": 5,
    "MinimumConfidenceThreshold": 0.7
  }
}
```

---

## Performance Optimization Tips

| Factor | Impact | Optimization |
|--------|--------|-------------|
| **Textract Polling** | ~20 seconds | Reduce `PollingIntervalMs` (default 5000) - but increases API calls |
| **Large Documents** | Processing time | Limit `MaxPages` in config (default 50) |
| **Image Analysis** | +5-10 seconds | Only enable for damage photo documents |
| **Comprehend Calls** | +1-2 seconds | Batch multiple documents using `extract-multiple` endpoint |
| **Bedrock Response** | +0.5-2 seconds | Use faster model (e.g., Claude 3 Haiku) for quick extractions |

---

## Dependencies & AWS Service Calls

```
DocumentsController.SubmitDocument()
├── DocumentUploadService.UploadAsync()
│   └── S3: PutObjectAsync() ★ Required
├── DocumentExtractionService.ExtractClaimDataAsync()
│   ├── DocumentUploadService.ExistsAsync()
│   │   └── S3: ListObjectsV2Async() ★ Required
│   ├── TextractService.AnalyzeDocumentAsync()
│   │   ├── Textract: StartDocumentAnalysisAsync() ★ Required
│   │   └── Textract: GetDocumentAnalysisAsync() ★ Required (polling)
│   ├── ComprehendService.DetectEntitiesAsync()
│   │   └── Comprehend: DetectEntitiesAsync() ★ Required
│   ├── ComprehendService.ExtractClaimFieldsAsync()
│   │   └── Regex patterns (local)
│   ├── RekognitionService.AnalyzeImagesAsync() ⊘ Conditional
│   │   └── Rekognition: DetectLabelsAsync()
│   ├── DocumentExtractionOrchestrator.SynthesizeClaimDataAsync()
│   │   └── LlmService (Bedrock) ★ Required
│   └── DocumentExtractionOrchestrator.ValidateExtractedData()
│       └── Local validation logic
└── Response assembly & return
```

**★ Critical path (must succeed)**  
**⊘ Conditional (only on demand)**
