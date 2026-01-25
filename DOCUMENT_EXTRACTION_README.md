# Document Extraction Feature - Quick Start

## Overview

The document extraction feature allows users to upload claim documents (PDFs, images) and automatically extract claim fields using AWS AI services:
- **Amazon S3**: Document storage
- **Amazon Textract**: OCR and form extraction
- **Amazon Comprehend**: Entity recognition
- **Amazon Rekognition**: Image analysis (optional)
- **Amazon Bedrock**: Intelligent data synthesis

## Prerequisites

### 1. AWS Services Setup

You need to enable and configure the following AWS services:

#### A. Create S3 Bucket for Documents
```powershell
aws s3 mb s3://claims-documents-dev --region us-east-1
```

#### B. Enable Textract (No setup required - pay per use)
- Service is ready to use with your AWS credentials
- Pricing: $1.50 per 1,000 pages (text detection), $50 per 1,000 pages (form analysis)

#### C. Enable Comprehend (No setup required for basic use)
- Built-in entity recognition works out of the box
- For custom entity recognition (insurance-specific), see "Advanced Setup" below

#### D. Enable Rekognition (Optional - for damage photo analysis)
```powershell
# No setup required for standard label detection
# For custom damage detection model, see "Advanced Setup" below
```

### 2. IAM Permissions

Ensure your IAM user/role has these permissions:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:ListBucket"
      ],
      "Resource": [
        "arn:aws:s3:::claims-documents-dev",
        "arn:aws:s3:::claims-documents-dev/*"
      ]
    },
    {
      "Effect": "Allow",
      "Action": [
        "textract:StartDocumentTextDetection",
        "textract:GetDocumentTextDetection",
        "textract:StartDocumentAnalysis",
        "textract:GetDocumentAnalysis"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "comprehend:DetectEntities"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "rekognition:DetectLabels",
        "rekognition:DetectText"
      ],
      "Resource": "*"
    }
  ]
}
```

### 3. Configuration

Update `src/ClaimsRagBot.Api/appsettings.json`:

```json
{
  "AWS": {
    "Region": "us-east-1",
    "AccessKeyId": "YOUR_ACCESS_KEY",
    "SecretAccessKey": "YOUR_SECRET_KEY",
    "S3": {
      "DocumentBucket": "claims-documents-dev",
      "UploadPrefix": "uploads/",
      "ProcessedPrefix": "processed/"
    },
    "Textract": {
      "Enabled": true,
      "PollingIntervalMs": 5000,
      "MaxPollingAttempts": 60
    },
    "Comprehend": {
      "Enabled": true
    },
    "Rekognition": {
      "Enabled": true,
      "MinConfidence": 70.0
    }
  }
}
```

## Installation

### 1. Restore NuGet Packages
```powershell
cd src/ClaimsRagBot.Infrastructure
dotnet restore
```

### 2. Build the Project
```powershell
cd ../../
dotnet build
```

### 3. Run the API
```powershell
cd src/ClaimsRagBot.Api
dotnet run
```

The API will start at: `https://localhost:5001`

## Usage

### API Endpoints

#### 1. Upload Document Only
```http
POST https://localhost:5001/api/documents/upload
Content-Type: multipart/form-data

file: claim_form.pdf
userId: user123
```

**Response:**
```json
{
  "documentId": "abc123-def456",
  "s3Bucket": "claims-documents-dev",
  "s3Key": "uploads/user123/abc123-def456/claim_form.pdf",
  "contentType": "application/pdf",
  "fileSize": 245678,
  "uploadedAt": "2026-01-25T10:30:00Z"
}
```

#### 2. Extract Claim Data from Document
```http
POST https://localhost:5001/api/documents/extract
Content-Type: application/json

{
  "documentId": "abc123-def456",
  "documentType": "ClaimForm"
}
```

**Response:**
```json
{
  "extractedClaim": {
    "policyNumber": "POL-2024-12345",
    "claimDescription": "Vehicle collision on Highway 101 resulting in front bumper damage...",
    "claimAmount": 2500,
    "policyType": "Motor"
  },
  "overallConfidence": 0.92,
  "fieldConfidences": {
    "policyNumber": 0.95,
    "claimDescription": 0.88,
    "claimAmount": 0.97,
    "policyType": 0.91
  },
  "ambiguousFields": [],
  "rawExtractedData": {
    "textractConfidence": 94.5
  }
}
```

#### 3. Upload and Extract in One Step
```http
POST https://localhost:5001/api/documents/submit
Content-Type: multipart/form-data

file: claim_form.pdf
userId: user123
documentType: ClaimForm
```

**Response:**
```json
{
  "uploadResult": { ... },
  "extractionResult": { ... },
  "validationStatus": "ReadyForReview",
  "nextAction": "ReviewAndConfirm"
}
```

### PowerShell Examples

#### Upload Document
```powershell
$file = "C:\path\to\claim_form.pdf"
$uri = "https://localhost:5001/api/documents/upload"

$form = @{
    file = Get-Item -Path $file
    userId = "user123"
}

$response = Invoke-RestMethod -Uri $uri -Method Post -Form $form
$documentId = $response.documentId
Write-Host "Document uploaded: $documentId"
```

#### Extract Claim Data
```powershell
$uri = "https://localhost:5001/api/documents/extract"
$body = @{
    documentId = $documentId
    documentType = "ClaimForm"
} | ConvertTo-Json

$result = Invoke-RestMethod -Uri $uri -Method Post -Body $body -ContentType "application/json"
Write-Host "Extracted claim: $($result.extractedClaim.policyNumber)"
Write-Host "Confidence: $($result.overallConfidence)"
```

## Testing

### Test with Sample Document

1. Create a test claim form (or use a real one)
2. Upload via Swagger UI: https://localhost:5001/swagger
3. Navigate to `/api/documents/submit`
4. Upload your PDF/image
5. Review extracted data

### Expected Processing Time

- **Simple text document (1 page)**: 10-20 seconds
- **Complex form with tables (5 pages)**: 30-60 seconds
- **With image analysis**: +10 seconds per image

### Confidence Thresholds

- **≥ 0.85**: Ready for automatic submission
- **0.70 - 0.84**: Requires user review
- **< 0.70**: Requires manual correction

## Document Types Supported

| Document Type | Textract Mode | Use Case |
|--------------|---------------|----------|
| `ClaimForm` | FORMS + TABLES | Standard insurance claim forms |
| `PoliceReport` | FORMS + TABLES | Accident/incident reports |
| `RepairEstimate` | FORMS + TABLES | Mechanic/contractor estimates |
| `DamagePhotos` | TEXT + Rekognition | Photos of damage |
| `MedicalRecords` | FORMS | Injury/health claim documentation |
| `Mixed` | FORMS + TABLES | Multiple document types |

## Troubleshooting

### Error: "Document bucket not found"
```powershell
# Create the S3 bucket
aws s3 mb s3://claims-documents-dev --region us-east-1
```

### Error: "Textract job timed out"
- Increase `MaxPollingAttempts` in appsettings.json
- Check if document is corrupted or too large (> 50 pages)

### Error: "Access Denied"
- Verify IAM permissions (see Prerequisites section)
- Check AWS credentials in appsettings.json

### Low Extraction Confidence
- Use higher quality scans (300 DPI+)
- Ensure text is clearly readable
- Avoid handwritten forms (use typed forms when possible)
- Enable Comprehend custom entity recognizer (see Advanced Setup)

## Advanced Setup

### Custom Comprehend Entity Recognizer

For better insurance-specific entity extraction:

1. **Prepare Training Data** (1,000+ examples)
```csv
Text,Entity
"Policy Number: POL-2024-12345",POLICY_NUMBER
"Claim amount of $2,500",CLAIM_AMOUNT
"Motor insurance policy",POLICY_TYPE
```

2. **Train Custom Model**
```powershell
# Upload training data
aws s3 cp training-data.csv s3://claims-documents-dev/training/

# Create recognizer (takes 30-60 minutes)
aws comprehend create-entity-recognizer `
  --recognizer-name claims-entities `
  --language-code en `
  --input-data-config "DataFormat=COMPREHEND_CSV,EntityTypes=[{Type=POLICY_NUMBER},{Type=CLAIM_AMOUNT},{Type=POLICY_TYPE}],Documents={S3Uri=s3://claims-documents-dev/training/training-data.csv}" `
  --data-access-role-arn arn:aws:iam::YOUR_ACCOUNT:role/ComprehendRole
```

3. **Update Configuration**
```json
{
  "AWS": {
    "Comprehend": {
      "CustomEntityRecognizerArn": "arn:aws:comprehend:us-east-1:123456789:entity-recognizer/claims-entities"
    }
  }
}
```

### Custom Rekognition Model (Vehicle Damage Detection)

1. **Collect and Label Images** (500+ images per damage type)
2. **Train Custom Model** via AWS Console:
   - Amazon Rekognition > Custom Labels
   - Create project: "vehicle-damage-detection"
   - Add labels: Collision, Scratch, Dent, etc.
   - Train model (2-4 hours)

3. **Update Configuration**
```json
{
  "AWS": {
    "Rekognition": {
      "CustomModelArn": "arn:aws:rekognition:us-east-1:123456789:project/vehicle-damage/version/1"
    }
  }
}
```

## Cost Estimates

For 1,000 documents/month:

| Service | Usage | Monthly Cost |
|---------|-------|--------------|
| S3 Storage | 10GB + 1,000 uploads | $5 |
| Textract | 1,000 pages (FORMS) | $50 |
| Comprehend | 1,000 docs × 2KB | $4 |
| Rekognition | 500 images | $0.50 |
| **Total** | | **~$60/month** |

**Cost per claim extraction: ~$0.06**

## Next Steps

1. Test with sample documents
2. Integrate with frontend UI
3. Add user review/correction workflow
4. Train custom Comprehend model for better accuracy
5. Set up production S3 bucket with lifecycle policies
6. Enable CloudWatch monitoring
7. Implement DynamoDB tracking for extraction history

## Support

For issues or questions:
- Check logs: Console output from DocumentExtractionOrchestrator
- Review AWS CloudWatch logs for Textract/Comprehend errors
- Verify IAM permissions
- Test with simpler documents first

---

**Implementation Status**: ✅ Complete and ready for testing
**Last Updated**: January 2026
