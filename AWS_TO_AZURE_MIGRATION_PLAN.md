# AWS to Azure Migration Plan - Claims RAG Bot
## Comprehensive Migration Strategy & Service Mapping

**Generated Date:** February 5, 2026  
**Current Cloud Provider:** Amazon Web Services (AWS)  
**Target Cloud Provider:** Microsoft Azure  
**Application:** Claims Validation RAG Bot with Document Processing

---

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Current Application Architecture](#current-application-architecture)
3. [AWS Services Inventory & Implementation Details](#aws-services-inventory--implementation-details)
4. [AWS to Azure Service Mapping](#aws-to-azure-service-mapping)
5. [Detailed Migration Roadmap](#detailed-migration-roadmap)
6. [Code Changes Required](#code-changes-required)
7. [Configuration Changes](#configuration-changes)
8. [Testing Strategy](#testing-strategy)
9. [Risk Assessment](#risk-assessment)
10. [Cost Comparison](#cost-comparison)

---

## Executive Summary

### Current State
The Claims RAG Bot is a production-grade AI-powered claims validation system built on AWS using:
- **7 AWS AI/ML Services** (Bedrock, OpenSearch, Textract, Comprehend, Rekognition, DynamoDB, S3)
- **RAG Architecture** for policy-aware claim validation
- **.NET 10.0** backend with Clean Architecture
- **Angular 18** frontend with Material UI
- **10 NuGet packages** for AWS SDK integration

### Migration Scope
- **Backend Services:** 10 C# service implementations
- **Infrastructure:** Vector database, object storage, NoSQL database, AI/ML services
- **Configuration:** appsettings.json, authentication, endpoints
- **Frontend:** No changes required (API contract remains same)
- **Estimated Effort:** 40-60 hours of development + 20 hours testing

---

## Current Application Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     ANGULAR 18 FRONTEND                         │
│  (Claims Chatbot UI - Material Design)                          │
│  - ChatComponent, ClaimFormComponent, DocumentUploadComponent   │
└────────────────────────┬────────────────────────────────────────┘
                         │ HTTP/REST API
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│               ASP.NET CORE 10.0 WEB API                         │
│  Controllers: ClaimsController, DocumentsController             │
│  Endpoints: /claims/validate, /documents/upload, /documents/extract │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│           APPLICATION LAYER (RAG Orchestration)                 │
│  ClaimValidationOrchestrator - Coordinates 4-step validation:   │
│  1. Generate Embedding → 2. Retrieve Clauses → 3. LLM Analysis │
│  4. Apply Business Rules → 5. Audit Trail                       │
└─────┬──────────────┬──────────────┬──────────────┬─────────────┘
      │              │              │              │
      ▼              ▼              ▼              ▼
┌─────────────────────────────────────────────────────────────────┐
│                  INFRASTRUCTURE LAYER                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ Bedrock      │  │ OpenSearch   │  │ DynamoDB     │          │
│  │ - Embeddings │  │ - Vector DB  │  │ - Audit Trail│          │
│  │ - LLM Claude │  │ - RAG Search │  │ - NoSQL      │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ S3           │  │ Textract     │  │ Comprehend   │          │
│  │ - Documents  │  │ - OCR        │  │ - NLP        │          │
│  │ - Presigned  │  │ - Forms/Tables│ │ - Entities   │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
│                                                                  │
│  ┌──────────────┐                                               │
│  │ Rekognition  │                                               │
│  │ - Image      │                                               │
│  │ - Analysis   │                                               │
│  └──────────────┘                                               │
└─────────────────────────────────────────────────────────────────┘
```

### Technology Stack

| Layer | Technology | Purpose |
|-------|------------|---------|
| **Frontend** | Angular 18 + Material UI | User interface for claim submission |
| **API** | ASP.NET Core 10.0 Web API | RESTful endpoints |
| **Application** | Clean Architecture | Business logic orchestration |
| **Infrastructure** | AWS SDKs (C#) | Cloud service integration |
| **AI/ML** | AWS Bedrock (Claude 3.5, Titan) | LLM & embeddings |
| **Vector DB** | Amazon OpenSearch Serverless | RAG retrieval |
| **Storage** | Amazon S3 | Document storage |
| **Database** | Amazon DynamoDB | Audit trail |
| **OCR** | Amazon Textract | Document extraction |
| **NLP** | Amazon Comprehend | Entity recognition |
| **Vision** | Amazon Rekognition | Image analysis |

### Project Structure

```
ClaimsRagBot/
├── src/
│   ├── ClaimsRagBot.Api/              # Web API (Controllers, Program.cs)
│   ├── ClaimsRagBot.Application/      # RAG Orchestration
│   │   └── RAG/
│   │       └── ClaimValidationOrchestrator.cs
│   ├── ClaimsRagBot.Core/             # Domain Models & Interfaces
│   │   ├── Interfaces/
│   │   │   ├── IEmbeddingService.cs
│   │   │   ├── ILlmService.cs
│   │   │   ├── IRetrievalService.cs
│   │   │   ├── IAuditService.cs
│   │   │   ├── ITextractService.cs
│   │   │   ├── IComprehendService.cs
│   │   │   ├── IRekognitionService.cs
│   │   │   ├── IDocumentUploadService.cs
│   │   │   └── IDocumentExtractionService.cs
│   │   └── Models/
│   │       ├── ClaimRequest.cs
│   │       ├── ClaimDecision.cs
│   │       ├── PolicyClause.cs
│   │       └── DocumentExtractionResult.cs
│   └── ClaimsRagBot.Infrastructure/   # AWS Implementations
│       ├── Bedrock/
│       │   ├── EmbeddingService.cs    # Titan Text Embeddings V2
│       │   └── LlmService.cs          # Claude 3.5 Sonnet
│       ├── OpenSearch/
│       │   └── RetrievalService.cs    # Vector search with SigV4
│       ├── DynamoDB/
│       │   └── AuditService.cs        # Claim audit trail
│       ├── S3/
│       │   └── DocumentUploadService.cs # Document storage
│       ├── Textract/
│       │   └── TextractService.cs     # OCR extraction
│       ├── Comprehend/
│       │   └── ComprehendService.cs   # NLP entity extraction
│       ├── Rekognition/
│       │   └── RekognitionService.cs  # Image analysis
│       ├── DocumentExtraction/
│       │   └── DocumentExtractionOrchestrator.cs
│       └── Tools/
│           └── PolicyIngestionService.cs # OpenSearch data loader
├── tools/
│   └── PolicyIngestion/               # CLI tool for policy upload
│       └── Program.cs
└── claims-chatbot-ui/                 # Angular frontend
    └── src/app/
        ├── components/
        │   ├── chat/
        │   ├── claim-form/
        │   ├── claim-result/
        │   └── document-upload/
        ├── services/
        │   └── claims-api.service.ts
        └── models/
            └── claim.model.ts
```

---

## AWS Services Inventory & Implementation Details

### 1. Amazon Bedrock (AI Foundation Models)

**Purpose:** LLM inference and text embeddings for RAG architecture

#### Models Used:
1. **Claude 3.5 Sonnet** (`anthropic.claude-3-5-sonnet-20241022-v2:0`)
   - Use Case: Claim validation reasoning
   - Context Window: 200K tokens
   - Input: Claim details + retrieved policy clauses
   - Output: Coverage decision with explanation

2. **Titan Text Embeddings V2** (`amazon.titan-embed-text-v2:0`)
   - Use Case: Convert text to 1024-dimensional vectors
   - Input: Claim descriptions, policy clauses
   - Output: Float array embeddings for vector search

#### Implementation Files:
- **EmbeddingService.cs** (62 lines)
  - NuGet: `AWSSDK.BedrockRuntime` v4.0.14.6
  - Client: `AmazonBedrockRuntimeClient`
  - Method: `InvokeModelAsync()` with JSON body
  - Auth: AWS credentials or default credential chain

```csharp
// Current Implementation Pattern
var config = new AmazonBedrockRuntimeConfig { RegionEndpoint = regionEndpoint };
_client = new AmazonBedrockRuntimeClient(credentials, config);

var request = new InvokeModelRequest
{
    ModelId = "amazon.titan-embed-text-v2:0",
    Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody))
};
var response = await _client.InvokeModelAsync(request);
```

- **LlmService.cs** (96 lines)
  - Model: Claude 3.5 Sonnet
  - Input: System prompt + user prompt with claim context
  - Output: Structured JSON decision
  - Error Handling: `AmazonBedrockRuntimeException`

```csharp
// Claude Invocation Pattern
var claudeRequest = new
{
    anthropic_version = "bedrock-2023-05-31",
    max_tokens = 4096,
    messages = new[]
    {
        new { role = "user", content = prompt }
    },
    system = systemPrompt
};
```

#### Configuration:
```json
"AWS": {
  "Region": "us-east-1",
  "AccessKeyId": "AKIA...",
  "SecretAccessKey": "..."
}
```

#### Cost Model:
- Titan Embeddings: $0.0001 per 1K tokens input
- Claude 3.5 Sonnet: $0.003 per 1K tokens input, $0.015 per 1K tokens output
- Typical claim validation: ~5K tokens total → $0.08 per claim

---

### 2. Amazon OpenSearch Serverless (Vector Database)

**Purpose:** Semantic search of policy clauses using vector embeddings

#### Implementation:
- **RetrievalService.cs** (253 lines)
  - NuGet: `AWSSDK.OpenSearchServerless` v4.0.6.1, `OpenSearch.Client` v1.8.0
  - Index Name: `policy-clauses`
  - Vector Dimensions: 1024 (matches Titan embeddings)
  - Search Algorithm: K-Nearest Neighbors (KNN)
  - Authentication: AWS SigV4 request signing

```csharp
// OpenSearch KNN Query Pattern
var requestBody = new
{
    size = maxResults,
    query = new
    {
        knn = new
        {
            Embedding = new
            {
                vector = embedding,
                k = maxResults
            }
        }
    }
};

var requestUri = $"{_opensearchEndpoint}/{_indexName}/_search";
// Sign request with SigV4
var signedRequest = SignRequest(httpRequest, "aoss", region);
```

#### Index Schema:
```json
{
  "mappings": {
    "properties": {
      "ClauseId": { "type": "keyword" },
      "PolicyType": { "type": "keyword" },
      "ClauseText": { "type": "text" },
      "Category": { "type": "keyword" },
      "Embedding": {
        "type": "knn_vector",
        "dimension": 1024
      }
    }
  }
}
```

#### Configuration:
```json
"AWS": {
  "OpenSearchEndpoint": "https://xt8zn6h5untpetqggo24.us-east-1.aoss.amazonaws.com",
  "OpenSearchIndexName": "policy-clauses"
}
```

#### Data Ingestion:
- **PolicyIngestionService.cs** (155 lines)
  - Loads 35 sample insurance policy clauses
  - Generates embeddings via Bedrock
  - Indexes documents in OpenSearch with vectors
  - CLI tool: `tools/PolicyIngestion/Program.cs`

```bash
# Usage
dotnet run -- <opensearch-endpoint> [index-name]
```

#### Fallback Mechanism:
- If OpenSearch endpoint not configured → returns mock policy clauses
- Allows local development without infrastructure

#### Cost Model:
- OpenSearch Serverless: Minimum 2 OCU × $0.24/hour = ~$350/month
- Storage: $0.024 per GB-month

---

### 3. Amazon DynamoDB (NoSQL Database)

**Purpose:** Store claim audit trail for compliance and analytics

#### Implementation:
- **AuditService.cs** (78 lines)
  - NuGet: `AWSSDK.DynamoDBv2` v4.0.10.8
  - Client: `AmazonDynamoDBClient`
  - Table Name: `ClaimsAuditTrail`
  - Primary Key: `ClaimId` (String)
  - Billing Mode: On-demand (PAY_PER_REQUEST)

```csharp
// DynamoDB PutItem Pattern
var item = new Dictionary<string, AttributeValue>
{
    ["ClaimId"] = new AttributeValue { S = Guid.NewGuid().ToString() },
    ["PolicyNumber"] = new AttributeValue { S = request.PolicyNumber },
    ["ClaimAmount"] = new AttributeValue { N = request.ClaimAmount.ToString() },
    ["DecisionStatus"] = new AttributeValue { S = decision.Status },
    ["Timestamp"] = new AttributeValue { S = DateTime.UtcNow.ToString("o") },
    ["RetrievedClauses"] = new AttributeValue { S = JsonSerializer.Serialize(clauses) }
};

var putRequest = new PutItemRequest
{
    TableName = _tableName,
    Item = item
};
await _client.PutItemAsync(putRequest);
```

#### Table Schema:
```
ClaimId (PK)           : String (GUID)
PolicyNumber           : String
PolicyType             : String
ClaimAmount            : Number
ClaimDescription       : String
DecisionStatus         : String (Covered/Not Covered/Manual Review)
DecisionExplanation    : String
ConfidenceScore        : Number
RetrievedClauses       : String (JSON array)
ClauseReferences       : String (JSON array)
RequiredDocuments      : String (JSON array)
Timestamp              : String (ISO 8601)
ProcessingTimeMs       : Number
```

#### Configuration:
```json
"AWS": {
  "DynamoDB": {
    "TableName": "ClaimsAuditTrail",
    "ReadCapacity": 5,
    "WriteCapacity": 5
  }
}
```

#### Cost Model:
- On-demand pricing: $1.25 per million write requests
- Storage: $0.25 per GB-month
- Typical usage: ~500 claims/month → $0.63/month

---

### 4. Amazon S3 (Object Storage)

**Purpose:** Store uploaded claim documents (PDFs, images)

#### Implementation:
- **DocumentUploadService.cs** (142 lines)
  - NuGet: `AWSSDK.S3` v4.0.0
  - Client: `AmazonS3Client`
  - Bucket: `claims-documents-rag-dev`
  - Features: Presigned URLs, server-side encryption

```csharp
// S3 Upload Pattern
var putRequest = new PutObjectRequest
{
    BucketName = _bucketName,
    Key = $"{_uploadPrefix}{userId}/{fileName}",
    InputStream = fileStream,
    ContentType = contentType,
    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
};
await _s3Client.PutObjectAsync(putRequest);

// Generate Presigned URL (for frontend download)
var urlRequest = new GetPreSignedUrlRequest
{
    BucketName = _bucketName,
    Key = s3Key,
    Expires = DateTime.UtcNow.AddSeconds(_presignedUrlExpiration)
};
string presignedUrl = _s3Client.GetPreSignedURL(urlRequest);
```

#### Bucket Structure:
```
claims-documents-rag-dev/
├── uploads/
│   └── {userId}/
│       └── {fileName}
├── processed/
│   └── {documentId}/
│       ├── extracted-text.json
│       └── entities.json
└── policies/
    └── {policyId}/
        └── policy-document.pdf
```

#### Configuration:
```json
"AWS": {
  "S3": {
    "DocumentBucket": "claims-documents-rag-dev",
    "UploadPrefix": "uploads/",
    "ProcessedPrefix": "processed/",
    "PresignedUrlExpiration": 3600
  }
}
```

#### Cost Model:
- Storage: $0.023 per GB-month (Standard)
- PUT requests: $0.005 per 1,000 requests
- GET requests: $0.0004 per 1,000 requests
- Typical usage: 100 documents/month (500 MB) → $1.50/month

---

### 5. Amazon Textract (Document OCR)

**Purpose:** Extract text, forms, and tables from claim documents

#### Implementation:
- **TextractService.cs** (324 lines)
  - NuGet: `AWSSDK.Textract` v4.0.0
  - Client: `AmazonTextractClient`
  - Operations: `DetectDocumentText`, `AnalyzeDocument`

```csharp
// Textract Analyze Document Pattern
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

// Parse blocks (LINE, KEY_VALUE_SET, TABLE, CELL)
foreach (var block in response.Blocks)
{
    if (block.BlockType == "LINE")
    {
        extractedText.AppendLine(block.Text);
    }
    else if (block.BlockType == "KEY_VALUE_SET" && block.EntityTypes.Contains("KEY"))
    {
        // Extract form fields
    }
}
```

#### Extraction Capabilities:
- **Text Detection:** OCR for printed/handwritten text
- **Form Extraction:** Key-value pairs (e.g., "Policy Number: AFLAC-001")
- **Table Extraction:** Structured data from tables
- **Confidence Scores:** Per-block accuracy metrics

#### Configuration:
```json
"AWS": {
  "Textract": {
    "Enabled": true,
    "MaxPages": 50,
    "PollingIntervalMs": 5000,
    "MaxPollingAttempts": 60
  }
}
```

#### Cost Model:
- DetectDocumentText: $1.50 per 1,000 pages
- AnalyzeDocument (Forms/Tables): $50 per 1,000 pages
- Typical usage: 100 pages/month → $5.15/month

---

### 6. Amazon Comprehend (NLP)

**Purpose:** Extract named entities (dates, amounts, policy numbers) from text

#### Implementation:
- **ComprehendService.cs** (131 lines)
  - NuGet: `AWSSDK.Comprehend` v4.0.0
  - Client: `AmazonComprehendClient`
  - Operations: `DetectEntities`, `DetectKeyPhrases`

```csharp
// Comprehend Entity Detection Pattern
var request = new DetectEntitiesRequest
{
    Text = extractedText,
    LanguageCode = "en"
};
var response = await _client.DetectEntitiesAsync(request);

// Process entities
foreach (var entity in response.Entities)
{
    // Entity types: PERSON, DATE, QUANTITY, LOCATION, ORGANIZATION, etc.
    if (entity.Type == "DATE")
    {
        dates.Add(entity.Text);
    }
    else if (entity.Type == "QUANTITY" && entity.Text.Contains("$"))
    {
        amounts.Add(entity.Text);
    }
}
```

#### Entity Types Detected:
- **DATE:** Claim dates, admission dates
- **QUANTITY:** Claim amounts ($1,500)
- **PERSON:** Patient names
- **ORGANIZATION:** Hospitals, providers
- **OTHER:** Policy numbers (custom pattern)

#### Configuration:
```json
"AWS": {
  "Comprehend": {
    "Enabled": true,
    "CustomEntityRecognizerArn": "",
    "LanguageCode": "en"
  }
}
```

#### Cost Model:
- Entity detection: $0.0001 per unit (100 characters)
- Typical usage: 1,000 documents × 1,000 chars → $1.00/month

---

### 7. Amazon Rekognition (Image Analysis)

**Purpose:** Analyze images in claim documents (photos of damage, medical images)

#### Implementation:
- **RekognitionService.cs** (89 lines)
  - NuGet: `AWSSDK.Rekognition` v4.0.0
  - Client: `AmazonRekognitionClient`
  - Operations: `DetectLabels`, `DetectText`

```csharp
// Rekognition Label Detection Pattern
var request = new DetectLabelsRequest
{
    Image = new Image
    {
        S3Object = new Amazon.Rekognition.Model.S3Object
        {
            Bucket = s3Bucket,
            Name = s3Key
        }
    },
    MaxLabels = 10,
    MinConfidence = 70.0f
};
var response = await _client.DetectLabelsAsync(request);

// Process detected labels
foreach (var label in response.Labels)
{
    // Examples: "Car", "Damage", "Medical Equipment", "Hospital"
    Console.WriteLine($"{label.Name} (Confidence: {label.Confidence}%)");
}
```

#### Configuration:
```json
"AWS": {
  "Rekognition": {
    "Enabled": true,
    "CustomModelArn": "",
    "MinConfidence": 70.0
  }
}
```

#### Cost Model:
- Label detection: $1.00 per 1,000 images
- Text detection: $1.50 per 1,000 images
- Typical usage: 50 images/month → $0.13/month

---

### 8. DocumentExtractionOrchestrator

**Purpose:** Coordinate multi-service document processing pipeline

#### Implementation:
- **DocumentExtractionOrchestrator.cs** (530 lines)
  - Orchestrates: S3 → Textract → Comprehend → Rekognition → LLM
  - Combines outputs into unified `ClaimExtractionResult`

```csharp
// Extraction Pipeline
public async Task<ClaimExtractionResult> ExtractFromDocumentAsync(
    string documentId, DocumentType documentType)
{
    // Step 1: Get document from S3
    var document = await _documentUploadService.GetDocumentMetadataAsync(documentId);
    
    // Step 2: Extract text with Textract
    var textractResult = await _textractService.ExtractTextAsync(
        document.S3Bucket, document.S3Key);
    
    // Step 3: Extract entities with Comprehend
    var comprehendResult = await _comprehendService.ExtractEntitiesAsync(
        textractResult.ExtractedText);
    
    // Step 4: Analyze images with Rekognition (if applicable)
    var imageAnalysis = await _rekognitionService.AnalyzeImageAsync(
        document.S3Bucket, document.S3Key);
    
    // Step 5: LLM-based intelligent field extraction
    var llmExtraction = await _llmService.ExtractClaimFieldsAsync(
        textractResult, comprehendResult);
    
    // Step 6: Combine results
    return new ClaimExtractionResult
    {
        DocumentId = documentId,
        PolicyNumber = llmExtraction.PolicyNumber,
        ClaimAmount = llmExtraction.ClaimAmount,
        ClaimDate = llmExtraction.ClaimDate,
        // ... other fields
    };
}
```

---

### 9. PolicyIngestion CLI Tool

**Purpose:** Load sample insurance policy clauses into OpenSearch

#### Implementation:
- **Program.cs** (100 lines in `tools/PolicyIngestion`)
- Process:
  1. Read policy clauses from hardcoded data
  2. Generate embeddings for each clause (Bedrock)
  3. Create OpenSearch index
  4. Index documents with vectors

```bash
# Usage
cd tools/PolicyIngestion
dotnet run -- https://xt8zn6h5untpetqggo24.us-east-1.aoss.amazonaws.com policy-clauses
```

---

### AWS SDK NuGet Packages Used

```xml
<PackageReference Include="AWSSDK.BedrockRuntime" Version="4.0.14.6" />
<PackageReference Include="AWSSDK.Core" Version="4.0.3.12" />
<PackageReference Include="AWSSDK.DynamoDBv2" Version="4.0.10.8" />
<PackageReference Include="AWSSDK.Extensions.NETCore.Setup" Version="4.0.3.21" />
<PackageReference Include="AWSSDK.OpenSearchServerless" Version="4.0.6.1" />
<PackageReference Include="AWSSDK.S3" Version="4.0.0" />
<PackageReference Include="AWSSDK.Textract" Version="4.0.0" />
<PackageReference Include="AWSSDK.Comprehend" Version="4.0.0" />
<PackageReference Include="AWSSDK.Rekognition" Version="4.0.0" />
<PackageReference Include="OpenSearch.Client" Version="1.8.0" />
<PackageReference Include="OpenSearch.Net.Auth.AwsSigV4" Version="1.8.0" />
```

**Total:** 11 AWS-specific NuGet packages

---

## AWS to Azure Service Mapping

### Service Equivalence Matrix

| AWS Service | Azure Equivalent | Capability Match | Migration Complexity |
|------------|------------------|------------------|---------------------|
| **Amazon Bedrock** | **Azure OpenAI Service** | 95% | Medium |
| **Amazon OpenSearch Serverless** | **Azure AI Search** | 90% | High |
| **Amazon DynamoDB** | **Azure Cosmos DB** | 100% | Low |
| **Amazon S3** | **Azure Blob Storage** | 100% | Low |
| **Amazon Textract** | **Azure Document Intelligence** | 95% | Medium |
| **Amazon Comprehend** | **Azure Language Service** | 90% | Medium |
| **Amazon Rekognition** | **Azure Computer Vision** | 95% | Medium |

---

### 1. Amazon Bedrock → Azure OpenAI Service

#### Azure Equivalent Models:

| AWS Bedrock Model | Azure OpenAI Model | Notes |
|------------------|-------------------|-------|
| Claude 3.5 Sonnet | GPT-4 Turbo | Similar reasoning capabilities |
| Titan Text Embeddings V2 (1024d) | text-embedding-ada-002 (1536d) | Different dimensions |

#### Key Differences:

**Embeddings:**
- AWS: 1024 dimensions
- Azure: 1536 dimensions (ada-002) or 3072 (text-embedding-3-large)
- **Impact:** OpenSearch index must be recreated with new dimension size

**API Pattern:**
```csharp
// AWS Bedrock
var request = new InvokeModelRequest
{
    ModelId = "amazon.titan-embed-text-v2:0",
    Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody))
};

// Azure OpenAI
var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
var embeddings = await client.GetEmbeddingsAsync(
    new EmbeddingsOptions("text-embedding-ada-002", new[] { text })
);
```

**LLM Inference:**
```csharp
// AWS Claude
var claudeRequest = new
{
    anthropic_version = "bedrock-2023-05-31",
    max_tokens = 4096,
    messages = new[] { new { role = "user", content = prompt } }
};

// Azure GPT-4
var chatCompletions = await client.GetChatCompletionsAsync(
    new ChatCompletionsOptions
    {
        DeploymentName = "gpt-4-turbo",
        Messages =
        {
            new ChatRequestSystemMessage(systemPrompt),
            new ChatRequestUserMessage(userPrompt)
        },
        MaxTokens = 4096
    }
);
```

#### Azure NuGet Package:
```xml
<PackageReference Include="Azure.AI.OpenAI" Version="1.0.0-beta.14" />
```

#### Configuration Changes:
```json
// Before (AWS)
"AWS": {
  "Region": "us-east-1"
}

// After (Azure)
"Azure": {
  "OpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "EmbeddingDeployment": "text-embedding-ada-002",
    "ChatDeployment": "gpt-4-turbo"
  }
}
```

#### Migration Complexity: **Medium**
- Reason: Different API patterns, embedding dimensions change

---

### 2. Amazon OpenSearch Serverless → Azure AI Search

#### Azure AI Search Features:

| Feature | AWS OpenSearch | Azure AI Search | Notes |
|---------|---------------|-----------------|-------|
| Vector Search | KNN (k-Nearest Neighbors) | Vector search with HNSW | Both support semantic search |
| Dimensions | Configurable | 1-3072 | Must match Azure OpenAI embeddings |
| Index Structure | Mapping-based | Schema-based | Similar concepts |
| Authentication | AWS SigV4 | API Key or Azure AD | Simpler auth model |

#### API Pattern Comparison:

```csharp
// AWS OpenSearch - Manual HTTP + SigV4
var requestUri = $"{_opensearchEndpoint}/{_indexName}/_search";
var requestBody = new
{
    size = maxResults,
    query = new
    {
        knn = new
        {
            Embedding = new { vector = embedding, k = maxResults }
        }
    }
};
// Manual SigV4 signing required
var signedRequest = SignRequest(httpRequest, "aoss", region);

// Azure AI Search - Native SDK
var searchClient = new SearchClient(
    new Uri(searchEndpoint),
    indexName,
    new AzureKeyCredential(apiKey)
);

var searchOptions = new SearchOptions
{
    VectorSearch = new()
    {
        Queries = 
        { 
            new VectorizedQuery(embedding.ToArray())
            {
                KNearestNeighborsCount = maxResults,
                Fields = { "Embedding" }
            }
        }
    }
};
var results = await searchClient.SearchAsync<PolicyClause>(null, searchOptions);
```

#### Index Schema:

```csharp
// Azure AI Search Index Definition
var index = new SearchIndex(indexName)
{
    Fields =
    {
        new SimpleField("ClauseId", SearchFieldDataType.String) { IsKey = true },
        new SearchableField("ClauseText"),
        new SimpleField("PolicyType", SearchFieldDataType.String) { IsFilterable = true },
        new VectorSearchField("Embedding", 1536, "vector-profile") // Note: 1536 for ada-002
    },
    VectorSearch = new()
    {
        Profiles =
        {
            new VectorSearchProfile("vector-profile", "hnsw-config")
        },
        Algorithms =
        {
            new HnswAlgorithmConfiguration("hnsw-config")
        }
    }
};
```

#### Azure NuGet Package:
```xml
<PackageReference Include="Azure.Search.Documents" Version="11.5.1" />
```

#### Configuration:
```json
"Azure": {
  "AISearch": {
    "Endpoint": "https://your-search-service.search.windows.net",
    "AdminApiKey": "your-admin-key",
    "QueryApiKey": "your-query-key",
    "IndexName": "policy-clauses"
  }
}
```

#### Migration Complexity: **High**
- Reason: Different query syntax, no SigV4 signing, index must be recreated with new embedding dimensions

---

### 3. Amazon DynamoDB → Azure Cosmos DB

#### Azure Cosmos DB for NoSQL:

| Feature | DynamoDB | Cosmos DB | Migration Complexity |
|---------|----------|-----------|---------------------|
| API | Proprietary | NoSQL (JSON) | Low (similar concepts) |
| Partition Key | Hash key | Partition key | Same concept |
| Data Model | Key-value | Document-based | Similar |
| Consistency | Eventually consistent | Tunable (5 levels) | More options |

#### API Pattern:

```csharp
// AWS DynamoDB
var putRequest = new PutItemRequest
{
    TableName = "ClaimsAuditTrail",
    Item = new Dictionary<string, AttributeValue>
    {
        ["ClaimId"] = new AttributeValue { S = claimId },
        ["PolicyNumber"] = new AttributeValue { S = policyNumber },
        ["ClaimAmount"] = new AttributeValue { N = amount.ToString() }
    }
};
await _client.PutItemAsync(putRequest);

// Azure Cosmos DB
var container = _cosmosClient.GetContainer(databaseId, containerId);
var auditRecord = new ClaimAuditRecord
{
    id = claimId,
    PolicyNumber = policyNumber,
    ClaimAmount = amount
};
await container.CreateItemAsync(auditRecord, new PartitionKey(policyNumber));
```

#### Azure NuGet Package:
```xml
<PackageReference Include="Microsoft.Azure.Cosmos" Version="3.38.1" />
```

#### Configuration:
```json
"Azure": {
  "CosmosDB": {
    "Endpoint": "https://your-account.documents.azure.com:443/",
    "Key": "your-key",
    "DatabaseId": "ClaimsDatabase",
    "ContainerId": "AuditTrail"
  }
}
```

#### Migration Complexity: **Low**
- Reason: Very similar document model, straightforward SDK

---

### 4. Amazon S3 → Azure Blob Storage

#### Blob Storage Features:

| Feature | S3 | Azure Blob Storage | Notes |
|---------|----|--------------------|-------|
| Containers | Buckets | Containers | Same concept |
| Objects | Objects | Blobs | Same concept |
| Presigned URLs | ✓ | Shared Access Signatures (SAS) | Same functionality |
| Encryption | SSE-S3 | Server-side encryption | Both support |

#### API Pattern:

```csharp
// AWS S3
var putRequest = new PutObjectRequest
{
    BucketName = "claims-documents",
    Key = $"uploads/{userId}/{fileName}",
    InputStream = fileStream,
    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
};
await _s3Client.PutObjectAsync(putRequest);

// Azure Blob Storage
var blobServiceClient = new BlobServiceClient(connectionString);
var containerClient = blobServiceClient.GetBlobContainerClient("claims-documents");
var blobClient = containerClient.GetBlobClient($"uploads/{userId}/{fileName}");
await blobClient.UploadAsync(fileStream, new BlobUploadOptions
{
    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
});
```

#### SAS Token (Presigned URL equivalent):

```csharp
// Generate SAS token for download
var sasBuilder = new BlobSasBuilder
{
    BlobContainerName = containerName,
    BlobName = blobName,
    Resource = "b",
    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
};
sasBuilder.SetPermissions(BlobSasPermissions.Read);
var sasToken = blobClient.GenerateSasUri(sasBuilder);
```

#### Azure NuGet Package:
```xml
<PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />
```

#### Configuration:
```json
"Azure": {
  "BlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...",
    "ContainerName": "claims-documents",
    "UploadPrefix": "uploads/",
    "SasTokenExpiration": 3600
  }
}
```

#### Migration Complexity: **Low**
- Reason: Nearly identical concepts and workflow

---

### 5. Amazon Textract → Azure Document Intelligence

#### Document Intelligence Features:

| Feature | Textract | Document Intelligence | Notes |
|---------|----------|---------------------|-------|
| OCR | ✓ | ✓ | Both excellent |
| Forms | ✓ | ✓ (Prebuilt models) | Azure has domain-specific models |
| Tables | ✓ | ✓ | Similar capabilities |
| Custom Models | ✓ | ✓ | Azure has more prebuilt options |

#### API Pattern:

```csharp
// AWS Textract
var request = new AnalyzeDocumentRequest
{
    Document = new Document
    {
        S3Object = new Amazon.Textract.Model.S3Object
        {
            Bucket = bucket,
            Name = key
        }
    },
    FeatureTypes = new List<string> { "FORMS", "TABLES" }
};
var response = await _textractClient.AnalyzeDocumentAsync(request);

// Azure Document Intelligence
var client = new DocumentAnalysisClient(
    new Uri(endpoint),
    new AzureKeyCredential(apiKey)
);

var operation = await client.AnalyzeDocumentFromUriAsync(
    WaitUntil.Completed,
    "prebuilt-document", // or "prebuilt-invoice", "prebuilt-receipt"
    new Uri(blobSasUri)
);

var result = operation.Value;
foreach (var page in result.Pages)
{
    foreach (var line in page.Lines)
    {
        Console.WriteLine(line.Content);
    }
}
```

#### Azure NuGet Package:
```xml
<PackageReference Include="Azure.AI.FormRecognizer" Version="4.1.0" />
```

#### Configuration:
```json
"Azure": {
  "DocumentIntelligence": {
    "Endpoint": "https://your-resource.cognitiveservices.azure.com/",
    "ApiKey": "your-key",
    "ModelId": "prebuilt-document"
  }
}
```

#### Migration Complexity: **Medium**
- Reason: Different response structure, Azure has prebuilt models that may simplify code

---

### 6. Amazon Comprehend → Azure Language Service

#### Language Service Features:

| Feature | Comprehend | Azure Language | Notes |
|---------|-----------|----------------|-------|
| Entity Recognition | ✓ | ✓ | Similar quality |
| Key Phrases | ✓ | ✓ | Both support |
| Sentiment | ✓ | ✓ | Not used in current app |
| Custom Models | ✓ | ✓ | Both support training |

#### API Pattern:

```csharp
// AWS Comprehend
var request = new DetectEntitiesRequest
{
    Text = text,
    LanguageCode = "en"
};
var response = await _client.DetectEntitiesAsync(request);

// Azure Language Service
var client = new TextAnalyticsClient(
    new Uri(endpoint),
    new AzureKeyCredential(apiKey)
);

var response = await client.RecognizeEntitiesAsync(text);
foreach (var entity in response.Value)
{
    Console.WriteLine($"{entity.Text} ({entity.Category})");
}
```

#### Entity Category Mapping:

| Comprehend | Azure Language | Notes |
|-----------|----------------|-------|
| PERSON | Person | Direct match |
| DATE | DateTime | Direct match |
| QUANTITY | Quantity | Direct match |
| ORGANIZATION | Organization | Direct match |
| OTHER | Custom | May need custom NER model |

#### Azure NuGet Package:
```xml
<PackageReference Include="Azure.AI.TextAnalytics" Version="5.3.0" />
```

#### Configuration:
```json
"Azure": {
  "LanguageService": {
    "Endpoint": "https://your-resource.cognitiveservices.azure.com/",
    "ApiKey": "your-key"
  }
}
```

#### Migration Complexity: **Medium**
- Reason: Different entity categories, may require entity mapping logic

---

### 7. Amazon Rekognition → Azure Computer Vision

#### Computer Vision Features:

| Feature | Rekognition | Computer Vision | Notes |
|---------|------------|-----------------|-------|
| Object Detection | ✓ | ✓ | Similar |
| OCR | ✓ | ✓ | Azure has Read API |
| Face Detection | ✓ | ✓ | Both excellent |
| Custom Models | ✓ | ✓ (Custom Vision) | Separate service in Azure |

#### API Pattern:

```csharp
// AWS Rekognition
var request = new DetectLabelsRequest
{
    Image = new Image
    {
        S3Object = new Amazon.Rekognition.Model.S3Object
        {
            Bucket = bucket,
            Name = key
        }
    },
    MinConfidence = 70.0f
};
var response = await _client.DetectLabelsAsync(request);

// Azure Computer Vision
var client = new ComputerVisionClient(
    new ApiKeyServiceClientCredentials(apiKey)
)
{
    Endpoint = endpoint
};

var features = new List<VisualFeatureTypes?> 
{ 
    VisualFeatureTypes.Objects,
    VisualFeatureTypes.Tags 
};
var result = await client.AnalyzeImageAsync(imageUrl, features);

foreach (var tag in result.Tags)
{
    if (tag.Confidence > 0.7)
    {
        Console.WriteLine($"{tag.Name} ({tag.Confidence})");
    }
}
```

#### Azure NuGet Package:
```xml
<PackageReference Include="Microsoft.Azure.CognitiveServices.Vision.ComputerVision" Version="7.0.1" />
```

#### Configuration:
```json
"Azure": {
  "ComputerVision": {
    "Endpoint": "https://your-resource.cognitiveservices.azure.com/",
    "ApiKey": "your-key",
    "MinConfidence": 0.7
  }
}
```

#### Migration Complexity: **Medium**
- Reason: Different response structure, label names may vary

---

## Detailed Migration Roadmap

### Phase 1: Planning & Setup (Week 1)

#### Tasks:
1. **Azure Resource Provisioning**
   - Create Azure Resource Group
   - Provision Azure OpenAI Service
   - Create AI Search service
   - Set up Cosmos DB account
   - Create Blob Storage account
   - Provision Document Intelligence resource
   - Create Language Service resource
   - Set up Computer Vision resource

2. **Development Environment**
   - Install Azure SDK NuGet packages
   - Remove AWS SDK packages
   - Update .NET project references
   - Set up Azure credentials (Managed Identity or Service Principal)

3. **Configuration Migration**
   - Create new `appsettings.azure.json`
   - Map AWS config to Azure equivalents
   - Set up Key Vault for secrets

#### Azure Resources Checklist:

```bash
# Resource Group
az group create --name rg-claims-rag-bot --location eastus

# Azure OpenAI
az cognitiveservices account create \
  --name openai-claims-rag \
  --resource-group rg-claims-rag-bot \
  --kind OpenAI \
  --sku S0 \
  --location eastus

# Deploy models
az cognitiveservices account deployment create \
  --name openai-claims-rag \
  --resource-group rg-claims-rag-bot \
  --deployment-name gpt-4-turbo \
  --model-name gpt-4 \
  --model-version turbo-2024-04-09 \
  --model-format OpenAI

az cognitiveservices account deployment create \
  --name openai-claims-rag \
  --resource-group rg-claims-rag-bot \
  --deployment-name text-embedding-ada-002 \
  --model-name text-embedding-ada-002 \
  --model-version 2 \
  --model-format OpenAI

# AI Search
az search service create \
  --name search-claims-rag \
  --resource-group rg-claims-rag-bot \
  --sku standard \
  --location eastus

# Cosmos DB
az cosmosdb create \
  --name cosmos-claims-rag \
  --resource-group rg-claims-rag-bot \
  --default-consistency-level Session

az cosmosdb sql database create \
  --account-name cosmos-claims-rag \
  --resource-group rg-claims-rag-bot \
  --name ClaimsDatabase

az cosmosdb sql container create \
  --account-name cosmos-claims-rag \
  --database-name ClaimsDatabase \
  --name AuditTrail \
  --partition-key-path "/PolicyNumber"

# Blob Storage
az storage account create \
  --name stclaimsrag \
  --resource-group rg-claims-rag-bot \
  --location eastus \
  --sku Standard_LRS

az storage container create \
  --name claims-documents \
  --account-name stclaimsrag

# Document Intelligence
az cognitiveservices account create \
  --name docintel-claims-rag \
  --resource-group rg-claims-rag-bot \
  --kind FormRecognizer \
  --sku S0 \
  --location eastus

# Language Service
az cognitiveservices account create \
  --name lang-claims-rag \
  --resource-group rg-claims-rag-bot \
  --kind TextAnalytics \
  --sku S \
  --location eastus

# Computer Vision
az cognitiveservices account create \
  --name vision-claims-rag \
  --resource-group rg-claims-rag-bot \
  --kind ComputerVision \
  --sku S1 \
  --location eastus
```

---

### Phase 2: Core AI Services Migration (Week 2-3)

#### 2.1 EmbeddingService Migration

**File:** `src/ClaimsRagBot.Infrastructure/Azure/AzureEmbeddingService.cs`

```csharp
using Azure;
using Azure.AI.OpenAI;
using ClaimsRagBot.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.Azure;

public class AzureEmbeddingService : IEmbeddingService
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;

    public AzureEmbeddingService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:OpenAI:Endpoint"] 
            ?? throw new ArgumentException("Azure OpenAI endpoint not configured");
        var apiKey = configuration["Azure:OpenAI:ApiKey"] 
            ?? throw new ArgumentException("Azure OpenAI API key not configured");
        
        _deploymentName = configuration["Azure:OpenAI:EmbeddingDeployment"] 
            ?? "text-embedding-ada-002";
        
        _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        
        Console.WriteLine($"[AzureEmbedding] Using deployment: {_deploymentName}");
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var options = new EmbeddingsOptions(_deploymentName, new[] { text });
            var response = await _client.GetEmbeddingsAsync(options);
            
            var embedding = response.Value.Data[0].Embedding.ToArray();
            
            Console.WriteLine($"[AzureEmbedding] Generated embedding: {embedding.Length} dimensions");
            return embedding;
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"[AzureEmbedding] Error: {ex.Message}");
            throw;
        }
    }
}
```

**Changes:**
- Replace `AmazonBedrockRuntimeClient` with `OpenAIClient`
- Update embedding dimensions: 1024 → 1536
- Remove SigV4 signing (Azure uses API key)
- Update configuration keys

---

#### 2.2 LlmService Migration

**File:** `src/ClaimsRagBot.Infrastructure/Azure/AzureLlmService.cs`

```csharp
using Azure;
using Azure.AI.OpenAI;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ClaimsRagBot.Infrastructure.Azure;

public class AzureLlmService : ILlmService
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;

    public AzureLlmService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:OpenAI:Endpoint"] 
            ?? throw new ArgumentException("Azure OpenAI endpoint not configured");
        var apiKey = configuration["Azure:OpenAI:ApiKey"] 
            ?? throw new ArgumentException("Azure OpenAI API key not configured");
        
        _deploymentName = configuration["Azure:OpenAI:ChatDeployment"] 
            ?? "gpt-4-turbo";
        
        _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        
        Console.WriteLine($"[AzureLLM] Using deployment: {_deploymentName}");
    }

    public async Task<ClaimDecision> GenerateDecisionAsync(
        ClaimRequest request, 
        List<PolicyClause> clauses)
    {
        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(request, clauses);

        try
        {
            var chatOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                MaxTokens = 4096,
                Temperature = 0.3f,
                ResponseFormat = ChatCompletionsResponseFormat.JsonObject
            };

            var response = await _client.GetChatCompletionsAsync(chatOptions);
            var content = response.Value.Choices[0].Message.Content;

            var decision = JsonSerializer.Deserialize<ClaimDecision>(content);
            return decision ?? throw new InvalidOperationException("Failed to parse decision");
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"[AzureLLM] Error: {ex.Message}");
            throw;
        }
    }

    private string BuildSystemPrompt()
    {
        return @"You are an expert insurance claims adjuster for Aflac.
Analyze claims against policy clauses and provide coverage decisions.
Return JSON with: Status, Explanation, ClauseReferences, RequiredDocuments, ConfidenceScore.";
    }

    private string BuildUserPrompt(ClaimRequest request, List<PolicyClause> clauses)
    {
        var clausesText = string.Join("\n\n", clauses.Select(c => 
            $"[{c.ClauseId}] {c.ClauseText}"));

        return $@"CLAIM DETAILS:
Policy Number: {request.PolicyNumber}
Policy Type: {request.PolicyType}
Claim Amount: ${request.ClaimAmount}
Description: {request.ClaimDescription}

RELEVANT POLICY CLAUSES:
{clausesText}

Analyze and return decision as JSON.";
    }
}
```

**Changes:**
- Replace Claude API with GPT-4 Chat Completions API
- Update prompt format (Claude → ChatML)
- Use `ChatRequestSystemMessage` and `ChatRequestUserMessage`
- Enable JSON mode with `ResponseFormat = ChatCompletionsResponseFormat.JsonObject`

---

#### 2.3 RetrievalService Migration

**File:** `src/ClaimsRagBot.Infrastructure/Azure/AzureAISearchService.cs`

```csharp
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.Azure;

public class AzureAISearchService : IRetrievalService
{
    private readonly SearchClient _searchClient;
    private readonly SearchIndexClient _indexClient;
    private readonly string _indexName;

    public AzureAISearchService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:AISearch:Endpoint"] 
            ?? throw new ArgumentException("AI Search endpoint not configured");
        var apiKey = configuration["Azure:AISearch:AdminApiKey"] 
            ?? throw new ArgumentException("AI Search API key not configured");
        
        _indexName = configuration["Azure:AISearch:IndexName"] ?? "policy-clauses";
        
        var credential = new AzureKeyCredential(apiKey);
        _indexClient = new SearchIndexClient(new Uri(endpoint), credential);
        _searchClient = _indexClient.GetSearchClient(_indexName);
        
        Console.WriteLine($"[AzureAISearch] Connected to index: {_indexName}");
    }

    public async Task<List<PolicyClause>> RetrieveClausesAsync(
        float[] embedding, 
        string policyType, 
        int maxResults = 5)
    {
        try
        {
            var vectorQuery = new VectorizedQuery(embedding)
            {
                KNearestNeighborsCount = maxResults,
                Fields = { "Embedding" }
            };

            var searchOptions = new SearchOptions
            {
                VectorSearch = new() { Queries = { vectorQuery } },
                Filter = $"PolicyType eq '{policyType}'",
                Select = { "ClauseId", "ClauseText", "PolicyType", "Category" },
                Size = maxResults
            };

            var results = await _searchClient.SearchAsync<PolicyClauseSearchResult>(
                null, 
                searchOptions
            );

            var clauses = new List<PolicyClause>();
            await foreach (var result in results.Value.GetResultsAsync())
            {
                clauses.Add(new PolicyClause
                {
                    ClauseId = result.Document.ClauseId,
                    ClauseText = result.Document.ClauseText,
                    PolicyType = result.Document.PolicyType,
                    Category = result.Document.Category
                });
            }

            Console.WriteLine($"[AzureAISearch] Retrieved {clauses.Count} clauses");
            return clauses;
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"[AzureAISearch] Error: {ex.Message}");
            return GetMockClauses(policyType); // Fallback
        }
    }

    // Index creation method (for PolicyIngestion tool)
    public async Task CreateIndexAsync(int vectorDimensions = 1536)
    {
        var index = new SearchIndex(_indexName)
        {
            Fields =
            {
                new SimpleField("ClauseId", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("ClauseText"),
                new SimpleField("PolicyType", SearchFieldDataType.String) 
                { 
                    IsFilterable = true, 
                    IsFacetable = true 
                },
                new SimpleField("Category", SearchFieldDataType.String) 
                { 
                    IsFilterable = true 
                },
                new VectorSearchField("Embedding", vectorDimensions, "vector-profile")
            },
            VectorSearch = new()
            {
                Profiles =
                {
                    new VectorSearchProfile("vector-profile", "hnsw-config")
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("hnsw-config")
                    {
                        Parameters = new HnswParameters
                        {
                            Metric = VectorSearchAlgorithmMetric.Cosine,
                            M = 4,
                            EfConstruction = 400,
                            EfSearch = 500
                        }
                    }
                }
            }
        };

        await _indexClient.CreateOrUpdateIndexAsync(index);
        Console.WriteLine($"[AzureAISearch] Index '{_indexName}' created/updated");
    }

    private List<PolicyClause> GetMockClauses(string policyType)
    {
        // Mock fallback (same as AWS version)
        return new List<PolicyClause>
        {
            new PolicyClause
            {
                ClauseId = "MOCK-001",
                ClauseText = "Sample policy clause for testing",
                PolicyType = policyType,
                Category = "Coverage"
            }
        };
    }
}

// Search result model
public class PolicyClauseSearchResult
{
    public string ClauseId { get; set; } = string.Empty;
    public string ClauseText { get; set; } = string.Empty;
    public string PolicyType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
```

**Changes:**
- Replace OpenSearch HTTP + SigV4 with Azure Search SDK
- Update vector search query syntax
- Remove manual request signing
- Add index creation helper
- Update embedding dimensions: 1024 → 1536

---

### Phase 3: Storage & Database Migration (Week 3)

#### 3.1 AuditService Migration

**File:** `src/ClaimsRagBot.Infrastructure/Azure/AzureCosmosAuditService.cs`

```csharp
using Microsoft.Azure.Cosmos;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ClaimsRagBot.Infrastructure.Azure;

public class AzureCosmosAuditService : IAuditService
{
    private readonly CosmosClient _client;
    private readonly Container _container;

    public AzureCosmosAuditService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:CosmosDB:Endpoint"] 
            ?? throw new ArgumentException("Cosmos DB endpoint not configured");
        var key = configuration["Azure:CosmosDB:Key"] 
            ?? throw new ArgumentException("Cosmos DB key not configured");
        var databaseId = configuration["Azure:CosmosDB:DatabaseId"] ?? "ClaimsDatabase";
        var containerId = configuration["Azure:CosmosDB:ContainerId"] ?? "AuditTrail";

        _client = new CosmosClient(endpoint, key);
        _container = _client.GetContainer(databaseId, containerId);
        
        Console.WriteLine($"[CosmosDB] Connected to {databaseId}/{containerId}");
    }

    public async Task SaveAsync(
        ClaimRequest request, 
        ClaimDecision decision, 
        List<PolicyClause> retrievedClauses)
    {
        var auditRecord = new ClaimAuditRecord
        {
            id = Guid.NewGuid().ToString(),
            ClaimId = Guid.NewGuid().ToString(),
            PolicyNumber = request.PolicyNumber,
            PolicyType = request.PolicyType,
            ClaimAmount = request.ClaimAmount,
            ClaimDescription = request.ClaimDescription,
            DecisionStatus = decision.Status,
            DecisionExplanation = decision.Explanation,
            ConfidenceScore = decision.ConfidenceScore,
            ClauseReferences = decision.ClauseReferences,
            RequiredDocuments = decision.RequiredDocuments,
            RetrievedClauses = JsonSerializer.Serialize(retrievedClauses),
            Timestamp = DateTime.UtcNow
        };

        try
        {
            await _container.CreateItemAsync(
                auditRecord, 
                new PartitionKey(request.PolicyNumber)
            );
            
            Console.WriteLine($"[CosmosDB] Saved audit record: {auditRecord.ClaimId}");
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"[CosmosDB] Error: {ex.Message}");
            throw;
        }
    }
}

public class ClaimAuditRecord
{
    public string id { get; set; } = string.Empty; // Cosmos requires lowercase 'id'
    public string ClaimId { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string PolicyType { get; set; } = string.Empty;
    public decimal ClaimAmount { get; set; }
    public string ClaimDescription { get; set; } = string.Empty;
    public string DecisionStatus { get; set; } = string.Empty;
    public string DecisionExplanation { get; set; } = string.Empty;
    public float ConfidenceScore { get; set; }
    public List<string> ClauseReferences { get; set; } = new();
    public List<string> RequiredDocuments { get; set; } = new();
    public string RetrievedClauses { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
```

**Changes:**
- Replace DynamoDB SDK with Cosmos DB SDK
- Use document model instead of attribute dictionary
- Partition key: `PolicyNumber`
- Cosmos requires lowercase `id` field

---

#### 3.2 DocumentUploadService Migration

**File:** `src/ClaimsRagBot.Infrastructure/Azure/AzureBlobStorageService.cs`

```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.Azure;

public class AzureBlobStorageService : IDocumentUploadService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _containerClient;
    private readonly string _containerName;
    private readonly string _uploadPrefix;
    private readonly int _sasTokenExpiration;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["Azure:BlobStorage:ConnectionString"] 
            ?? throw new ArgumentException("Blob storage connection string not configured");
        
        _containerName = configuration["Azure:BlobStorage:ContainerName"] ?? "claims-documents";
        _uploadPrefix = configuration["Azure:BlobStorage:UploadPrefix"] ?? "uploads/";
        _sasTokenExpiration = int.Parse(
            configuration["Azure:BlobStorage:SasTokenExpiration"] ?? "3600"
        );

        _blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        
        Console.WriteLine($"[BlobStorage] Connected to container: {_containerName}");
    }

    public async Task<DocumentUploadResult> UploadDocumentAsync(
        Stream fileStream, 
        string fileName, 
        string contentType, 
        string? userId = null)
    {
        var blobName = $"{_uploadPrefix}{userId ?? "anonymous"}/{Guid.NewGuid()}_{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        try
        {
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            };

            await blobClient.UploadAsync(fileStream, uploadOptions);

            // Generate SAS token for download
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddSeconds(_sasTokenExpiration)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            
            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            var result = new DocumentUploadResult
            {
                DocumentId = Guid.NewGuid().ToString(),
                FileName = fileName,
                BlobName = blobName,
                ContainerName = _containerName,
                PresignedUrl = sasUri.ToString(),
                UploadedAt = DateTime.UtcNow
            };

            Console.WriteLine($"[BlobStorage] Uploaded: {blobName}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BlobStorage] Upload error: {ex.Message}");
            throw;
        }
    }

    public async Task<Stream> GetDocumentAsync(string blobName)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        var response = await blobClient.DownloadAsync();
        return response.Value.Content;
    }
}
```

**Changes:**
- Replace S3 SDK with Blob Storage SDK
- Use connection string instead of access keys
- Generate SAS tokens instead of presigned URLs
- Update path structure (`s3Key` → `blobName`)

---

### Phase 4: Document Processing Services (Week 4)

#### 4.1 TextractService Migration

**File:** `src/ClaimsRagBot.Infrastructure/Azure/AzureDocumentIntelligenceService.cs`

```csharp
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.Azure;

public class AzureDocumentIntelligenceService : ITextractService
{
    private readonly DocumentAnalysisClient _client;
    private readonly string _modelId;

    public AzureDocumentIntelligenceService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:DocumentIntelligence:Endpoint"] 
            ?? throw new ArgumentException("Document Intelligence endpoint not configured");
        var apiKey = configuration["Azure:DocumentIntelligence:ApiKey"] 
            ?? throw new ArgumentException("Document Intelligence API key not configured");
        
        _modelId = configuration["Azure:DocumentIntelligence:ModelId"] ?? "prebuilt-document";
        
        _client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        
        Console.WriteLine($"[DocumentIntelligence] Using model: {_modelId}");
    }

    public async Task<DocumentExtractionResult> ExtractTextAsync(
        string containerName, 
        string blobName)
    {
        try
        {
            // Assumes blob has SAS token in URL
            var blobUri = new Uri($"https://{containerName}.blob.core.windows.net/{blobName}?{sasToken}");
            
            var operation = await _client.AnalyzeDocumentFromUriAsync(
                WaitUntil.Completed,
                _modelId,
                blobUri
            );

            var result = operation.Value;
            var extractedText = new StringBuilder();
            var formFields = new Dictionary<string, string>();
            var tables = new List<TableData>();

            // Extract text
            foreach (var page in result.Pages)
            {
                foreach (var line in page.Lines)
                {
                    extractedText.AppendLine(line.Content);
                }
            }

            // Extract key-value pairs
            foreach (var kvp in result.KeyValuePairs)
            {
                var key = kvp.Key.Content;
                var value = kvp.Value?.Content ?? "";
                formFields[key] = value;
            }

            // Extract tables
            foreach (var table in result.Tables)
            {
                var tableData = new TableData
                {
                    RowCount = table.RowCount,
                    ColumnCount = table.ColumnCount,
                    Cells = table.Cells.Select(c => new CellData
                    {
                        RowIndex = c.RowIndex,
                        ColumnIndex = c.ColumnIndex,
                        Content = c.Content,
                        IsHeader = c.Kind == DocumentTableCellKind.ColumnHeader
                    }).ToList()
                };
                tables.Add(tableData);
            }

            return new DocumentExtractionResult
            {
                ExtractedText = extractedText.ToString(),
                FormFields = formFields,
                Tables = tables,
                PageCount = result.Pages.Count
            };
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"[DocumentIntelligence] Error: {ex.Message}");
            throw;
        }
    }
}
```

**Changes:**
- Replace Textract SDK with Document Intelligence SDK
- Use prebuilt models (`prebuilt-document`, `prebuilt-invoice`)
- Different response structure (Azure has `Pages`, `KeyValuePairs`, `Tables`)
- No separate form/table feature flags (model handles both)

---

#### 4.2 ComprehendService Migration

**File:** `src/ClaimsRagBot.Infrastructure/Azure/AzureLanguageService.cs`

```csharp
using Azure;
using Azure.AI.TextAnalytics;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.Azure;

public class AzureLanguageService : IComprehendService
{
    private readonly TextAnalyticsClient _client;

    public AzureLanguageService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:LanguageService:Endpoint"] 
            ?? throw new ArgumentException("Language Service endpoint not configured");
        var apiKey = configuration["Azure:LanguageService:ApiKey"] 
            ?? throw new ArgumentException("Language Service API key not configured");
        
        _client = new TextAnalyticsClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        
        Console.WriteLine("[LanguageService] Initialized");
    }

    public async Task<List<ExtractedEntity>> ExtractEntitiesAsync(string text)
    {
        try
        {
            var response = await _client.RecognizeEntitiesAsync(text);
            var entities = new List<ExtractedEntity>();

            foreach (var entity in response.Value)
            {
                entities.Add(new ExtractedEntity
                {
                    Text = entity.Text,
                    Type = MapEntityCategory(entity.Category.ToString()),
                    Score = (float)entity.ConfidenceScore,
                    BeginOffset = entity.Offset,
                    EndOffset = entity.Offset + entity.Length
                });
            }

            Console.WriteLine($"[LanguageService] Extracted {entities.Count} entities");
            return entities;
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"[LanguageService] Error: {ex.Message}");
            throw;
        }
    }

    private string MapEntityCategory(string azureCategory)
    {
        // Map Azure entity categories to AWS Comprehend equivalents
        return azureCategory switch
        {
            "Person" => "PERSON",
            "DateTime" => "DATE",
            "Quantity" => "QUANTITY",
            "Organization" => "ORGANIZATION",
            "Location" => "LOCATION",
            _ => "OTHER"
        };
    }
}
```

**Changes:**
- Replace Comprehend SDK with Text Analytics SDK
- Map entity categories (Azure → AWS naming)
- Use `RecognizeEntitiesAsync` instead of `DetectEntitiesAsync`

---

#### 4.3 RekognitionService Migration

**File:** `src/ClaimsRagBot.Infrastructure/Azure/AzureComputerVisionService.cs`

```csharp
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.Azure;

public class AzureComputerVisionService : IRekognitionService
{
    private readonly ComputerVisionClient _client;
    private readonly double _minConfidence;

    public AzureComputerVisionService(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:ComputerVision:Endpoint"] 
            ?? throw new ArgumentException("Computer Vision endpoint not configured");
        var apiKey = configuration["Azure:ComputerVision:ApiKey"] 
            ?? throw new ArgumentException("Computer Vision API key not configured");
        
        _minConfidence = double.Parse(
            configuration["Azure:ComputerVision:MinConfidence"] ?? "0.7"
        );

        _client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(apiKey))
        {
            Endpoint = endpoint
        };
        
        Console.WriteLine("[ComputerVision] Initialized");
    }

    public async Task<List<DetectedLabel>> AnalyzeImageAsync(string imageUrl)
    {
        try
        {
            var features = new List<VisualFeatureTypes?>
            {
                VisualFeatureTypes.Objects,
                VisualFeatureTypes.Tags,
                VisualFeatureTypes.Description
            };

            var analysis = await _client.AnalyzeImageAsync(imageUrl, features);
            var labels = new List<DetectedLabel>();

            foreach (var tag in analysis.Tags)
            {
                if (tag.Confidence >= _minConfidence)
                {
                    labels.Add(new DetectedLabel
                    {
                        Name = tag.Name,
                        Confidence = (float)tag.Confidence
                    });
                }
            }

            foreach (var obj in analysis.Objects)
            {
                if (obj.Confidence >= _minConfidence)
                {
                    labels.Add(new DetectedLabel
                    {
                        Name = obj.ObjectProperty,
                        Confidence = (float)obj.Confidence
                    });
                }
            }

            Console.WriteLine($"[ComputerVision] Detected {labels.Count} labels");
            return labels;
        }
        catch (ComputerVisionErrorException ex)
        {
            Console.WriteLine($"[ComputerVision] Error: {ex.Message}");
            throw;
        }
    }
}
```

**Changes:**
- Replace Rekognition SDK with Computer Vision SDK
- Use `AnalyzeImageAsync` instead of `DetectLabelsAsync`
- Map Azure tags/objects to label format

---

### Phase 5: Policy Ingestion Tool Migration (Week 4)

**File:** `tools/PolicyIngestion/Program.cs` (Azure version)

```csharp
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// Azure OpenAI for embeddings
var openAIClient = new OpenAIClient(
    new Uri(configuration["Azure:OpenAI:Endpoint"]),
    new AzureKeyCredential(configuration["Azure:OpenAI:ApiKey"])
);

// Azure AI Search
var searchClient = new SearchIndexClient(
    new Uri(configuration["Azure:AISearch:Endpoint"]),
    new AzureKeyCredential(configuration["Azure:AISearch:AdminApiKey"])
);

var indexName = "policy-clauses";

// Step 1: Create index
await CreateIndexAsync(searchClient, indexName);

// Step 2: Generate embeddings and index clauses
var clauses = GetSamplePolicyClauses();
var indexClient = searchClient.GetSearchClient(indexName);

foreach (var clause in clauses)
{
    // Generate embedding
    var embeddingResponse = await openAIClient.GetEmbeddingsAsync(
        new EmbeddingsOptions("text-embedding-ada-002", new[] { clause.ClauseText })
    );
    var embedding = embeddingResponse.Value.Data[0].Embedding.ToArray();

    // Index document
    var document = new SearchDocument
    {
        ["ClauseId"] = clause.ClauseId,
        ["ClauseText"] = clause.ClauseText,
        ["PolicyType"] = clause.PolicyType,
        ["Category"] = clause.Category,
        ["Embedding"] = embedding
    };

    await indexClient.UploadDocumentsAsync(new[] { document });
    Console.WriteLine($"Indexed: {clause.ClauseId}");
}

Console.WriteLine("Policy ingestion complete!");

async Task CreateIndexAsync(SearchIndexClient client, string name)
{
    var index = new SearchIndex(name)
    {
        Fields =
        {
            new SimpleField("ClauseId", SearchFieldDataType.String) { IsKey = true },
            new SearchableField("ClauseText"),
            new SimpleField("PolicyType", SearchFieldDataType.String) { IsFilterable = true },
            new SimpleField("Category", SearchFieldDataType.String) { IsFilterable = true },
            new VectorSearchField("Embedding", 1536, "vector-profile")
        },
        VectorSearch = new()
        {
            Profiles = { new VectorSearchProfile("vector-profile", "hnsw-config") },
            Algorithms = { new HnswAlgorithmConfiguration("hnsw-config") }
        }
    };

    await client.CreateOrUpdateIndexAsync(index);
    Console.WriteLine($"Index '{name}' created");
}
```

**Changes:**
- Replace Bedrock embedding calls with Azure OpenAI
- Replace OpenSearch HTTP with AI Search SDK
- Update index schema for 1536 dimensions
- Remove SigV4 signing logic

---

### Phase 6: Configuration & Deployment (Week 5)

#### 6.1 appsettings.azure.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Azure": {
    "OpenAI": {
      "Endpoint": "https://openai-claims-rag.openai.azure.com/",
      "ApiKey": "YOUR_AZURE_OPENAI_KEY",
      "EmbeddingDeployment": "text-embedding-ada-002",
      "ChatDeployment": "gpt-4-turbo"
    },
    "AISearch": {
      "Endpoint": "https://search-claims-rag.search.windows.net",
      "AdminApiKey": "YOUR_ADMIN_KEY",
      "QueryApiKey": "YOUR_QUERY_KEY",
      "IndexName": "policy-clauses"
    },
    "CosmosDB": {
      "Endpoint": "https://cosmos-claims-rag.documents.azure.com:443/",
      "Key": "YOUR_COSMOS_KEY",
      "DatabaseId": "ClaimsDatabase",
      "ContainerId": "AuditTrail"
    },
    "BlobStorage": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=stclaimsrag;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net",
      "ContainerName": "claims-documents",
      "UploadPrefix": "uploads/",
      "SasTokenExpiration": 3600
    },
    "DocumentIntelligence": {
      "Endpoint": "https://docintel-claims-rag.cognitiveservices.azure.com/",
      "ApiKey": "YOUR_DOCINTEL_KEY",
      "ModelId": "prebuilt-document"
    },
    "LanguageService": {
      "Endpoint": "https://lang-claims-rag.cognitiveservices.azure.com/",
      "ApiKey": "YOUR_LANG_KEY"
    },
    "ComputerVision": {
      "Endpoint": "https://vision-claims-rag.cognitiveservices.azure.com/",
      "ApiKey": "YOUR_VISION_KEY",
      "MinConfidence": 0.7
    }
  },
  "ClaimsValidation": {
    "AutoApprovalThreshold": 5000,
    "ConfidenceThreshold": 0.85
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
    "EnableOcr": true,
    "EnableEntityExtraction": true
  }
}
```

#### 6.2 Update Program.cs Dependency Injection

```csharp
using ClaimsRagBot.Application.RAG;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Infrastructure.Azure; // NEW namespace

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.azure.json", optional: false);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Azure services
builder.Services.AddSingleton<IEmbeddingService, AzureEmbeddingService>();
builder.Services.AddSingleton<IRetrievalService, AzureAISearchService>();
builder.Services.AddSingleton<ILlmService, AzureLlmService>();
builder.Services.AddSingleton<IAuditService, AzureCosmosAuditService>();
builder.Services.AddSingleton<IDocumentUploadService, AzureBlobStorageService>();
builder.Services.AddSingleton<ITextractService, AzureDocumentIntelligenceService>();
builder.Services.AddSingleton<IComprehendService, AzureLanguageService>();
builder.Services.AddSingleton<IRekognitionService, AzureComputerVisionService>();
builder.Services.AddScoped<IDocumentExtractionService, DocumentExtractionOrchestrator>();
builder.Services.AddScoped<ClaimValidationOrchestrator>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.Run();
```

#### 6.3 Update .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\ClaimsRagBot.Core\ClaimsRagBot.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Remove AWS packages -->
    <!-- <PackageReference Include="AWSSDK.BedrockRuntime" Version="4.0.14.6" /> -->
    <!-- <PackageReference Include="AWSSDK.DynamoDBv2" Version="4.0.10.8" /> -->
    <!-- ... (remove all AWS packages) -->

    <!-- Add Azure packages -->
    <PackageReference Include="Azure.AI.OpenAI" Version="1.0.0-beta.14" />
    <PackageReference Include="Azure.Search.Documents" Version="11.5.1" />
    <PackageReference Include="Microsoft.Azure.Cosmos" Version="3.38.1" />
    <PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />
    <PackageReference Include="Azure.AI.FormRecognizer" Version="4.1.0" />
    <PackageReference Include="Azure.AI.TextAnalytics" Version="5.3.0" />
    <PackageReference Include="Microsoft.Azure.CognitiveServices.Vision.ComputerVision" Version="7.0.1" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.2" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

---

## Code Changes Required

### Summary of File Changes

| File | Change Type | Estimated LOC Changed |
|------|------------|----------------------|
| `EmbeddingService.cs` | Rewrite | 62 → 45 |
| `LlmService.cs` | Rewrite | 96 → 85 |
| `RetrievalService.cs` | Rewrite | 253 → 180 |
| `AuditService.cs` | Rewrite | 78 → 65 |
| `DocumentUploadService.cs` | Rewrite | 142 → 110 |
| `TextractService.cs` | Rewrite | 324 → 180 |
| `ComprehendService.cs` | Rewrite | 131 → 90 |
| `RekognitionService.cs` | Rewrite | 89 → 75 |
| `DocumentExtractionOrchestrator.cs` | Moderate | 530 → 520 (minor updates) |
| `PolicyIngestionService.cs` | Rewrite | 155 → 120 |
| `Program.cs` (API) | Minor | 103 → 110 (DI updates) |
| `Program.cs` (PolicyIngestion) | Rewrite | 100 → 90 |
| `ClaimsRagBot.Infrastructure.csproj` | Replace packages | 11 AWS → 7 Azure packages |
| `appsettings.json` | Recreate | New Azure config structure |

**Total Estimated Changes:** ~1,500 lines of code affected

---

## Configuration Changes

### Environment Variables

```bash
# AWS (Current)
AWS_REGION=us-east-1
AWS_ACCESS_KEY_ID=AKIA...
AWS_SECRET_ACCESS_KEY=...
AWS_OPENSEARCH_ENDPOINT=https://...aoss.amazonaws.com

# Azure (New)
AZURE_OPENAI_ENDPOINT=https://....openai.azure.com/
AZURE_OPENAI_API_KEY=...
AZURE_SEARCH_ENDPOINT=https://....search.windows.net
AZURE_SEARCH_ADMIN_KEY=...
AZURE_COSMOS_ENDPOINT=https://....documents.azure.com:443/
AZURE_COSMOS_KEY=...
AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;...
AZURE_DOCINTEL_ENDPOINT=https://....cognitiveservices.azure.com/
AZURE_DOCINTEL_KEY=...
AZURE_LANGUAGE_ENDPOINT=https://....cognitiveservices.azure.com/
AZURE_LANGUAGE_KEY=...
AZURE_VISION_ENDPOINT=https://....cognitiveservices.azure.com/
AZURE_VISION_KEY=...
```

### Key Vault Integration (Recommended)

```csharp
// Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://kv-claims-rag.vault.azure.net/"),
    new DefaultAzureCredential()
);
```

---

## Testing Strategy

### Unit Testing

1. **Mock Azure SDK Clients**
   ```csharp
   // Example: Mock OpenAIClient
   var mockClient = new Mock<OpenAIClient>();
   mockClient.Setup(c => c.GetEmbeddingsAsync(It.IsAny<EmbeddingsOptions>(), default))
             .ReturnsAsync(mockEmbeddingResponse);
   ```

2. **Test Each Service Independently**
   - `AzureEmbeddingService_GenerateEmbedding_ReturnsCorrectDimensions`
   - `AzureLlmService_GenerateDecision_ParsesJsonCorrectly`
   - `AzureAISearchService_RetrieveClauses_ReturnsTopK`

### Integration Testing

1. **Azure Service Connectivity**
   - Test OpenAI API connectivity
   - Test AI Search index creation
   - Test Cosmos DB writes
   - Test Blob Storage uploads

2. **End-to-End Validation Flow**
   ```bash
   # Test claim validation
   POST /claims/validate
   {
     "policyNumber": "TEST-001",
     "policyType": "Health",
     "claimAmount": 1000,
     "claimDescription": "Hospital admission for pneumonia"
   }
   ```

3. **Document Processing Pipeline**
   - Upload PDF → Extract text → Extract entities → Validate claim

### Performance Testing

1. **Embedding Generation:** Target <500ms per call
2. **Vector Search:** Target <300ms per query
3. **LLM Response:** Target <3s per decision
4. **Document OCR:** Target <10s per page

### Comparison Testing (AWS vs Azure)

Run identical claims through both systems and compare:
- Decision accuracy
- Response times
- Confidence scores
- Retrieved policy clauses

---

## Risk Assessment

### High Risk Items

1. **Embedding Dimension Mismatch**
   - **Risk:** 1024 (AWS) → 1536 (Azure) breaks vector search
   - **Mitigation:** Recreate entire AI Search index with new dimensions
   - **Impact:** All existing policy clauses must be re-indexed

2. **LLM Response Format Changes**
   - **Risk:** GPT-4 JSON parsing differs from Claude
   - **Mitigation:** Extensive prompt engineering and testing
   - **Impact:** May require decision parsing logic updates

3. **Cost Overruns**
   - **Risk:** Azure OpenAI can be more expensive than Bedrock
   - **Mitigation:** Set budget alerts, use quota limits
   - **Impact:** Potential 20-30% cost increase

### Medium Risk Items

1. **API Rate Limits**
   - **Azure OpenAI:** 60K tokens/minute (default)
   - **Mitigation:** Request quota increase, implement retry logic

2. **Data Migration**
   - **Risk:** Losing audit trail data during DynamoDB → Cosmos migration
   - **Mitigation:** Export DynamoDB table, transform, import to Cosmos

3. **Authentication Complexity**
   - **Risk:** Azure has multiple auth methods (API key, Managed Identity, AAD)
   - **Mitigation:** Start with API keys, migrate to Managed Identity

### Low Risk Items

1. **Blob Storage Migration**
   - **S3 → Blob:** Nearly identical functionality
   - **Mitigation:** Simple data copy with AzCopy

2. **Frontend Changes**
   - **Risk:** None (API contract unchanged)
   - **Impact:** Zero frontend modifications required

---

## Cost Comparison

### AWS Monthly Costs (Current Usage: 500 claims/month)

| Service | Usage | Cost |
|---------|-------|------|
| Bedrock (Titan Embeddings) | 500 × 500 tokens | $0.03 |
| Bedrock (Claude 3.5) | 500 × 5K tokens | $40.00 |
| OpenSearch Serverless | 2 OCU × 730 hours | $350.40 |
| DynamoDB | 500 writes | $0.63 |
| S3 | 500 MB storage + requests | $1.50 |
| Textract | 100 pages | $5.15 |
| Comprehend | 1,000 units | $1.00 |
| Rekognition | 50 images | $0.13 |
| **TOTAL** | | **$398.84/month** |

### Azure Monthly Costs (Projected)

| Service | Usage | Cost |
|---------|-------|------|
| Azure OpenAI (ada-002) | 500 × 500 tokens | $0.02 |
| Azure OpenAI (GPT-4 Turbo) | 500 × 5K tokens | $50.00 |
| AI Search (Standard) | Base + queries | $250.00 |
| Cosmos DB | 500 writes + storage | $5.00 |
| Blob Storage | 500 MB + requests | $1.20 |
| Document Intelligence | 100 pages | $10.00 |
| Language Service | 1,000 units | $1.00 |
| Computer Vision | 50 images | $0.10 |
| **TOTAL** | | **$317.32/month** |

**Cost Savings:** $81.52/month (20% reduction)

**Key Drivers:**
- AI Search cheaper than OpenSearch Serverless
- Cosmos DB cheaper than DynamoDB for this usage
- GPT-4 more expensive than Claude
- Document Intelligence 2× cost of Textract

---

## Conclusion

### Migration Feasibility: **HIGH**

All AWS services have equivalent Azure offerings with comparable or superior functionality. The migration is technically straightforward with well-documented Azure SDKs.

### Recommended Approach

1. **Parallel Development:** Build Azure version alongside AWS (2 weeks)
2. **A/B Testing:** Run both systems with 10% traffic to Azure (1 week)
3. **Gradual Cutover:** Increase Azure traffic to 100% over 2 weeks
4. **AWS Decommission:** After 1 month of stable Azure operation

### Total Timeline: 6-8 Weeks

- **Week 1:** Azure resource provisioning + planning
- **Week 2-3:** Core AI services migration
- **Week 3:** Storage & database migration
- **Week 4:** Document processing services migration
- **Week 5:** Testing & validation
- **Week 6:** Deployment & monitoring
- **Week 7-8:** Parallel operation & cutover

### Success Metrics

- ✅ 100% API compatibility (no frontend changes)
- ✅ <5% decision accuracy variance
- ✅ <20% response time variance
- ✅ Zero data loss during migration
- ✅ 20%+ cost reduction

---

**Document Version:** 1.0  
**Last Updated:** February 5, 2026  
**Author:** GitHub Copilot  
**Status:** Ready for Implementation
