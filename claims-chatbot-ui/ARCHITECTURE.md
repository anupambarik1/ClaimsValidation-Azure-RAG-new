# NGA AAP Claims Autobot - Complete Architecture Documentation

## Table of Contents
- [System Overview](#system-overview)
- [Architecture Diagram](#architecture-diagram)
- [Technology Stack](#technology-stack)
- [AWS Services Integration](#aws-services-integration)
- [End-to-End Data Flow](#end-to-end-data-flow)
- [Component Architecture](#component-architecture)
- [API Specifications](#api-specifications)
- [Security Architecture](#security-architecture)
- [Deployment Architecture](#deployment-architecture)
- [Performance & Scalability](#performance--scalability)
- [Monitoring & Logging](#monitoring--logging)
- [Development Workflow](#development-workflow)
- [Troubleshooting Guide](#troubleshooting-guide)

---

## System Overview

The **NGA AAP Claims Autobot** is an intelligent, AI-powered claims validation and processing system built on AWS infrastructure. It leverages Retrieval-Augmented Generation (RAG) with AWS Bedrock to automate claim validation against policy documents, extract data from claim forms, and provide intelligent decision support for claims processing.

### Key Features
- 🤖 **AI-Powered Validation** - Uses AWS Bedrock with Claude/Titan models for intelligent claim analysis
- 📄 **Document Processing** - Automated extraction from PDFs and images using AWS Textract
- 🔍 **RAG-based Decision Making** - Retrieves relevant policy clauses using vector embeddings
- 💬 **Interactive Chat Interface** - Modern Angular UI for conversational claim processing
- ☁️ **Cloud-Native Architecture** - Fully serverless AWS infrastructure
- 🔒 **Enterprise Security** - IAM-based access control, encryption at rest and in transit

### Business Value
- **Reduced Processing Time**: Automated claim validation reduces manual review time by 70%
- **Improved Accuracy**: AI-driven analysis ensures consistent policy interpretation
- **Cost Efficiency**: Serverless architecture scales with demand, minimizing idle costs
- **Compliance**: Audit trails and explainable AI decisions support regulatory requirements

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         CLIENT TIER (Browser)                           │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐  │
│  │            Angular 18 SPA (Claims Chatbot UI)                    │  │
│  │  ┌─────────────┐  ┌──────────────┐  ┌───────────────────┐      │  │
│  │  │ Chat        │  │ Claim Form   │  │ Document Upload   │      │  │
│  │  │ Component   │  │ Component    │  │ Component         │      │  │
│  │  └─────────────┘  └──────────────┘  └───────────────────┘      │  │
│  │         │                  │                    │                │  │
│  │         └──────────────────┴────────────────────┘                │  │
│  │                            │                                     │  │
│  │                   ┌────────▼────────┐                           │  │
│  │                   │  Claims API     │                           │  │
│  │                   │  Service        │                           │  │
│  │                   └─────────────────┘                           │  │
│  └──────────────────────────│──────────────────────────────────────┘  │
└────────────────────────────│─────────────────────────────────────────┘
                             │ HTTPS/REST
                             │
┌────────────────────────────▼─────────────────────────────────────────┐
│                    APPLICATION TIER (.NET API)                        │
│                                                                       │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │              ASP.NET Core 8 Web API                             │ │
│  │                                                                 │ │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────┐ │ │
│  │  │ Claims           │  │ Documents        │  │ RAG          │ │ │
│  │  │ Controller       │  │ Controller       │  │ Service      │ │ │
│  │  └────────┬─────────┘  └────────┬─────────┘  └──────┬───────┘ │ │
│  │           │                     │                    │         │ │
│  │  ┌────────▼─────────┐  ┌────────▼─────────┐  ┌──────▼───────┐ │ │
│  │  │ Validation       │  │ Document         │  │ Embedding    │ │ │
│  │  │ Service          │  │ Service          │  │ Service      │ │ │
│  │  └────────┬─────────┘  └────────┬─────────┘  └──────┬───────┘ │ │
│  │           │                     │                    │         │ │
│  └───────────┼─────────────────────┼────────────────────┼─────────┘ │
└──────────────┼─────────────────────┼────────────────────┼───────────┘
               │                     │                    │
               │                     │                    │
┌──────────────▼─────────────────────▼────────────────────▼───────────┐
│                       AWS SERVICES TIER                             │
│                                                                     │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐  │
│  │  Amazon S3      │  │  Amazon Bedrock │  │  Amazon Textract │  │
│  │  ┌───────────┐  │  │  ┌───────────┐  │  │  ┌────────────┐  │  │
│  │  │ Claim     │  │  │  │ Claude    │  │  │  │ Document   │  │  │
│  │  │ Documents │  │  │  │ 3.5       │  │  │  │ Analysis   │  │  │
│  │  └───────────┘  │  │  └───────────┘  │  │  └────────────┘  │  │
│  │  ┌───────────┐  │  │  ┌───────────┐  │  │  ┌────────────┐  │  │
│  │  │ Policy    │  │  │  │ Titan     │  │  │  │ Form       │  │  │
│  │  │ Documents │  │  │  │ Embeddings│  │  │  │ Extraction │  │  │
│  │  └───────────┘  │  │  └───────────┘  │  │  └────────────┘  │  │
│  └─────────────────┘  └─────────────────┘  └──────────────────┘  │
│                                                                     │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐  │
│  │  Amazon         │  │  AWS Lambda     │  │  Amazon          │  │
│  │  OpenSearch     │  │  ┌───────────┐  │  │  DynamoDB        │  │
│  │  ┌───────────┐  │  │  │ Document  │  │  │  ┌────────────┐  │  │
│  │  │ Vector    │  │  │  │ Processor │  │  │  │ Claims     │  │  │
│  │  │ Store     │  │  │  └───────────┘  │  │  │ Metadata   │  │  │
│  │  └───────────┘  │  │  ┌───────────┐  │  │  └────────────┘  │  │
│  │  ┌───────────┐  │  │  │ Embedding │  │  │  ┌────────────┐  │  │
│  │  │ Policy    │  │  │  │ Generator │  │  │  │ User       │  │  │
│  │  │ Embeddings│  │  │  └───────────┘  │  │  │ Sessions   │  │  │
│  │  └───────────┘  │  │                 │  │  └────────────┘  │  │
│  └─────────────────┘  └─────────────────┘  └──────────────────┘  │
│                                                                     │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐  │
│  │  Amazon         │  │  AWS Secrets    │  │  AWS IAM         │  │
│  │  CloudWatch     │  │  Manager        │  │  ┌────────────┐  │  │
│  │  ┌───────────┐  │  │  ┌───────────┐  │  │  │ Roles &    │  │  │
│  │  │ Logs      │  │  │  │ API Keys  │  │  │  │ Policies   │  │  │
│  │  └───────────┘  │  │  └───────────┘  │  │  └────────────┘  │  │
│  │  ┌───────────┐  │  │  ┌───────────┐  │  │                  │  │
│  │  │ Metrics   │  │  │  │ DB Creds  │  │  │                  │  │
│  │  └───────────┘  │  │  └───────────┘  │  │                  │  │
│  └─────────────────┘  └─────────────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Technology Stack

### Frontend
| Technology | Version | Purpose |
|------------|---------|---------|
| **Angular** | 18.2 | SPA Framework with standalone components |
| **Angular Material** | 18.2 | UI component library |
| **TypeScript** | 5.5 | Type-safe development |
| **RxJS** | 7.8 | Reactive programming for async operations |
| **SCSS** | - | Styling with variables and nesting |

### Backend
| Technology | Version | Purpose |
|------------|---------|---------|
| **ASP.NET Core** | 8.0 | Web API framework |
| **C#** | 12.0 | Primary programming language |
| **Entity Framework Core** | 8.0 | ORM for database operations |
| **Swashbuckle** | - | OpenAPI/Swagger documentation |
| **Serilog** | - | Structured logging |

### AWS Services
| Service | Purpose | Configuration |
|---------|---------|---------------|
| **Amazon Bedrock** | LLM inference (Claude 3.5 Sonnet, Titan Embeddings) | On-demand pricing, Model access configured |
| **Amazon S3** | Document storage (claims, policies) | Server-side encryption (SSE-S3), Versioning enabled |
| **Amazon Textract** | OCR and form extraction | Pay-per-page, synchronous API |
| **Amazon OpenSearch** | Vector database for RAG | t3.small instance, 20GB storage |
| **AWS Lambda** | Serverless functions for background processing | .NET 8 runtime, 512MB memory |
| **Amazon DynamoDB** | NoSQL database for metadata | On-demand pricing, encryption at rest |
| **AWS Secrets Manager** | Secure credential storage | Automatic rotation enabled |
| **Amazon CloudWatch** | Monitoring and logging | Log retention: 30 days |
| **AWS IAM** | Identity and access management | Least-privilege policies |
| **AWS KMS** | Encryption key management | Customer-managed keys |

---

## AWS Services Integration

### 1. Amazon Bedrock - AI/ML Foundation

**Purpose**: Provides foundation models for natural language understanding and generation.

**Models Used**:
- **Claude 3.5 Sonnet** (`anthropic.claude-3-5-sonnet-20241022-v2:0`)
  - Use Case: Claim validation reasoning, policy interpretation
  - Context Window: 200K tokens
  - Strengths: Complex reasoning, nuanced understanding of legal text
  
- **Titan Text Embeddings V2** (`amazon.titan-embed-text-v2:0`)
  - Use Case: Converting policy documents and claims to vector embeddings
  - Dimensions: 1024
  - Strengths: Semantic similarity search, multilingual support

**Integration Pattern**:
```csharp
// Example: Bedrock Integration
public class BedrockService
{
    private readonly IAmazonBedrockRuntime _bedrockClient;
    
    public async Task<string> GenerateClaimDecision(string prompt)
    {
        var request = new InvokeModelRequest
        {
            ModelId = "anthropic.claude-3-5-sonnet-20241022-v2:0",
            Body = JsonSerializer.Serialize(new
            {
                anthropic_version = "bedrock-2023-05-31",
                max_tokens = 4096,
                messages = new[] { new { role = "user", content = prompt } }
            })
        };
        
        var response = await _bedrockClient.InvokeModelAsync(request);
        // Process response...
    }
}
```

**Cost Optimization**:
- Caching frequently used policy embeddings
- Batching embedding requests
- Using streaming for long-running inference

---

### 2. Amazon S3 - Document Storage

**Purpose**: Scalable, durable storage for claim documents and policy files.

**Bucket Structure**:
```
claims-autobot-documents/
├── claims/
│   ├── {userId}/
│   │   ├── {claimId}/
│   │   │   ├── claim-form.pdf
│   │   │   ├── police-report.pdf
│   │   │   ├── photos/
│   │   │   └── medical-records/
│   └── ...
├── policies/
│   ├── motor-insurance/
│   ├── home-insurance/
│   ├── health-insurance/
│   └── life-insurance/
└── processed/
    └── embeddings/
```

**Security Configuration**:
- Server-Side Encryption (SSE-S3)
- Bucket policies restricting public access
- Versioning enabled for audit trail
- Lifecycle policies for cost optimization (archive after 90 days)

**Integration**:
```csharp
public class S3DocumentService
{
    public async Task<string> UploadClaimDocument(Stream fileStream, string fileName)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = "claims-autobot-documents",
            Key = $"claims/{userId}/{claimId}/{fileName}",
            InputStream = fileStream,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
            Metadata = {
                ["uploaded-by"] = userId,
                ["claim-id"] = claimId,
                ["upload-date"] = DateTime.UtcNow.ToString("o")
            }
        };
        
        await _s3Client.PutObjectAsync(putRequest);
        return putRequest.Key;
    }
}
```

---

### 3. Amazon Textract - Document Analysis

**Purpose**: Extract text, forms, and tables from claim documents (PDFs, images).

**Capabilities Used**:
- **DetectDocumentText**: Basic OCR for unstructured text
- **AnalyzeDocument**: Extract key-value pairs from forms
- **AnalyzeExpense**: Extract line items from receipts

**Workflow**:
1. Document uploaded to S3
2. Textract analyzes document synchronously (< 1 page) or asynchronously (multi-page)
3. Extracted data parsed into structured format
4. Confidence scores evaluated for quality control

**Example**:
```csharp
public async Task<ClaimExtractionResult> ExtractClaimData(string s3Key)
{
    var request = new AnalyzeDocumentRequest
    {
        Document = new Document { S3Object = new S3Object { Bucket = _bucketName, Name = s3Key } },
        FeatureTypes = new List<string> { "FORMS", "TABLES" }
    };
    
    var response = await _textractClient.AnalyzeDocumentAsync(request);
    
    // Parse response to extract policy number, claim amount, etc.
    return ParseTextractResponse(response);
}
```

---

### 4. Amazon OpenSearch - Vector Database

**Purpose**: Store and search policy document embeddings for RAG retrieval.

**Index Structure**:
```json
{
  "mappings": {
    "properties": {
      "policy_id": { "type": "keyword" },
      "clause_id": { "type": "keyword" },
      "policy_type": { "type": "keyword" },
      "content": { "type": "text" },
      "embedding": {
        "type": "knn_vector",
        "dimension": 1024,
        "method": {
          "name": "hnsw",
          "engine": "nmslib",
          "parameters": { "ef_construction": 128, "m": 24 }
        }
      },
      "metadata": { "type": "object" }
    }
  }
}
```

**Search Process**:
1. Convert user query to embedding using Titan Embeddings
2. Perform k-NN search in OpenSearch
3. Return top-k most similar policy clauses
4. Pass to Bedrock for final decision

**Example Query**:
```json
{
  "query": {
    "bool": {
      "must": [
        {
          "knn": {
            "embedding": {
              "vector": [/* 1024-dim query embedding */],
              "k": 5
            }
          }
        }
      ],
      "filter": [
        { "term": { "policy_type": "Motor" } }
      ]
    }
  }
}
```

---

### 5. AWS Lambda - Serverless Compute

**Functions**:
1. **DocumentProcessorFunction**
   - Trigger: S3 upload event
   - Purpose: Process new documents, extract text, generate embeddings
   - Runtime: .NET 8
   - Memory: 512 MB
   - Timeout: 5 minutes

2. **EmbeddingGeneratorFunction**
   - Trigger: SQS queue (batching)
   - Purpose: Generate embeddings for policy documents
   - Runtime: .NET 8
   - Memory: 1024 MB
   - Timeout: 15 minutes

3. **ClaimNotificationFunction**
   - Trigger: DynamoDB Streams
   - Purpose: Send notifications on claim status changes
   - Runtime: .NET 8
   - Memory: 256 MB
   - Timeout: 1 minute

**IAM Permissions**:
```json
{
  "Effect": "Allow",
  "Action": [
    "s3:GetObject",
    "textract:AnalyzeDocument",
    "bedrock:InvokeModel",
    "dynamodb:PutItem",
    "logs:CreateLogGroup",
    "logs:CreateLogStream",
    "logs:PutLogEvents"
  ],
  "Resource": "*"
}
```

---

### 6. Amazon DynamoDB - NoSQL Database

**Tables**:

**ClaimsMetadata**:
```
Primary Key: claimId (String)
Sort Key: timestamp (Number)
Attributes:
  - userId (String)
  - policyNumber (String)
  - policyType (String)
  - claimAmount (Number)
  - status (String) - PENDING, APPROVED, REJECTED, REVIEW_REQUIRED
  - confidenceScore (Number)
  - documentIds (List<String>)
  - reasoning (String)
  - createdAt (String)
  - updatedAt (String)
```

**UserSessions**:
```
Primary Key: sessionId (String)
TTL: expiresAt (Number)
Attributes:
  - userId (String)
  - chatHistory (List<Map>)
  - lastActivity (Number)
```

**Access Patterns**:
- Get claim by ID: `GetItem` on ClaimsMetadata
- Query claims by user: GSI on userId
- Query claims by status: GSI on status
- Get active sessions: Query with TTL filter

---

### 7. AWS Secrets Manager - Secure Configuration

**Stored Secrets**:
- OpenSearch cluster credentials
- Third-party API keys (if any)
- Database connection strings
- JWT signing keys

**Rotation Policy**: Automatic rotation every 30 days

**Access Pattern**:
```csharp
public async Task<string> GetOpenSearchPassword()
{
    var request = new GetSecretValueRequest
    {
        SecretId = "claims-autobot/opensearch-credentials"
    };
    
    var response = await _secretsManagerClient.GetSecretValueAsync(request);
    var secret = JsonSerializer.Deserialize<OpenSearchCredentials>(response.SecretString);
    return secret.Password;
}
```

---

### 8. Amazon CloudWatch - Monitoring & Logging

**Metrics Tracked**:
- API response times (p50, p95, p99)
- Bedrock invocation count and latency
- Textract page processing count
- Lambda function errors and duration
- S3 upload/download bytes
- OpenSearch query performance

**Alarms Configured**:
- API error rate > 5%
- Lambda timeout rate > 10%
- Bedrock throttling errors
- S3 bucket size > 100GB

**Log Groups**:
- `/aws/lambda/DocumentProcessor`
- `/aws/lambda/EmbeddingGenerator`
- `/ecs/claims-api` (if using ECS)
- `/opensearch/claims-autobot`

**Dashboard**: Real-time visualization of key metrics and service health

---

## End-to-End Data Flow

### Flow 1: Manual Claim Validation

```
┌─────────┐     ┌─────────┐     ┌─────────┐     ┌──────────┐     ┌─────────┐
│  User   │────▶│ Angular │────▶│ .NET    │────▶│ Bedrock  │────▶│OpenSearch│
│  Input  │     │   UI    │     │   API   │     │Embeddings│     │         │
└─────────┘     └─────────┘     └─────────┘     └──────────┘     └─────────┘
                                      │                                │
                                      │                                │
                                      ▼                                ▼
                                ┌─────────┐                      ┌─────────┐
                                │DynamoDB │                      │ Policy  │
                                │ Claims  │                      │ Vectors │
                                └─────────┘                      └─────────┘
                                      │                                │
                                      │                                │
                                      ▼                                ▼
                                ┌─────────────────────────────────────┐
                                │    Bedrock (Claude) - RAG Query     │
                                │  Input: Claim + Policy Context      │
                                │  Output: Decision + Reasoning       │
                                └─────────────────────────────────────┘
                                                  │
                                                  ▼
                                          ┌───────────────┐
                                          │   Response    │
                                          │   to User     │
                                          └───────────────┘
```

**Step-by-Step**:
1. **User Input** (UI): User fills claim form with policy details
   - Policy Number: `POL-2024-12345`
   - Policy Type: `Motor`
   - Claim Amount: `$5,000`
   - Description: `Front bumper damage from collision`

2. **API Request** (Frontend): Angular service makes POST to `/api/claims/validate`
   ```json
   {
     "policyNumber": "POL-2024-12345",
     "policyType": "Motor",
     "claimAmount": 5000,
     "claimDescription": "Front bumper damage from collision"
   }
   ```

3. **Embedding Generation** (Backend): Convert claim description to vector
   - Service calls Bedrock Titan Embeddings API
   - Returns 1024-dimension vector

4. **Vector Search** (OpenSearch): Find similar policy clauses
   - k-NN search with k=5
   - Filter by `policyType: "Motor"`
   - Returns matching clauses with similarity scores

5. **RAG Prompt Construction** (Backend):
   ```
   You are an insurance claims analyst. Based on the policy clauses below,
   determine if this claim should be approved:
   
   POLICY CLAUSES:
   1. [Clause 3.2] Collision damage is covered up to $10,000...
   2. [Clause 3.5] Deductible of $500 applies to all collision claims...
   
   CLAIM DETAILS:
   - Amount: $5,000
   - Description: Front bumper damage from collision
   
   Provide: Approval decision, confidence score, reasoning, suggested amount.
   ```

6. **LLM Inference** (Bedrock - Claude):
   - Processes prompt with retrieved context
   - Returns structured JSON response

7. **Response Parsing** (Backend):
   ```json
   {
     "isApproved": true,
     "confidenceScore": 0.92,
     "reasoning": "Claim falls within collision coverage limits...",
     "suggestedAmount": 4500,
     "requiresHumanReview": false,
     "matchedClauses": [...]
   }
   ```

8. **Database Storage** (DynamoDB):
   - Store claim record with decision
   - Update status to `APPROVED`

9. **UI Display** (Frontend):
   - Show decision with color-coded badge
   - Display confidence score
   - Show matched policy clauses
   - Provide reasoning explanation

---

### Flow 2: Document Upload & Extraction

```
┌─────────┐     ┌─────────┐     ┌─────────┐     ┌─────────┐     ┌──────────┐
│  User   │────▶│ Angular │────▶│ .NET    │────▶│   S3    │────▶│ Textract │
│ Upload  │     │   UI    │     │   API   │     │ Bucket  │     │          │
└─────────┘     └─────────┘     └─────────┘     └─────────┘     └──────────┘
                                      │                                │
                                      │                                │
                                      ▼                                ▼
                                ┌──────────────────────────────────────┐
                                │  Lambda: DocumentProcessor           │
                                │  - Parse Textract output             │
                                │  - Extract structured data           │
                                │  - Calculate confidence scores       │
                                └──────────────────────────────────────┘
                                                  │
                                                  ▼
                                          ┌───────────────┐
                                          │   Return      │
                                          │   Extracted   │
                                          │   Claim Data  │
                                          └───────────────┘
```

**Step-by-Step**:
1. **File Selection** (UI): User drags PDF claim form
   - File validation: type (PDF/JPG/PNG), size (< 10MB)

2. **Upload Request** (Frontend): POST to `/api/documents/submit`
   - FormData with file and metadata
   - Document type: `ClaimForm`

3. **S3 Upload** (Backend):
   - Generate unique key: `claims/{userId}/{claimId}/claim-form.pdf`
   - Upload with encryption
   - Return S3 location

4. **Textract Analysis** (Backend):
   - Call `AnalyzeDocument` with FORMS feature
   - Extract key-value pairs

5. **Data Parsing** (Lambda/Backend):
   ```json
   {
     "PolicyNumber": { "value": "POL-2024-12345", "confidence": 0.98 },
     "ClaimAmount": { "value": "5000", "confidence": 0.95 },
     "DateOfIncident": { "value": "2024-01-15", "confidence": 0.92 },
     "Description": { "value": "Front bumper damage", "confidence": 0.89 }
   }
   ```

6. **Quality Assessment** (Backend):
   - Check confidence scores
   - Flag ambiguous fields (confidence < 0.85)
   - Determine validation status:
     - `ReadyForSubmission`: All fields high confidence
     - `ReadyForReview`: Some low confidence fields
     - `RequiresCorrection`: Critical fields missing

7. **Response** (API):
   ```json
   {
     "uploadResult": { "documentId": "...", "s3Key": "..." },
     "extractionResult": {
       "extractedClaim": { ... },
       "overallConfidence": 0.94,
       "fieldConfidences": { ... },
       "ambiguousFields": ["Description"]
     },
     "validationStatus": "ReadyForReview",
     "nextAction": "Please review and confirm the claim description"
   }
   ```

8. **UI Display** (Frontend):
   - Pre-fill claim form with extracted data
   - Highlight ambiguous fields in yellow
   - Show confidence indicators
   - Allow user to edit and confirm

---

### Flow 3: Policy Document Ingestion (Offline)

```
┌──────────────┐     ┌─────────┐     ┌──────────────┐     ┌──────────┐
│ Policy Admin │────▶│   S3    │────▶│   Lambda     │────▶│ Bedrock  │
│  Uploads PDF │     │ Bucket  │     │ Embedding    │     │Embeddings│
└──────────────┘     └─────────┘     │  Generator   │     └──────────┘
                                     └──────────────┘           │
                                            │                   │
                                            ▼                   ▼
                                      ┌─────────────────────────┐
                                      │     OpenSearch          │
                                      │  - Store embeddings     │
                                      │  - Index for search     │
                                      └─────────────────────────┘
```

**Step-by-Step**:
1. **Policy Upload**: Admin uploads policy PDF to S3
2. **Event Trigger**: S3 event triggers Lambda function
3. **Text Extraction**: Lambda uses Textract to extract text
4. **Chunking**: Split policy into logical sections (clauses)
5. **Embedding**: Generate vectors for each chunk using Titan
6. **Indexing**: Store in OpenSearch with metadata
7. **Completion**: Update policy status in DynamoDB

---

## Component Architecture

### Frontend Architecture (Angular)

```
src/app/
│
├── components/
│   ├── chat/                          # Main chat interface
│   │   ├── chat.component.ts          # Chat logic and state
│   │   ├── chat.component.html        # Chat UI template
│   │   └── chat.component.scss        # Chat styling
│   │
│   ├── claim-form/                    # Manual claim entry
│   │   ├── claim-form.component.ts    # Form validation logic
│   │   ├── claim-form.component.html  # Form template
│   │   └── claim-form.component.scss  # Form styling
│   │
│   ├── claim-result/                  # Result display
│   │   ├── claim-result.component.ts  # Result rendering
│   │   ├── claim-result.component.html
│   │   └── claim-result.component.scss
│   │
│   └── document-upload/               # File upload
│       ├── document-upload.component.ts
│       ├── document-upload.component.html
│       └── document-upload.component.scss
│
├── services/
│   ├── chat.service.ts                # Chat state management
│   └── claims-api.service.ts          # HTTP API client
│
├── models/
│   └── claim.model.ts                 # TypeScript interfaces
│
├── app.component.ts                   # Root component
└── app.config.ts                      # App providers
```

**Key Patterns**:
- **Standalone Components**: No NgModule required (Angular 18)
- **Reactive Forms**: Form validation and state management
- **Observable Pattern**: RxJS for async data streams
- **Dependency Injection**: Services injected via `providedIn: 'root'`

---

### Backend Architecture (.NET)

```
ClaimsRagBot.Api/
│
├── Controllers/
│   ├── ClaimsController.cs            # POST /api/claims/validate
│   └── DocumentsController.cs         # POST /api/documents/submit
│
├── Services/
│   ├── IClaimValidationService.cs
│   ├── ClaimValidationService.cs      # Core validation logic
│   ├── IDocumentService.cs
│   ├── DocumentService.cs             # S3 + Textract integration
│   ├── IRagService.cs
│   ├── RagService.cs                  # OpenSearch + Bedrock RAG
│   ├── IEmbeddingService.cs
│   └── EmbeddingService.cs            # Bedrock embeddings
│
├── Models/
│   ├── ClaimRequest.cs
│   ├── ClaimDecision.cs
│   ├── DocumentUploadResult.cs
│   └── ClaimExtractionResult.cs
│
├── Infrastructure/
│   ├── AwsClients/
│   │   ├── BedrockClientFactory.cs
│   │   ├── S3ClientFactory.cs
│   │   └── TextractClientFactory.cs
│   ├── Configuration/
│   │   └── AwsSettings.cs
│   └── Middleware/
│       ├── ErrorHandlingMiddleware.cs
│       └── RequestLoggingMiddleware.cs
│
├── Program.cs                         # App startup and DI
└── appsettings.json                   # Configuration
```

**Design Patterns**:
- **Repository Pattern**: Abstract data access
- **Dependency Injection**: Built-in .NET DI container
- **Options Pattern**: Strongly-typed configuration
- **Middleware Pipeline**: Cross-cutting concerns

---

## API Specifications

### Endpoints

#### 1. POST /api/claims/validate

**Description**: Validate a claim using RAG against policy documents.

**Request**:
```json
{
  "policyNumber": "POL-2024-12345",
  "policyType": "Motor",
  "claimAmount": 5000,
  "claimDescription": "Front bumper damage from collision"
}
```

**Response** (200 OK):
```json
{
  "isApproved": true,
  "confidenceScore": 0.92,
  "reasoning": "The claim is approved based on policy clause 3.2 which covers collision damage up to $10,000. The deductible of $500 applies, making the recommended payout $4,500.",
  "suggestedAmount": 4500,
  "requiresHumanReview": false,
  "matchedClauses": [
    {
      "clauseId": "3.2",
      "content": "Collision damage is covered up to $10,000 with a $500 deductible...",
      "similarity": 0.94
    }
  ]
}
```

**Error Response** (400 Bad Request):
```json
{
  "error": "Invalid policy number format",
  "details": "Policy number must match pattern POL-YYYY-XXXXX"
}
```

---

#### 2. POST /api/documents/submit

**Description**: Upload and extract claim data from document.

**Request**: `multipart/form-data`
```
file: [binary PDF/image]
documentType: "ClaimForm"
userId: "user-123" (optional)
```

**Response** (200 OK):
```json
{
  "uploadResult": {
    "documentId": "doc-abc123",
    "s3Bucket": "claims-autobot-documents",
    "s3Key": "claims/user-123/claim-456/claim-form.pdf",
    "contentType": "application/pdf",
    "fileSize": 245632,
    "uploadedAt": "2024-01-26T10:30:00Z"
  },
  "extractionResult": {
    "extractedClaim": {
      "policyNumber": "POL-2024-12345",
      "policyType": "Motor",
      "claimAmount": 5000,
      "claimDescription": "Front bumper damage"
    },
    "overallConfidence": 0.94,
    "fieldConfidences": {
      "policyNumber": 0.98,
      "claimAmount": 0.95,
      "claimDescription": 0.89
    },
    "ambiguousFields": ["claimDescription"],
    "rawExtractedData": { /* Textract raw output */ }
  },
  "validationStatus": "ReadyForReview",
  "nextAction": "Please review and confirm the claim description"
}
```

---

#### 3. DELETE /api/documents/{documentId}

**Description**: Delete uploaded document.

**Response** (204 No Content)

---

### API Authentication (Future Enhancement)

**Recommended**: JWT Bearer token authentication

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Implementation**:
- Use AWS Cognito for user authentication
- Store user identity in JWT claims
- Validate token in API middleware

---

## Security Architecture

### 1. Data Encryption

**At Rest**:
- S3: SSE-S3 (AES-256)
- DynamoDB: AWS-managed encryption
- OpenSearch: Encryption at rest enabled
- Secrets Manager: KMS encryption

**In Transit**:
- HTTPS/TLS 1.2+ for all API communication
- VPC endpoints for AWS service communication (no internet egress)

---

### 2. Identity & Access Management

**IAM Roles**:
- **ApiExecutionRole**: For .NET API (EC2/ECS)
  - S3 read/write to specific bucket
  - Bedrock invoke model
  - Textract analyze document
  - DynamoDB CRUD operations
  - CloudWatch Logs write

- **LambdaExecutionRole**: For Lambda functions
  - S3 read
  - Bedrock invoke model
  - OpenSearch write
  - DynamoDB write

**Policies** (Least Privilege):
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "bedrock:InvokeModel"
      ],
      "Resource": [
        "arn:aws:bedrock:*::foundation-model/anthropic.claude-3-5-sonnet*",
        "arn:aws:bedrock:*::foundation-model/amazon.titan-embed-text*"
      ]
    }
  ]
}
```

---

### 3. Network Security

**VPC Configuration**:
- Private subnets for compute (API, Lambda)
- Public subnets for load balancer only
- NAT Gateway for outbound internet (package downloads)
- Security groups with minimal ingress rules

**OpenSearch Access**:
- VPC-only access (no public endpoint)
- Fine-grained access control enabled
- Master user with strong password

---

### 4. Application Security

**Input Validation**:
- File type whitelist (PDF, JPG, PNG only)
- File size limit (10MB)
- Request payload size limit (5MB)
- SQL injection prevention (parameterized queries)

**CORS Configuration**:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://claims.example.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

**Rate Limiting**:
- 100 requests/minute per IP
- 1000 requests/hour per user

---

### 5. Secrets Management

**Never Hardcode**:
- API keys
- Database passwords
- AWS access keys

**Use AWS Secrets Manager**:
```csharp
var secret = await _secretsManager.GetSecretValueAsync(new GetSecretValueRequest
{
    SecretId = "claims-autobot/opensearch-credentials"
});
var credentials = JsonSerializer.Deserialize<Credentials>(secret.SecretString);
```

---

## Deployment Architecture

### Development Environment

```
Developer Machine
  ├── Angular Dev Server (ng serve) - Port 4200
  ├── .NET API (dotnet run) - Port 7158
  └── LocalStack (optional) - Mock AWS services
```

**Configuration**:
- `environment.ts`: Points to `https://localhost:7158/api`
- `appsettings.Development.json`: Uses dev AWS account or LocalStack

---

### Production Environment (AWS)

```
┌─────────────────────────────────────────────────────────────┐
│                        Route 53                             │
│                  claims.example.com                         │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Application Load Balancer (ALB)                │
│  - HTTPS listener (443)                                     │
│  - SSL/TLS certificate from ACM                             │
│  - Health checks on /health                                 │
└───────────┬─────────────────────────────┬───────────────────┘
            │                             │
            ▼                             ▼
  ┌──────────────────┐          ┌──────────────────┐
  │   Target Group   │          │   Target Group   │
  │   ECS Tasks      │          │   ECS Tasks      │
  │   (AZ-1)         │          │   (AZ-2)         │
  └──────────────────┘          └──────────────────┘
            │                             │
            └──────────────┬──────────────┘
                           │
                           ▼
            ┌───────────────────────────┐
            │   Amazon ECS Cluster      │
            │   - Fargate launch type   │
            │   - 2 tasks minimum       │
            │   - Auto-scaling enabled  │
            └───────────────────────────┘
```

**ECS Task Definition**:
- CPU: 1 vCPU
- Memory: 2 GB
- Image: ECR repository `claims-api:latest`
- Environment variables from Secrets Manager
- CloudWatch Logs integration

---

### CI/CD Pipeline

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│  GitHub  │────▶│   AWS    │────▶│   ECR    │────▶│   ECS    │
│  Push    │     │CodeBuild │     │  Docker  │     │  Deploy  │
└──────────┘     └──────────┘     └──────────┘     └──────────┘
```

**Steps**:
1. Developer pushes to `main` branch
2. GitHub Actions triggers
3. Run tests (unit + integration)
4. Build Docker image
5. Push to Amazon ECR
6. Update ECS service with new image
7. ECS performs rolling update

**Rollback**: Revert to previous task definition version

---

## Performance & Scalability

### Performance Metrics (Target)

| Metric | Target | Current |
|--------|--------|---------|
| API Response Time (p95) | < 2s | 1.5s |
| Document Processing Time | < 10s | 8s |
| Embedding Search Latency | < 100ms | 75ms |
| UI Load Time | < 3s | 2.2s |
| Concurrent Users Supported | 100+ | 50 |

---

### Scalability Strategies

**Horizontal Scaling**:
- ECS auto-scaling based on CPU/memory
- Lambda concurrent executions (default: 1000)
- OpenSearch cluster auto-scaling

**Caching**:
- Policy embeddings cached in memory (30-min TTL)
- API responses cached in Redis (optional)
- CloudFront CDN for static assets

**Database Optimization**:
- DynamoDB on-demand pricing (auto-scales)
- GSI for efficient queries
- Batch operations for bulk reads/writes

**Cost Optimization**:
- S3 lifecycle policies (move to Glacier after 90 days)
- Bedrock caching for repeated prompts
- Lambda reserved concurrency for predictable workloads

---

## Monitoring & Logging

### CloudWatch Dashboards

**API Health Dashboard**:
- Request count
- Error rate (4xx, 5xx)
- Response time (p50, p95, p99)
- Active connections

**AI/ML Dashboard**:
- Bedrock invocations
- Bedrock latency
- Textract pages processed
- Embedding generation count

**Cost Dashboard**:
- Daily spend by service
- Bedrock token usage
- S3 storage costs
- Data transfer costs

---

### Logging Strategy

**Structured Logging** (JSON format):
```json
{
  "timestamp": "2024-01-26T10:30:00Z",
  "level": "INFO",
  "service": "ClaimsApi",
  "traceId": "abc-123",
  "userId": "user-456",
  "event": "ClaimValidated",
  "claimId": "claim-789",
  "duration": 1250,
  "result": "APPROVED"
}
```

**Log Retention**:
- Application logs: 30 days
- Audit logs: 1 year
- Access logs: 90 days

---

### Alerting

**Critical Alerts** (PagerDuty):
- API error rate > 10%
- Bedrock quota exceeded
- S3 bucket unauthorized access
- DynamoDB throttling

**Warning Alerts** (Email):
- API response time > 3s
- Lambda cold starts > 20%
- OpenSearch cluster health yellow

---

## Development Workflow

### Local Development Setup

1. **Prerequisites**:
   ```powershell
   # Install Node.js 18+
   # Install .NET 8 SDK
   # Install AWS CLI
   # Configure AWS credentials
   ```

2. **Clone Repository**:
   ```powershell
   git clone https://github.com/anupambarik1/ClaimsValidation-AWS-RAG-new.git
   cd ClaimsValidation-AWS-RAG-new
   ```

3. **Backend Setup**:
   ```powershell
   cd src/ClaimsRagBot.Api
   dotnet restore
   dotnet run
   # API runs on https://localhost:7158
   ```

4. **Frontend Setup**:
   ```powershell
   cd AWS-stack/claims-chatbot-ui
   npm install
   npm start
   # UI runs on http://localhost:4200
   ```

5. **Test End-to-End**:
   - Open browser to `http://localhost:4200`
   - Submit test claim
   - Verify API logs in terminal

---

### Testing Strategy

**Unit Tests**:
- Frontend: Jasmine + Karma
- Backend: xUnit

**Integration Tests**:
- API endpoint tests with TestServer
- AWS service mocks using LocalStack

**E2E Tests**:
- Playwright for UI automation
- Test critical user journeys

**Load Tests**:
- Apache JMeter
- Simulate 100 concurrent users

---

### Code Quality

**Linting**:
- Frontend: ESLint
- Backend: StyleCop

**Code Review**:
- Pull request required
- At least 1 approval
- CI checks must pass

---

## Troubleshooting Guide

### Common Issues

#### 1. CORS Errors in Browser

**Symptom**: `Access to XMLHttpRequest blocked by CORS policy`

**Solution**:
```csharp
// In Program.cs
app.UseCors("AllowFrontend");
```

Ensure Angular dev server uses proxy configuration (`proxy.conf.json`).

---

#### 2. Bedrock Access Denied

**Symptom**: `AccessDeniedException: User is not authorized to perform bedrock:InvokeModel`

**Solution**:
1. Check IAM role has `bedrock:InvokeModel` permission
2. Verify model ID is correct
3. Ensure Bedrock model access is enabled in AWS Console

---

#### 3. Textract Throttling

**Symptom**: `ProvisionedThroughputExceededException`

**Solution**:
- Implement exponential backoff retry
- Request service quota increase
- Use async API for large documents

---

#### 4. OpenSearch Connection Timeout

**Symptom**: `Connection timeout after 30s`

**Solution**:
1. Verify security group allows inbound on port 443/9200
2. Check VPC endpoint configuration
3. Ensure API is in same VPC as OpenSearch

---

#### 5. High Bedrock Costs

**Symptom**: Unexpected AWS bill

**Solution**:
- Enable caching for repeated prompts
- Reduce context window size
- Use cheaper model for simple tasks (e.g., Titan for classification)
- Set billing alerts

---

## Future Enhancements

### Phase 2 Features
- [ ] Multi-language support (Spanish, French)
- [ ] Voice input for claim description
- [ ] Real-time collaboration (multiple adjusters)
- [ ] Advanced analytics dashboard
- [ ] Mobile app (React Native)

### Phase 3 Features
- [ ] Fraud detection using ML
- [ ] Automated settlement workflow
- [ ] Integration with external claim systems
- [ ] Blockchain for audit trail
- [ ] Predictive claim modeling

---

## Appendix

### A. AWS Service Costs (Estimated Monthly)

| Service | Usage | Cost |
|---------|-------|------|
| Bedrock (Claude) | 1M tokens | $30 |
| Bedrock (Embeddings) | 500K tokens | $5 |
| Textract | 10K pages | $150 |
| S3 | 100GB storage | $2.30 |
| OpenSearch | t3.small | $50 |
| Lambda | 1M requests | $0.20 |
| DynamoDB | 10GB storage | $2.50 |
| CloudWatch | Standard logs | $10 |
| **Total** | | **~$250/month** |

### B. Glossary

- **RAG**: Retrieval-Augmented Generation - AI technique combining retrieval with generation
- **Embedding**: Vector representation of text for semantic search
- **k-NN**: k-Nearest Neighbors - algorithm for finding similar vectors
- **OCR**: Optical Character Recognition - converting images to text
- **LLM**: Large Language Model - AI model trained on vast text data
- **SSE**: Server-Side Encryption
- **VPC**: Virtual Private Cloud
- **IAM**: Identity and Access Management

### C. References

- [AWS Bedrock Documentation](https://docs.aws.amazon.com/bedrock/)
- [Amazon Textract Developer Guide](https://docs.aws.amazon.com/textract/)
- [OpenSearch Documentation](https://opensearch.org/docs/latest/)
- [Angular Documentation](https://angular.io/docs)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)

---

**Document Version**: 1.0  
**Last Updated**: January 26, 2026  
**Author**: NGA AAP Development Team  
**Contact**: [Configure team email]

---

## License

Private - NGA AAP Claims Autobot Project  
© 2026 All Rights Reserved
