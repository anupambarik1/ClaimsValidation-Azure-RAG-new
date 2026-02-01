# AWS Services Configuration Guide
## Complete Reference for Claims RAG Bot AWS Resources

This guide provides step-by-step instructions to view, verify, and troubleshoot all AWS services used by the Claims RAG Bot application in your AWS account.

---

## Table of Contents
1. [AWS Services Overview](#aws-services-overview)
2. [Amazon Bedrock Configuration](#amazon-bedrock-configuration)
3. [Amazon OpenSearch Serverless](#amazon-opensearch-serverless)
4. [Amazon DynamoDB](#amazon-dynamodb)
5. [Amazon S3](#amazon-s3)
6. [Amazon Textract](#amazon-textract)
7. [Amazon Comprehend](#amazon-comprehend)
8. [IAM Roles and Permissions](#iam-roles-and-permissions)
9. [CloudWatch Logs and Monitoring](#cloudwatch-logs-and-monitoring)
10. [Cost Analysis and Budgets](#cost-analysis-and-budgets)
11. [Troubleshooting Checklist](#troubleshooting-checklist)

---

## AWS Services Overview

### Services Used by Claims RAG Bot

| Service | Purpose | Primary Function | Code Reference |
|---------|---------|------------------|----------------|
| **Amazon Bedrock** | AI/LLM & Embeddings | Generate embeddings and AI decisions | `EmbeddingService.cs`, `LlmService.cs` |
| **OpenSearch Serverless** | Vector Database | Semantic search of policy clauses | `RetrievalService.cs` |
| **DynamoDB** | NoSQL Database | Store claim audit trail | `AuditService.cs` |
| **S3** | Object Storage | Store uploaded claim documents | `S3Service.cs` |
| **Textract** | OCR Service | Extract text from documents | `TextractService.cs` |
| **Comprehend** | NLP Service | Extract entities from text | `ComprehendService.cs` |
| **IAM** | Access Control | Permissions for services | All services |
| **CloudWatch** | Logging & Monitoring | Track API calls and errors | All services |

### Region Configuration

**Default Region**: `us-east-1` (N. Virginia)

**Where to Check**:
- File: `src/ClaimsRagBot.Api/appsettings.json`
- Key: `"AWS:Region": "us-east-1"`

**To Change Region**:
1. Update `appsettings.json`
2. Verify all services are available in target region
3. Update CloudFormation template region if using infrastructure as code

---

## Amazon Bedrock Configuration

### Overview
Amazon Bedrock provides access to foundation models (LLMs) for AI-powered claim validation.

### Models Used

| Model | Model ID | Purpose | Code File |
|-------|----------|---------|-----------|
| **Titan Embeddings v1** | `amazon.titan-embed-text-v1` | Generate 1536-dim vectors | `EmbeddingService.cs` |
| **Claude 3.5 Sonnet** | `us.anthropic.claude-3-5-sonnet-20241022-v2:0` | AI decision reasoning | `LlmService.cs` |

### Step-by-Step: Access Bedrock in AWS Console

#### 1. Navigate to Amazon Bedrock
```
AWS Console → Services → Search "Bedrock" → Amazon Bedrock
```

#### 2. Check Model Access
```
Left Menu → Bedrock configurations → Model access
```

**What to Verify**:
- ✅ **Titan Embeddings - Text v1**: Status should be "Access granted"
- ✅ **Claude 3.5 Sonnet v2**: Status should be "Access granted"

**If NOT Granted**:
1. Click "Manage model access" (top right)
2. Check boxes for:
   - ✅ Amazon Titan Embeddings
   - ✅ Anthropic Claude 3.5 Sonnet
3. Click "Request model access"
4. Wait 1-5 minutes for approval (usually instant)

#### 3. View Model Invocations
```
Left Menu → Bedrock configurations → Invocation logging
```

**What to See**:
- Model invocations count
- Success/failure rates
- Throttling events

**How to Enable Logging** (if not enabled):
1. Click "Edit"
2. Enable "Model invocations"
3. Select CloudWatch Logs or S3 destination
4. Save changes

#### 4. Test Model Access (Optional)
```
Left Menu → Text, Image, Chat → Chat
```

1. Select "Claude 3.5 Sonnet"
2. Type test message: "Hello, test"
3. Should receive response
4. Confirms model access is working

### Bedrock API Calls in Application

#### Embedding Generation
**File**: `src/ClaimsRagBot.Infrastructure/Bedrock/EmbeddingService.cs`

```csharp
var request = new InvokeModelRequest
{
    ModelId = "amazon.titan-embed-text-v1",
    Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestBody))),
    ContentType = "application/json"
};
```

**CloudWatch Logs Path**:
```
CloudWatch → Log groups → /aws/bedrock/modelinvocations
```

**Sample Log Entry**:
```json
{
  "schemaType": "ModelInvocationLog",
  "modelId": "amazon.titan-embed-text-v1",
  "operation": "InvokeModel",
  "timestamp": "2026-02-01T14:30:00.000Z",
  "input": {
    "inputContentType": "application/json",
    "inputBodyJson": {
      "inputText": "Hospital confinement for 3 days due to pneumonia"
    }
  },
  "output": {
    "outputContentType": "application/json"
  }
}
```

#### LLM Decision Generation
**File**: `src/ClaimsRagBot.Infrastructure/Bedrock/LlmService.cs`

```csharp
var invokeRequest = new InvokeModelRequest
{
    ModelId = "us.anthropic.claude-3-5-sonnet-20241022-v2:0",
    Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestBody))),
    ContentType = "application/json"
};
```

**Request Body Structure**:
```json
{
  "anthropic_version": "bedrock-2023-05-31",
  "max_tokens": 1024,
  "messages": [
    {
      "role": "user",
      "content": "Claim:\nPolicy Number: AFLAC-HOSP-2024-001\n..."
    }
  ],
  "system": "You are an insurance claims validation assistant..."
}
```

### Common Bedrock Errors

| Error Code | Meaning | Solution |
|------------|---------|----------|
| `AccessDeniedException` | No model access or IAM permission issue | Request model access, check IAM policy |
| `ThrottlingException` | Too many requests | Implement retry with exponential backoff |
| `ValidationException` | Invalid request format | Check model ID, request body structure |
| `ResourceNotFoundException` | Model not available in region | Verify region supports model |

---

## Amazon OpenSearch Serverless

### Overview
OpenSearch Serverless provides vector search capabilities for semantic matching of claim descriptions against policy clauses.

### Collection Configuration

**Collection Name**: `claims-policies-dev` (or as configured)
**Collection Type**: `VECTORSEARCH`
**Index Name**: `policy-clauses`

### Step-by-Step: Access OpenSearch in AWS Console

#### 1. Navigate to OpenSearch Service
```
AWS Console → Services → Search "OpenSearch" → Amazon OpenSearch Service
```

#### 2. View Serverless Collections
```
Left Menu → Serverless → Collections
```

**What to See**:
- Collection name: `claims-policies-dev`
- Status: "Active" (green)
- Endpoint URL (copy this for `appsettings.json`)

**Collection Details to Note**:
```
Collection endpoint: https://xxxxxxx.us-east-1.aoss.amazonaws.com
ARN: arn:aws:aoss:us-east-1:123456789012:collection/xxxxxxx
```

#### 3. Check Network Policy
```
Collections → Select your collection → Security configuration tab → Network access
```

**Configuration**:
```json
{
  "Rules": [
    {
      "ResourceType": "collection",
      "Resource": ["collection/claims-policies-dev"]
    }
  ],
  "AllowFromPublic": true
}
```

**Note**: For production, restrict to VPC endpoints instead of public access.

#### 4. Check Encryption Policy
```
Collections → Select your collection → Security configuration tab → Encryption
```

**Configuration**:
```json
{
  "Rules": [
    {
      "ResourceType": "collection",
      "Resource": ["collection/claims-policies-dev"]
    }
  ],
  "AWSOwnedKey": true
}
```

#### 5. Check Data Access Policy
```
Left Menu → Serverless → Data access policies
```

**What to Verify**:
- Policy grants permissions to your IAM role
- Permissions include: `aoss:*` or specific actions

**Sample Policy**:
```json
[
  {
    "Rules": [
      {
        "ResourceType": "collection",
        "Resource": ["collection/claims-policies-dev"],
        "Permission": ["aoss:*"]
      },
      {
        "ResourceType": "index",
        "Resource": ["index/claims-policies-dev/*"],
        "Permission": ["aoss:*"]
      }
    ],
    "Principal": [
      "arn:aws:iam::123456789012:role/ClaimsApiRole"
    ]
  }
]
```

#### 6. View Index and Documents
```
Collections → Select collection → OpenSearch Dashboards URL
```

**In OpenSearch Dashboards**:

1. **Dev Tools Console**:
```
Left Menu → Management → Dev Tools
```

2. **Check Index Exists**:
```json
GET _cat/indices
```

Expected output:
```
green open policy-clauses ... 1536
```

3. **Check Index Mapping**:
```json
GET /policy-clauses/_mapping
```

Expected structure:
```json
{
  "policy-clauses": {
    "mappings": {
      "properties": {
        "clauseId": { "type": "keyword" },
        "text": { "type": "text" },
        "coverageType": { "type": "keyword" },
        "policyType": { "type": "keyword" },
        "embedding": {
          "type": "knn_vector",
          "dimension": 1536,
          "method": {
            "engine": "nmslib",
            "name": "hnsw"
          }
        }
      }
    }
  }
}
```

4. **Count Documents**:
```json
GET /policy-clauses/_count
```

Expected:
```json
{
  "count": 50
}
```

5. **View Sample Document**:
```json
GET /policy-clauses/_search
{
  "size": 1
}
```

Expected response:
```json
{
  "hits": {
    "hits": [
      {
        "_source": {
          "clauseId": "AFLAC-HOSP-2.1",
          "text": "Hospital Indemnity benefit pays $500 per day for hospital confinement",
          "coverageType": "Hospital Indemnity",
          "policyType": "health",
          "embedding": [0.0234, -0.0567, ...]
        }
      }
    ]
  }
}
```

### OpenSearch Query from Application

**File**: `src/ClaimsRagBot.Infrastructure/OpenSearch/RetrievalService.cs`

**Query Structure**:
```json
{
  "size": 5,
  "query": {
    "bool": {
      "must": [
        {
          "knn": {
            "embedding": {
              "vector": [0.0234, -0.0567, ...],
              "k": 5
            }
          }
        }
      ],
      "filter": [
        {
          "term": {
            "policyType": "health"
          }
        }
      ]
    }
  },
  "_source": ["clauseId", "text", "coverageType", "policyType"]
}
```

**Testing Query Manually**:
```json
POST /policy-clauses/_search
{
  "size": 5,
  "query": {
    "match": {
      "text": "hospital pneumonia"
    }
  }
}
```

### OpenSearch Metrics

**CloudWatch Metrics Path**:
```
CloudWatch → Metrics → OpenSearch Service → Serverless
```

**Key Metrics**:
- `SearchRate`: Number of search requests/second
- `SearchLatency`: Average query time
- `IndexingRate`: Document indexing rate
- `2xx`, `4xx`, `5xx`: HTTP response counts

---

## Amazon DynamoDB

### Overview
DynamoDB stores the complete audit trail of all claim validations, including AI decisions, specialist reviews, and metadata.

### Table Configuration

**Table Name**: `ClaimsAuditTrail`
**Billing Mode**: `PAY_PER_REQUEST` (On-Demand)

### Step-by-Step: Access DynamoDB in AWS Console

#### 1. Navigate to DynamoDB
```
AWS Console → Services → Search "DynamoDB" → DynamoDB
```

#### 2. View Tables
```
Left Menu → Tables
```

**What to See**:
- Table name: `ClaimsAuditTrail`
- Status: "Active" (green)
- Item count

#### 3. View Table Details
```
Tables → Click "ClaimsAuditTrail"
```

**Overview Tab**:
- **Table ARN**: `arn:aws:dynamodb:us-east-1:123456789012:table/ClaimsAuditTrail`
- **Item count**: Number of claims stored
- **Table size**: Storage used
- **Read/Write capacity**: On-demand

#### 4. Check Table Schema
```
Table → Actions → View details → Indexes tab
```

**Primary Key**:
- **Partition key**: `ClaimId` (String)

**Global Secondary Index** (if configured):
- **Index name**: `PolicyNumberIndex`
- **Partition key**: `PolicyNumber` (String)
- **Sort key**: `Timestamp` (String)

#### 5. View Attribute Definitions
```
Table → Actions → View details → Attributes tab
```

**Attributes**:
| Attribute Name | Type | Description |
|----------------|------|-------------|
| `ClaimId` | String (S) | Unique claim identifier (GUID) |
| `Timestamp` | String (S) | ISO 8601 timestamp |
| `PolicyNumber` | String (S) | Policy identifier |
| `ClaimAmount` | Number (N) | Dollar amount |
| `ClaimDescription` | String (S) | Claim details |
| `DecisionStatus` | String (S) | "Covered", "Not Covered", "Manual Review" |
| `Explanation` | String (S) | AI reasoning |
| `ConfidenceScore` | Number (N) | 0.0-1.0 |
| `ClauseReferences` | List (L) | Array of clause IDs |
| `RequiredDocuments` | List (L) | Array of document types |
| `RetrievedClauses` | String (S) | JSON of retrieval metadata |
| `SpecialistNotes` | String (S) | Optional specialist comments |
| `SpecialistId` | String (S) | Optional specialist ID |
| `ReviewedAt` | String (S) | Optional review timestamp |

#### 6. View Sample Items
```
Table → Explore table items
```

**What to See**:
- List of all claims
- Click any item to see full details

**Sample Item**:
```json
{
  "ClaimId": "a7b3c4d5-e6f7-8g9h-0i1j-2k3l4m5n6o7p",
  "Timestamp": "2026-02-01T14:30:00.000Z",
  "PolicyNumber": "AFLAC-HOSP-2024-001",
  "ClaimAmount": 1500,
  "ClaimDescription": "Hospital confinement for 3 days due to pneumonia",
  "DecisionStatus": "Covered",
  "Explanation": "Hospital Indemnity benefit pays $500/day for 3 days...",
  "ConfidenceScore": 0.96,
  "ClauseReferences": ["AFLAC-HOSP-2.1", "AFLAC-HOSP-3.2"],
  "RequiredDocuments": ["Hospital admission records", "Discharge summary"],
  "RetrievedClauses": "[{\"ClauseId\":\"AFLAC-HOSP-2.1\",\"Score\":0.92}]"
}
```

#### 7. Query Items by ClaimId
```
Explore table items → Scan/Query items → Query
```

**Query Configuration**:
- **Table/Index**: ClaimsAuditTrail
- **Partition key**: ClaimId
- **Partition key value**: `a7b3c4d5-e6f7-8g9h-0i1j-2k3l4m5n6o7p`
- Click "Run"

#### 8. Query Items by PolicyNumber (using GSI)
```
Explore table items → Scan/Query items → Query
```

**Query Configuration**:
- **Table/Index**: PolicyNumberIndex
- **Partition key**: PolicyNumber
- **Partition key value**: `AFLAC-HOSP-2024-001`
- Click "Run"

#### 9. Filter Items by Status
```
Explore table items → Scan/Query items → Scan
```

**Add Filter**:
- **Attribute name**: DecisionStatus
- **Condition**: =
- **Value**: `Covered` (or `Not Covered`, `Manual Review`)
- Click "Run"

### DynamoDB Operations from Application

**File**: `src/ClaimsRagBot.Infrastructure/DynamoDB/AuditService.cs`

#### PutItem (Save Claim)
```csharp
var putRequest = new PutItemRequest
{
    TableName = "ClaimsAuditTrail",
    Item = new Dictionary<string, AttributeValue>
    {
        ["ClaimId"] = new AttributeValue { S = Guid.NewGuid().ToString() },
        ["Timestamp"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
        ["PolicyNumber"] = new AttributeValue { S = request.PolicyNumber },
        // ... more attributes
    }
};
await _client.PutItemAsync(putRequest);
```

#### GetItem (Retrieve Single Claim)
```csharp
var request = new GetItemRequest
{
    TableName = "ClaimsAuditTrail",
    Key = new Dictionary<string, AttributeValue>
    {
        ["ClaimId"] = new AttributeValue { S = claimId }
    }
};
var response = await _client.GetItemAsync(request);
```

#### Scan (Get All Claims)
```csharp
var request = new ScanRequest
{
    TableName = "ClaimsAuditTrail",
    FilterExpression = "DecisionStatus = :status",
    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
    {
        [":status"] = new AttributeValue { S = "Covered" }
    }
};
var response = await _client.ScanAsync(request);
```

#### UpdateItem (Specialist Override)
```csharp
var updateRequest = new UpdateItemRequest
{
    TableName = "ClaimsAuditTrail",
    Key = new Dictionary<string, AttributeValue>
    {
        ["ClaimId"] = new AttributeValue { S = claimId }
    },
    UpdateExpression = "SET DecisionStatus = :status, SpecialistNotes = :notes, SpecialistId = :specialistId, ReviewedAt = :reviewedAt",
    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
    {
        [":status"] = new AttributeValue { S = newStatus },
        [":notes"] = new AttributeValue { S = specialistNotes },
        [":specialistId"] = new AttributeValue { S = specialistId },
        [":reviewedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
    }
};
await _client.UpdateItemAsync(updateRequest);
```

### DynamoDB Metrics

**CloudWatch Metrics Path**:
```
CloudWatch → Metrics → DynamoDB → Table Metrics
```

**Key Metrics**:
- `ConsumedReadCapacityUnits`: Read usage
- `ConsumedWriteCapacityUnits`: Write usage
- `UserErrors`: Client-side errors (4xx)
- `SystemErrors`: Server-side errors (5xx)
- `SuccessfulRequestLatency`: Average request time

### Export Data from DynamoDB

#### Export to S3
```
Table → Actions → Export to S3
```

1. Choose destination S3 bucket
2. Select export format (DynamoDB JSON or ION)
3. Click "Export"
4. Monitor in "Exports and streams" tab

#### Export to CSV (via Console)
```
Explore table items → Select items → Export results → CSV
```

---

## Amazon S3

### Overview
S3 stores uploaded claim documents (PDFs, images) for OCR processing and archival.

### Bucket Configuration

**Bucket Name**: `claims-documents-rag-dev` (as configured in `appsettings.json`)
**Region**: `us-east-1`

### Step-by-Step: Access S3 in AWS Console

#### 1. Navigate to S3
```
AWS Console → Services → Search "S3" → S3
```

#### 2. View Buckets
```
Buckets → Search for "claims-documents"
```

**What to See**:
- Bucket name: `claims-documents-rag-dev`
- Region: `us-east-1`
- Access: Private (Block all public access enabled)

#### 3. View Bucket Details
```
Buckets → Click bucket name
```

**Objects Tab**:
- Folder structure:
  ```
  uploads/         # Uploaded documents
  processed/       # Post-OCR documents
  claims/          # Organized by user/claim
  ```

**Sample Object Path**:
```
s3://claims-documents-rag-dev/claims/user123/claim-abc-123/hospital-bill.pdf
```

#### 4. View Object Properties
```
Objects → Click on a file
```

**Object Details**:
- **Object URL**: `s3://claims-documents-rag-dev/claims/.../file.pdf`
- **Size**: File size in bytes
- **Last modified**: Upload timestamp
- **Storage class**: Standard
- **Server-side encryption**: Enabled (SSE-S3 or SSE-KMS)

#### 5. Check Bucket Permissions
```
Bucket → Permissions tab
```

**Bucket Policy** (sample):
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "AllowClaimsAPIAccess",
      "Effect": "Allow",
      "Principal": {
        "AWS": "arn:aws:iam::123456789012:role/ClaimsApiRole"
      },
      "Action": [
        "s3:GetObject",
        "s3:PutObject",
        "s3:DeleteObject"
      ],
      "Resource": "arn:aws:s3:::claims-documents-rag-dev/*"
    }
  ]
}
```

#### 6. Check Bucket Versioning
```
Bucket → Properties tab → Bucket Versioning
```

**Status**: Enabled (recommended for document retention)

#### 7. Check Lifecycle Rules
```
Bucket → Management tab → Lifecycle rules
```

**Sample Rule**:
- **Name**: `MoveOldDocumentsToGlacier`
- **Actions**:
  - Transition to Glacier after 90 days
  - Delete after 7 years (compliance retention)

### S3 Operations from Application

**File**: `src/ClaimsRagBot.Infrastructure/S3/S3Service.cs`

#### Upload Document
```csharp
var putRequest = new PutObjectRequest
{
    BucketName = "claims-documents-rag-dev",
    Key = $"claims/{userId}/{claimId}/{fileName}",
    InputStream = fileStream,
    ContentType = contentType,
    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
};
await _s3Client.PutObjectAsync(putRequest);
```

#### Generate Presigned URL (for download)
```csharp
var request = new GetPreSignedUrlRequest
{
    BucketName = "claims-documents-rag-dev",
    Key = documentKey,
    Expires = DateTime.UtcNow.AddHours(1)
};
string url = _s3Client.GetPreSignedURL(request);
```

#### Delete Document
```csharp
var deleteRequest = new DeleteObjectRequest
{
    BucketName = "claims-documents-rag-dev",
    Key = documentKey
};
await _s3Client.DeleteObjectAsync(deleteRequest);
```

### S3 Metrics

**CloudWatch Metrics Path**:
```
CloudWatch → Metrics → S3 → Storage Metrics
```

**Key Metrics**:
- `BucketSizeBytes`: Total storage used
- `NumberOfObjects`: Count of objects
- `AllRequests`: Total API requests
- `GetRequests`, `PutRequests`: Operation counts

---

## Amazon Textract

### Overview
Textract extracts text and structured data from uploaded claim documents (PDFs, images).

### Step-by-Step: View Textract in AWS Console

#### 1. Navigate to Textract
```
AWS Console → Services → Search "Textract" → Amazon Textract
```

#### 2. Test Textract (Optional)
```
Left Menu → Analyze document
```

1. Upload sample claim document
2. Select analysis type:
   - **Detect text**: Basic OCR
   - **Analyze document**: Forms, tables, queries
3. Click "Analyze"
4. View extracted text and key-value pairs

#### 3. View Recent Textract Jobs
```
Left Menu → Jobs
```

**What to See**:
- List of recent document analysis jobs
- Status: Succeeded, Failed, In Progress
- Job ID
- Timestamp

**Note**: For synchronous API calls (as used in the app), jobs won't appear here.

### Textract Operations from Application

**File**: `src/ClaimsRagBot.Infrastructure/Textract/TextractService.cs`

#### Detect Text (Synchronous)
```csharp
var request = new DetectDocumentTextRequest
{
    Document = new Document
    {
        S3Object = new Amazon.Textract.Model.S3Object
        {
            Bucket = s3Bucket,
            Name = s3Key
        }
    }
};
var response = await _textractClient.DetectDocumentTextAsync(request);
```

**Response Structure**:
```json
{
  "Blocks": [
    {
      "BlockType": "LINE",
      "Text": "Patient Name: John Doe",
      "Confidence": 99.8
    },
    {
      "BlockType": "LINE",
      "Text": "Hospital Admission: 01/15/2026",
      "Confidence": 98.5
    }
  ]
}
```

#### Analyze Document (Forms, Tables)
```csharp
var request = new AnalyzeDocumentRequest
{
    Document = new Document
    {
        S3Object = new Amazon.Textract.Model.S3Object
        {
            Bucket = s3Bucket,
            Name = s3Key
        }
    },
    FeatureTypes = new List<string> { "FORMS", "TABLES" }
};
var response = await _textractClient.AnalyzeDocumentAsync(request);
```

**Key-Value Pairs Extracted**:
```json
{
  "Key": "Policy Number",
  "Value": "AFLAC-HOSP-2024-001",
  "Confidence": 97.3
}
```

### Textract Metrics

**CloudWatch Metrics Path**:
```
CloudWatch → Metrics → Textract
```

**Key Metrics**:
- `DocumentTextDetectionCount`: Number of OCR operations
- `ResponseTime`: Average processing time
- `UserErrorCount`: 4xx errors
- `ServerErrorCount`: 5xx errors

---

## Amazon Comprehend

### Overview
Comprehend extracts named entities (dates, amounts, policy numbers) from extracted text.

### Step-by-Step: View Comprehend in AWS Console

#### 1. Navigate to Comprehend
```
AWS Console → Services → Search "Comprehend" → Amazon Comprehend
```

#### 2. Test Entity Detection
```
Left Menu → Real-time analysis → Entities
```

1. Paste sample text:
   ```
   Patient admitted on 01/15/2026 for pneumonia.
   Policy Number: AFLAC-HOSP-2024-001
   Claim Amount: $1,500
   ```
2. Select language: English
3. Click "Analyze"

**Expected Entities**:
| Entity | Type | Confidence |
|--------|------|------------|
| `01/15/2026` | DATE | 99% |
| `AFLAC-HOSP-2024-001` | OTHER | 95% |
| `$1,500` | QUANTITY | 98% |
| `pneumonia` | MEDICAL_CONDITION | 92% |

#### 3. View Recent Analysis Jobs
```
Left Menu → Analysis jobs → Entity recognition
```

**Note**: For real-time API calls (as used in the app), jobs won't appear here.

### Comprehend Operations from Application

**File**: `src/ClaimsRagBot.Infrastructure/Comprehend/ComprehendService.cs`

#### Detect Entities
```csharp
var request = new DetectEntitiesRequest
{
    Text = extractedText,
    LanguageCode = "en"
};
var response = await _comprehendClient.DetectEntitiesAsync(request);
```

**Response Structure**:
```json
{
  "Entities": [
    {
      "Text": "01/15/2026",
      "Type": "DATE",
      "Score": 0.99,
      "BeginOffset": 25,
      "EndOffset": 35
    },
    {
      "Text": "$1,500",
      "Type": "QUANTITY",
      "Score": 0.98,
      "BeginOffset": 120,
      "EndOffset": 126
    }
  ]
}
```

### Comprehend Metrics

**CloudWatch Metrics Path**:
```
CloudWatch → Metrics → Comprehend
```

**Key Metrics**:
- `RequestCount`: Total API calls
- `SuccessfulRequestLatency`: Average response time
- `UserErrorCount`: 4xx errors

---

## IAM Roles and Permissions

### Overview
IAM controls access between services and the application.

### Required IAM Policies

#### 1. View IAM Roles
```
AWS Console → Services → IAM → Roles
```

**Search for**: `ClaimsApiRole` or `ClaimsRagBotExecutionRole`

#### 2. Check Role Trust Relationship
```
Roles → Select role → Trust relationships tab
```

**Trust Policy** (allows EC2/Lambda to assume role):
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Service": "ec2.amazonaws.com"
      },
      "Action": "sts:AssumeRole"
    }
  ]
}
```

#### 3. Check Attached Policies
```
Roles → Select role → Permissions tab
```

**Required Policies**:

##### Bedrock Access
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "bedrock:InvokeModel",
        "bedrock:InvokeModelWithResponseStream"
      ],
      "Resource": [
        "arn:aws:bedrock:us-east-1::foundation-model/amazon.titan-embed-text-v1",
        "arn:aws:bedrock:us-east-1::foundation-model/anthropic.claude-3-5-sonnet-20241022-v2:0"
      ]
    }
  ]
}
```

##### OpenSearch Access
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "aoss:*"
      ],
      "Resource": "arn:aws:aoss:us-east-1:123456789012:collection/*"
    }
  ]
}
```

##### DynamoDB Access
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "dynamodb:PutItem",
        "dynamodb:GetItem",
        "dynamodb:UpdateItem",
        "dynamodb:Query",
        "dynamodb:Scan"
      ],
      "Resource": [
        "arn:aws:dynamodb:us-east-1:123456789012:table/ClaimsAuditTrail",
        "arn:aws:dynamodb:us-east-1:123456789012:table/ClaimsAuditTrail/index/*"
      ]
    }
  ]
}
```

##### S3 Access
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:GetObject",
        "s3:PutObject",
        "s3:DeleteObject"
      ],
      "Resource": "arn:aws:s3:::claims-documents-rag-dev/*"
    }
  ]
}
```

##### Textract Access
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "textract:DetectDocumentText",
        "textract:AnalyzeDocument"
      ],
      "Resource": "*"
    }
  ]
}
```

##### Comprehend Access
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "comprehend:DetectEntities",
        "comprehend:DetectKeyPhrases"
      ],
      "Resource": "*"
    }
  ]
}
```

---

## CloudWatch Logs and Monitoring

### Overview
CloudWatch captures logs and metrics from all AWS services and application code.

### Step-by-Step: View CloudWatch Logs

#### 1. Navigate to CloudWatch
```
AWS Console → Services → CloudWatch
```

#### 2. View Log Groups
```
Left Menu → Logs → Log groups
```

**Expected Log Groups**:
- `/aws/bedrock/modelinvocations` - Bedrock API calls
- `/aws/opensearch/claims-policies-dev` - OpenSearch queries
- `/aws/lambda/ClaimsValidationFunction` - Lambda logs (if using Lambda)
- `/ecs/claims-rag-bot-api` - ECS container logs (if using ECS)

#### 3. View Bedrock Logs
```
Log groups → /aws/bedrock/modelinvocations → Log streams
```

**Sample Log Entry**:
```json
{
  "timestamp": "2026-02-01T14:30:00.000Z",
  "modelId": "us.anthropic.claude-3-5-sonnet-20241022-v2:0",
  "operation": "InvokeModel",
  "input": {
    "inputTokenCount": 150
  },
  "output": {
    "outputTokenCount": 85
  }
}
```

#### 4. View Application Logs (if using CloudWatch Logs)
```
Log groups → Select your application log group
```

**Search for Errors**:
1. Click "Search log group"
2. Enter filter: `ERROR` or `Exception`
3. Set time range
4. Click "Search"

#### 5. Create Metric Filters
```
Log groups → Select group → Metric filters tab → Create metric filter
```

**Example: Count API Errors**:
- **Filter pattern**: `[timestamp, request_id, level = ERROR, ...]`
- **Metric namespace**: `ClaimsRagBot`
- **Metric name**: `APIErrors`
- **Metric value**: 1

#### 6. View CloudWatch Metrics
```
Left Menu → Metrics → All metrics
```

**Browse by Service**:
- Bedrock
- DynamoDB
- OpenSearch Service
- S3
- Textract
- Comprehend

**Create Dashboard**:
```
Left Menu → Dashboards → Create dashboard
```

Add widgets for:
- Bedrock invocations
- DynamoDB read/write operations
- OpenSearch query latency
- Application error count

#### 7. Set Up Alarms
```
Left Menu → Alarms → All alarms → Create alarm
```

**Example Alarm: High Error Rate**:
1. Select metric: `ClaimsRagBot/APIErrors`
2. Conditions: `> 10` errors in 5 minutes
3. Notification: SNS topic → Email
4. Name: `ClaimsAPI-HighErrorRate`

---

## Cost Analysis and Budgets

### Step-by-Step: View Costs

#### 1. Navigate to Cost Explorer
```
AWS Console → Top right (Account) → Billing and Cost Management → Cost Explorer
```

#### 2. View Costs by Service
```
Cost Explorer → Group by: Service
```

**Expected Costs** (approximate monthly):
| Service | Usage | Estimated Cost |
|---------|-------|----------------|
| Bedrock (Claude 3.5) | 10,000 requests | $30-50 |
| Bedrock (Titan Embed) | 10,000 embeddings | $1-2 |
| OpenSearch Serverless | 1 OCU-hour | $0.24/hour = $175/month |
| DynamoDB (On-Demand) | 1M reads, 100K writes | $1.25 + $1.25 = $2.50 |
| S3 | 10 GB storage, 1000 requests | $0.23 + $0.01 = $0.24 |
| Textract | 1000 pages | $1.50 |
| Comprehend | 10,000 units | $1.00 |
| **Total** | | **~$210-230/month** |

#### 3. Set Up Budget Alerts
```
Billing → Budgets → Create budget
```

**Budget Configuration**:
1. **Type**: Cost budget
2. **Name**: `ClaimsRagBot-MonthlyBudget`
3. **Amount**: $250/month
4. **Alert threshold**: 80% ($200)
5. **Email**: your-email@example.com

#### 4. Enable Cost Allocation Tags
```
Billing → Cost allocation tags
```

**Recommended Tags**:
- `Application: ClaimsRagBot`
- `Environment: dev/staging/prod`
- `Owner: YourTeam`

Tag resources in CloudFormation template:
```yaml
Tags:
  - Key: Application
    Value: ClaimsRagBot
  - Key: Environment
    Value: dev
```

---

## Troubleshooting Checklist

### 1. Bedrock Issues

**Symptom**: `AccessDeniedException`
```
□ Check model access granted (Console → Bedrock → Model access)
□ Verify IAM role has bedrock:InvokeModel permission
□ Confirm region supports model (us-east-1, us-west-2)
□ Check model ID is correct in code
```

**Symptom**: `ThrottlingException`
```
□ Implement exponential backoff retry logic
□ Request quota increase (Service Quotas console)
□ Reduce concurrent requests
```

### 2. OpenSearch Issues

**Symptom**: Connection timeout
```
□ Check network policy allows access (public or VPC)
□ Verify endpoint URL in appsettings.json
□ Confirm collection status is "Active"
□ Check IAM role has aoss:* permissions
```

**Symptom**: No search results
```
□ Verify index exists: GET /policy-clauses/_count
□ Check documents are indexed: GET /policy-clauses/_search
□ Confirm embedding dimension matches (1536)
□ Test query manually in OpenSearch Dashboards
```

### 3. DynamoDB Issues

**Symptom**: `ResourceNotFoundException`
```
□ Verify table name matches code (ClaimsAuditTrail)
□ Check table exists in correct region
□ Confirm table status is "Active"
```

**Symptom**: `ProvisionedThroughputExceededException`
```
□ Switch to On-Demand billing mode
□ Or increase provisioned capacity
□ Implement retry with exponential backoff
```

**Symptom**: Item not found
```
□ Verify ClaimId is correct (case-sensitive)
□ Check primary key schema matches query
□ Use Scan to list all items and verify ClaimId
```

### 4. S3 Issues

**Symptom**: `AccessDenied`
```
□ Check bucket policy allows IAM role access
□ Verify bucket name in appsettings.json
□ Confirm object key path is correct
□ Check if bucket encryption requires KMS permissions
```

**Symptom**: Object not found
```
□ Verify S3 key path (case-sensitive)
□ Check upload succeeded (look for S3 PutObject log)
□ Confirm bucket region matches application region
```

### 5. Textract Issues

**Symptom**: `InvalidParameterException`
```
□ Verify S3 object exists before Textract call
□ Check file format is supported (PDF, PNG, JPG)
□ Confirm file size is under limit (10 MB for sync)
```

**Symptom**: Low confidence scores
```
□ Improve image quality (resolution, contrast)
□ Rotate image to correct orientation
□ Use AnalyzeDocument instead of DetectDocumentText
```

### 6. General AWS Issues

**Symptom**: Credentials errors
```
□ Check AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY in environment
□ Or verify IAM role is attached to EC2/ECS/Lambda
□ Confirm credentials are not expired
□ Test with AWS CLI: aws sts get-caller-identity
```

**Symptom**: Region mismatches
```
□ Verify all services in same region (us-east-1)
□ Check appsettings.json region configuration
□ Confirm AWS SDK client region matches
```

---

## Quick Reference: AWS CLI Commands

### Bedrock
```bash
# List foundation models
aws bedrock list-foundation-models --region us-east-1

# Test model invocation (example)
aws bedrock-runtime invoke-model \
  --model-id amazon.titan-embed-text-v1 \
  --body '{"inputText":"test"}' \
  --region us-east-1 \
  output.json
```

### OpenSearch Serverless
```bash
# List collections
aws opensearchserverless list-collections --region us-east-1

# Get collection details
aws opensearchserverless get-collection --id <collection-id> --region us-east-1
```

### DynamoDB
```bash
# List tables
aws dynamodb list-tables --region us-east-1

# Describe table
aws dynamodb describe-table --table-name ClaimsAuditTrail --region us-east-1

# Get item by ClaimId
aws dynamodb get-item \
  --table-name ClaimsAuditTrail \
  --key '{"ClaimId":{"S":"your-claim-id"}}' \
  --region us-east-1

# Scan table with filter
aws dynamodb scan \
  --table-name ClaimsAuditTrail \
  --filter-expression "DecisionStatus = :status" \
  --expression-attribute-values '{":status":{"S":"Covered"}}' \
  --region us-east-1
```

### S3
```bash
# List buckets
aws s3 ls

# List objects in bucket
aws s3 ls s3://claims-documents-rag-dev/ --recursive

# Download object
aws s3 cp s3://claims-documents-rag-dev/claims/user/file.pdf ./file.pdf
```

### Textract
```bash
# Detect text from S3 document
aws textract detect-document-text \
  --document '{"S3Object":{"Bucket":"claims-documents-rag-dev","Name":"claims/file.pdf"}}' \
  --region us-east-1
```

### Comprehend
```bash
# Detect entities
aws comprehend detect-entities \
  --text "Patient admitted on 01/15/2026. Policy: AFLAC-HOSP-2024-001" \
  --language-code en \
  --region us-east-1
```

---

## Appendix: Configuration File Mapping

### appsettings.json AWS Configuration

**File**: `src/ClaimsRagBot.Api/appsettings.json`

```json
{
  "AWS": {
    "Region": "us-east-1",
    "AccessKeyId": "",  // Leave empty to use IAM role
    "SecretAccessKey": "",  // Leave empty to use IAM role
    "OpenSearchEndpoint": "https://xxxxxxx.us-east-1.aoss.amazonaws.com",
    "OpenSearchIndexName": "policy-clauses",
    "S3": {
      "DocumentBucket": "claims-documents-rag-dev",
      "UploadPrefix": "uploads/",
      "ProcessedPrefix": "processed/"
    }
  }
}
```

**How to Find Values**:
| Setting | AWS Console Path |
|---------|------------------|
| `OpenSearchEndpoint` | OpenSearch Service → Serverless → Collections → Endpoint |
| `DocumentBucket` | S3 → Buckets → Bucket name |

---

**End of AWS Services Configuration Guide**

This guide provides complete visibility into all AWS services used by the Claims RAG Bot. Use this as a reference for troubleshooting, monitoring, and understanding the cloud infrastructure supporting your application.
