# Claims RAG Bot - Integrated Execution Flow Documentation

**Version:** 1.0  
**Date:** January 30, 2026  
**Purpose:** Complete step-by-step execution flow from UI to AWS services

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Execution Flow - Manual Claim Entry](#execution-flow---manual-claim-entry)
3. [Execution Flow - Document Upload](#execution-flow---document-upload)
4. [AWS Services Integration](#aws-services-integration)
5. [Data Storage & DynamoDB Tables](#data-storage--dynamodb-tables)
6. [Business Rules & Validation Layer](#business-rules--validation-layer)
7. [Mock vs. Real Services](#mock-vs-real-services)
8. [Complete Flow Diagrams](#complete-flow-diagrams)

---

## Architecture Overview

### Technology Stack

**Frontend (Angular 18+)**
- Standalone Components Architecture
- Angular Material UI
- RxJS for Reactive State Management
- HttpClient for API Communication

**Backend (.NET 8 Web API)**
- ASP.NET Core Minimal API
- Dependency Injection
- Clean Architecture Pattern (Core → Application → Infrastructure → API)

**AWS Services**
- **Amazon Bedrock** (Claude Sonnet 3.5 v2) - LLM & Embeddings
- **Amazon OpenSearch Serverless** - Vector Database for RAG
- **Amazon Textract** - Document Text/Form Extraction
- **Amazon Comprehend** - NLP Entity Recognition
- **Amazon Rekognition** - Image Analysis (damage photos)
- **Amazon S3** - Document Storage
- **Amazon DynamoDB** - Audit Trail Storage

---

## Execution Flow - Manual Claim Entry

### Flow Overview
```
User → ClaimFormComponent → ClaimsApiService → ClaimsController 
→ ClaimValidationOrchestrator → AWS Services → DynamoDB Audit → Response
```

### Detailed Step-by-Step Execution

#### **STEP 1: User Interaction (Angular UI)**

**Component:** `ClaimFormComponent` (`claims-chatbot-ui/src/app/components/claim-form/claim-form.component.ts`)

**User Actions:**
1. User fills out the claim form:
   - Policy Number (e.g., `POL-2024-001`)
   - Policy Type (Motor/Health/Home/Life)
   - Claim Amount (e.g., `$5000`)
   - Claim Description (min 20 chars)

2. User clicks "Submit Claim" button

**Code Execution:**
```typescript
submitClaim(): void {
  if (this.claimForm.valid) {
    this.isSubmitting = true;
    const claim: ClaimRequest = {
      policyNumber: this.claimForm.value.policyNumber,
      policyType: this.claimForm.value.policyType,
      claimAmount: parseFloat(this.claimForm.value.claimAmount),
      claimDescription: this.claimForm.value.claimDescription
    };

    this.claimSubmitted.emit(claim); // Emits to parent ChatComponent
  }
}
```

**Data Structure Created:**
```typescript
interface ClaimRequest {
  policyNumber: string;      // "POL-2024-001"
  policyType: string;        // "Motor"
  claimAmount: number;       // 5000
  claimDescription: string;  // "Vehicle collision on highway..."
}
```

---

#### **STEP 2: Parent Component Handles Emission**

**Component:** `ChatComponent` (`claims-chatbot-ui/src/app/components/chat/chat.component.ts`)

**Code Execution:**
```typescript
handleClaimSubmit(claim: ClaimRequest): void {
  this.isLoading = true;
  
  // Add user message to chat
  this.chatService.addUserMessage(
    `Validating claim:\nPolicy: ${claim.policyNumber}\nType: ${claim.policyType}...`,
    'claim',
    claim
  );

  // Call API service
  this.apiService.validateClaim(claim).subscribe({
    next: (result) => { /* Handle success */ },
    error: (error) => { /* Handle error */ }
  });
}
```

---

#### **STEP 3: HTTP Request to .NET API**

**Service:** `ClaimsApiService` (`claims-chatbot-ui/src/app/services/claims-api.service.ts`)

**Code Execution:**
```typescript
validateClaim(claim: ClaimRequest): Observable<ClaimDecision> {
  return this.http.post<ClaimDecision>(
    `${this.baseUrl}/claims/validate`,  // http://localhost:5000/api/claims/validate
    claim
  );
}
```

**HTTP Request:**
- **Method:** POST
- **URL:** `http://localhost:5000/api/claims/validate`
- **Headers:** `Content-Type: application/json`
- **Body:**
```json
{
  "policyNumber": "POL-2024-001",
  "policyType": "Motor",
  "claimAmount": 5000,
  "claimDescription": "Vehicle collision on highway resulting in front-end damage"
}
```

---

#### **STEP 4: .NET API Controller Receives Request**

**Controller:** `ClaimsController` (`src/ClaimsRagBot.Api/Controllers/ClaimsController.cs`)

**Code Execution:**
```csharp
[HttpPost("validate")]
public async Task<ActionResult<ClaimDecision>> ValidateClaim([FromBody] ClaimRequest request)
{
    _logger.LogInformation(
        "Validating claim for policy {PolicyNumber}, amount: ${Amount}",
        request.PolicyNumber,
        request.ClaimAmount
    );

    var decision = await _orchestrator.ValidateClaimAsync(request);

    _logger.LogInformation(
        "Claim validated: {PolicyNumber}, Status: {Status}, Confidence: {Confidence:F2}",
        request.PolicyNumber,
        decision.Status,
        decision.ConfidenceScore
    );

    return Ok(decision);
}
```

**Dependencies Injected:**
- `ClaimValidationOrchestrator` - Main orchestration logic
- `ILogger<ClaimsController>` - Logging

---

#### **STEP 5: RAG Orchestration Layer**

**Orchestrator:** `ClaimValidationOrchestrator` (`src/ClaimsRagBot.Application/RAG/ClaimValidationOrchestrator.cs`)

This is the **core RAG pipeline**. It executes these sub-steps:

##### **STEP 5.1: Generate Embedding (Amazon Bedrock)**

**Service:** `EmbeddingService` (`src/ClaimsRagBot.Infrastructure/Bedrock/EmbeddingService.cs`)

**Code:**
```csharp
public async Task<ClaimDecision> ValidateClaimAsync(ClaimRequest request)
{
    // Step 1: Generate embedding for claim description
    var embedding = await _embeddingService.GenerateEmbeddingAsync(request.ClaimDescription);
    // Returns: float[1536] vector
}
```

**AWS Service Called:** Amazon Bedrock - Titan Embeddings v1
- **Model:** `amazon.titan-embed-text-v1`
- **Input:** Claim description text
- **Output:** 1536-dimensional embedding vector

**Bedrock API Call:**
```csharp
var requestBody = new { inputText = text };
var request = new InvokeModelRequest {
    ModelId = "amazon.titan-embed-text-v1",
    Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestBody))),
    ContentType = "application/json"
};
var response = await _client.InvokeModelAsync(request);
```

**Output:** `float[1536]` embedding vector

---

##### **STEP 5.2: Retrieve Relevant Policy Clauses (Amazon OpenSearch)**

**Service:** `RetrievalService` (`src/ClaimsRagBot.Infrastructure/OpenSearch/RetrievalService.cs`)

**Code:**
```csharp
// Step 2: Retrieve relevant policy clauses
var clauses = await _retrievalService.RetrieveClausesAsync(embedding, request.PolicyType);
```

**OpenSearch Query:**
```json
{
  "size": 5,
  "query": {
    "bool": {
      "must": [{
        "knn": {
          "embedding": {
            "vector": [/* 1536 floats */],
            "k": 5
          }
        }
      }],
      "filter": [{
        "term": { "policyType": "motor" }
      }]
    }
  }
}
```

**AWS Service:** Amazon OpenSearch Serverless
- **Index:** `policy-clauses`
- **Search Type:** K-Nearest Neighbors (KNN) vector similarity
- **Authentication:** AWS SigV4

**Mock Fallback:** If OpenSearch is not configured, returns mock policy clauses:
```csharp
private List<PolicyClause> GetMotorPolicyClauses()
{
    return new List<PolicyClause>
    {
        new PolicyClause(
            ClauseId: "MOTOR-001",
            Text: "Collision Coverage: Covers damage from vehicle-to-vehicle or vehicle-to-object collisions up to $50,000 per incident.",
            CoverageType: "Collision",
            Score: 0.95f
        ),
        // ... more clauses
    };
}
```

**Output:** List of 5 most relevant policy clauses with similarity scores

---

#### **Understanding OpenSearch Serverless & Mock Fallback (Deep Dive)**

This section explains how the system retrieves relevant policy information to validate claims using vector similarity search.

##### **What is Amazon OpenSearch Serverless?**

Amazon OpenSearch Serverless is a **vector database** that enables semantic search across policy documents. Unlike traditional keyword search, it understands the **meaning** of text.

**Key Capabilities:**
- Stores policy documents as numerical vectors (embeddings)
- Performs semantic similarity searches
- Finds the most contextually relevant policy clauses for any given claim
- Automatically scales without infrastructure management

##### **The Vector Search Process**

**1. Indexing Phase (One-time Setup)**

When policy documents are ingested into the system:

```
Policy Document Text:
"Collision Coverage: Covers damage from vehicle-to-vehicle or 
vehicle-to-object collisions up to $50,000 per incident."

        ↓ (Amazon Bedrock Titan Embeddings)
        
Vector Embedding (1536 dimensions):
[0.234, 0.876, 0.543, 0.198, -0.432, 0.678, ...]

        ↓ (Stored in OpenSearch)
        
Index: policy-clauses
Document ID: MOTOR-001
Fields: {
  clauseId: "MOTOR-001",
  text: "Collision Coverage: Covers damage...",
  coverageType: "Collision",
  policyType: "motor",
  embedding: [0.234, 0.876, ...] // 1536 numbers
}
```

**2. Search Phase (Every Claim)**

When a claim is submitted:

```
Claim Description:
"My car was damaged in an accident with another vehicle on the highway"

        ↓ (Convert to embedding)
        
[0.245, 0.891, 0.537, 0.203, -0.428, 0.685, ...]

        ↓ (K-Nearest Neighbors search in OpenSearch)
        
Find 5 policy vectors closest to claim vector:
1. MOTOR-001: Collision Coverage (95% similarity)
2. MOTOR-003: Liability Exclusions (88% similarity)
3. MOTOR-004: Towing Benefits (82% similarity)
4. MOTOR-002: Comprehensive Coverage (78% similarity)
5. MOTOR-005: Glass Coverage (75% similarity)
```

##### **How K-Nearest Neighbors (KNN) Works**

KNN is the algorithm that finds similar items by calculating distance between vectors:

```
Visual Representation (simplified to 2D):

         Policy Vectors in Vector Space
         
    Comprehensive •
                    \
                     \
    Claim Vector •    \
                  \    • Collision (CLOSEST)
                   \  /
                    •
                 Liability
                    
    Distance Calculation:
    - Collision: 0.05 units → 95% similarity
    - Liability: 0.12 units → 88% similarity
    - Comprehensive: 0.22 units → 78% similarity
```

**The KNN Query Sent to OpenSearch:**

```json
{
  "size": 5,  // Return top 5 results
  "query": {
    "bool": {
      "must": [{
        "knn": {
          "embedding": {
            "vector": [0.245, 0.891, ...],  // Your claim embedding
            "k": 5  // Find 5 nearest neighbors
          }
        }
      }],
      "filter": [{
        "term": { 
          "policyType": "motor"  // Only search motor policies
        }
      }]
    }
  },
  "_source": ["clauseId", "text", "coverageType", "policyType"]
}
```

**What This Query Does:**
1. Searches the `policy-clauses` index
2. Compares the claim vector against all policy clause vectors
3. Filters results to only "motor" policy type
4. Returns the 5 most similar clauses with their similarity scores
5. Fetches only the specified fields (not the full embedding vector)

##### **AWS SigV4 Authentication**

Every request to OpenSearch must be cryptographically signed to prove authorization:

```csharp
private async Task SignRequestAsync(HttpRequestMessage request)
{
    // Retrieve AWS credentials
    var creds = await _credentials.GetCredentialsAsync();
    
    // Add security token to request headers
    request.Headers.Add("X-Amz-Security-Token", creds.Token);
    
    // In production, full SigV4 signing includes:
    // - Request timestamp
    // - Request body hash
    // - Canonical request string
    // - Signature calculation with secret key
}
```

**Credential Chain Priority:**
1. AccessKeyId + SecretAccessKey from `appsettings.json`
2. Environment variables (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`)
3. AWS credentials file (`~/.aws/credentials`)
4. IAM role (automatic when running on AWS services)

##### **Mock Fallback System**

The system includes a **fallback mechanism** that provides hardcoded test data when OpenSearch is not available.

**When Mock Data is Used:**
1. OpenSearch endpoint not configured in `appsettings.json`
2. OpenSearch service is unavailable (network error, service down)
3. Authentication fails
4. Development/testing without AWS infrastructure

**Implementation:**

```csharp
public async Task<List<PolicyClause>> RetrieveClausesAsync(
    float[] embedding, 
    string policyType)
{
    // Check if OpenSearch is configured
    if (!_useRealOpenSearch)
    {
        return await GetMockClausesAsync(policyType);
    }

    try
    {
        // Attempt real OpenSearch query
        return await QueryOpenSearchAsync(embedding, policyType);
    }
    catch (Exception ex)
    {
        // Fallback on failure
        Console.WriteLine($"OpenSearch failed: {ex.Message}");
        return await GetMockClausesAsync(policyType);
    }
}

private List<PolicyClause> GetMotorPolicyClauses()
{
    return new List<PolicyClause>
    {
        new PolicyClause(
            ClauseId: "MOT-001",
            Text: "Collision coverage applies to damage from accidents " +
                  "with other vehicles or objects. Deductible: $500. " +
                  "Maximum coverage: Actual cash value of vehicle.",
            CoverageType: "Collision",
            Score: 0.92f  // Simulated similarity score
        ),
        new PolicyClause(
            ClauseId: "MOT-002",
            Text: "Comprehensive coverage includes theft, vandalism, " +
                  "weather damage, and animal collisions. Deductible: $250.",
            CoverageType: "Comprehensive",
            Score: 0.88f
        ),
        new PolicyClause(
            ClauseId: "MOT-003",
            Text: "Liability coverage excludes: intentional damage, racing, " +
                  "driving under influence, or use for commercial delivery without rider.",
            CoverageType: "Exclusions",
            Score: 0.85f
        ),
        new PolicyClause(
            ClauseId: "MOT-004",
            Text: "Towing and rental reimbursement up to $75/day for " +
                  "maximum 10 days following covered loss.",
            CoverageType: "Additional Benefits",
            Score: 0.80f
        ),
        new PolicyClause(
            ClauseId: "MOT-005",
            Text: "Glass damage (windshield, windows) covered with $100 " +
                  "deductible waiver for repair, full deductible for replacement.",
            CoverageType: "Glass Coverage",
            Score: 0.78f
        )
    };
}
```

**Mock Data Structure:**

Each `PolicyClause` contains:
- **ClauseId**: Unique identifier (e.g., "MOT-001")
- **Text**: The actual policy clause content
- **CoverageType**: Category (Collision, Comprehensive, etc.)
- **Score**: Simulated similarity score (0.0 to 1.0)

**Why Mock Data is Valuable:**

| Use Case | Benefit |
|----------|---------|
| **Local Development** | Work without AWS account or internet connectivity |
| **Unit Testing** | Predictable, repeatable test data |
| **Demos** | Show functionality without infrastructure costs |
| **CI/CD Pipelines** | Run tests without external dependencies |
| **Debugging** | Isolate issues in business logic vs. AWS integration |
| **Cost Savings** | Avoid OpenSearch queries during development |

##### **Production vs. Development Flow Comparison**

**Production (with OpenSearch):**
```
User Claim
    ↓
Convert to Vector [0.23, 0.87, ...]
    ↓
Query OpenSearch with KNN
    ↓
Calculate similarity scores against 10,000+ policy clauses
    ↓
Return top 5 matches:
  1. Collision Coverage (95.3% match)
  2. Liability Terms (88.7% match)
  3. Deductible Info (82.1% match)
  4. Exclusions (78.9% match)
  5. Towing Benefits (75.4% match)
```

**Development (Mock Fallback):**
```
User Claim
    ↓
Skip vector conversion (no Bedrock call needed)
    ↓
Return hardcoded motor policy clauses:
  1. MOT-001: Collision (92% simulated)
  2. MOT-002: Comprehensive (88% simulated)
  3. MOT-003: Exclusions (85% simulated)
  4. MOT-004: Towing (80% simulated)
  5. MOT-005: Glass (78% simulated)
```

##### **Configuration Check**

The system determines which mode to use based on configuration:

```csharp
public RetrievalService(IConfiguration? configuration = null)
{
    // Read OpenSearch endpoint from config
    _opensearchEndpoint = configuration?["AWS:OpenSearchEndpoint"] ?? "";
    _indexName = configuration?["AWS:OpenSearchIndexName"] ?? "policy-clauses";
    
    // If endpoint is empty, use mock mode
    _useRealOpenSearch = !string.IsNullOrEmpty(_opensearchEndpoint);
    
    Console.WriteLine(_useRealOpenSearch 
        ? "✓ Using real OpenSearch" 
        : "⚠ Using mock policy data (OpenSearch not configured)");
}
```

**appsettings.json Example:**

```json
// Production Configuration
{
  "AWS": {
    "OpenSearchEndpoint": "https://abc123.us-east-1.aoss.amazonaws.com",
    "OpenSearchIndexName": "policy-clauses"
  }
}

// Development Configuration (triggers mock mode)
{
  "AWS": {
    "OpenSearchEndpoint": "",  // Empty = use mock
    "OpenSearchIndexName": "policy-clauses"
  }
}
```

##### **Similarity Score Interpretation**

The **Score** value represents how relevant a policy clause is to the claim:

| Score Range | Interpretation | Action |
|-------------|----------------|--------|
| **0.90 - 1.00** | Highly relevant | Strong match, likely applicable |
| **0.80 - 0.89** | Moderately relevant | Good match, consider for decision |
| **0.70 - 0.79** | Somewhat relevant | Weak match, use with caution |
| **< 0.70** | Low relevance | Likely not applicable |

**Example in Context:**

```
Claim: "My car was damaged in a parking lot collision"

Retrieved Clauses:
1. Collision Coverage (0.95) ← STRONG: Directly addresses collision
2. Comprehensive Coverage (0.88) ← MODERATE: Related to vehicle damage
3. Liability Exclusions (0.85) ← MODERATE: Relevant for coverage limits
4. Towing Benefits (0.80) ← MODERATE: Related to post-accident services
5. Glass Coverage (0.78) ← WEAK: Not directly related to collision

The LLM uses scores to prioritize which clauses to emphasize in the decision.
```

---

##### **STEP 5.3: Guardrail Check**

**Code:**
```csharp
// Step 3: Guardrail - if no clauses found, manual review required
if (!clauses.Any())
{
    var manualReviewDecision = new ClaimDecision(
        Status: "Manual Review",
        Explanation: "No relevant policy clauses found for this claim type",
        ClauseReferences: new List<string>(),
        RequiredDocuments: new List<string> { "Policy Document", "Claim Evidence" },
        ConfidenceScore: 0.0f
    );
    
    await _auditService.SaveAsync(request, manualReviewDecision, clauses);
    return manualReviewDecision;
}
```

**Purpose:** Safety check to prevent hallucinations - if no relevant policy found, require human review

---

##### **STEP 5.4: Generate Decision using LLM (Amazon Bedrock Claude)**

**Service:** `LlmService` (`src/ClaimsRagBot.Infrastructure/Bedrock/LlmService.cs`)

**Code:**
```csharp
// Step 4: Generate decision using LLM
var decision = await _llmService.GenerateDecisionAsync(request, clauses);
```

**AWS Service:** Amazon Bedrock - Claude 3.5 Sonnet v2
- **Model:** `us.anthropic.claude-3-5-sonnet-20241022-v2:0`

**Prompt Construction:**
```csharp
private string BuildPrompt(ClaimRequest request, List<PolicyClause> clauses)
{
    var clausesText = string.Join("\n\n", clauses.Select(c => 
        $"[{c.ClauseId}] {c.CoverageType}: {c.Text}"));

    return $@"Claim:
Policy Number: {request.PolicyNumber}
Claim Amount: ${request.ClaimAmount}
Description: {request.ClaimDescription}

Policy Clauses:
{clausesText}

Respond in JSON:
{{
  ""status"": ""Covered"" | ""Not Covered"" | ""Manual Review"",
  ""explanation"": ""<explanation>"",
  ""clauseReferences"": [""<clause_id>""],
  ""requiredDocuments"": [""<document>""],
  ""confidenceScore"": 0.0-1.0
}}";
}
```

**System Prompt:**
```
You are an insurance claims validation assistant.
You MUST:
- Use ONLY the provided policy clauses
- Cite clause IDs
- If unsure, say 'Needs Manual Review'
- Respond in valid JSON format only
```

**Bedrock API Call:**
```csharp
var requestBody = new {
    anthropic_version = "bedrock-2023-05-31",
    max_tokens = 1024,
    messages = new[] {
        new { role = "user", content = prompt }
    },
    system = systemPrompt
};

var invokeRequest = new InvokeModelRequest {
    ModelId = "us.anthropic.claude-3-5-sonnet-20241022-v2:0",
    Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestBody))),
    ContentType = "application/json"
};

var response = await _client.InvokeModelAsync(invokeRequest);
```

**LLM Response (Example):**
```json
{
  "status": "Covered",
  "explanation": "The claim for vehicle collision damage falls under Collision Coverage (MOTOR-001), which covers up to $50,000 per incident. The claim amount of $5,000 is within the coverage limit.",
  "clauseReferences": ["MOTOR-001"],
  "requiredDocuments": ["Police Report", "Damage Photos", "Repair Estimate"],
  "confidenceScore": 0.92
}
```

---

##### **STEP 5.5: Apply Business Rules (Aflac-style Validation)**

**Code:**
```csharp
// Step 5: Apply Aflac-style business rules
decision = ApplyBusinessRules(decision, request);

private ClaimDecision ApplyBusinessRules(ClaimDecision decision, ClaimRequest request)
{
    const decimal autoApprovalThreshold = 5000m;
    const float confidenceThreshold = 0.85f;

    // Rule 1: Low confidence → Manual Review
    if (decision.ConfidenceScore < confidenceThreshold)
    {
        return decision with {
            Status = "Manual Review",
            Explanation = $"Confidence below threshold ({decision.ConfidenceScore:F2} < {confidenceThreshold}). " + decision.Explanation
        };
    }

    // Rule 2: High amount + covered → Manual Review
    if (request.ClaimAmount > autoApprovalThreshold && decision.Status == "Covered")
    {
        return decision with {
            Status = "Manual Review",
            Explanation = $"Amount ${request.ClaimAmount} exceeds auto-approval limit. " + decision.Explanation
        };
    }

    // Rule 3: Exclusion clause detected → Deny or Manual Review
    if (decision.ClauseReferences.Any(c => c.Contains("Exclusion", StringComparison.OrdinalIgnoreCase)))
    {
        return decision with {
            Status = decision.Status == "Covered" ? "Manual Review" : decision.Status,
            Explanation = "Potential exclusion clause detected. " + decision.Explanation
        };
    }

    return decision;
}
```

**Business Rules:**
1. **Confidence Threshold:** < 85% → Manual Review
2. **Auto-Approval Limit:** > $5,000 → Manual Review (even if covered)
3. **Exclusion Detection:** Exclusion clauses → Manual Review/Deny

**Purpose:** Risk mitigation and compliance

---

##### **STEP 5.6: Save Audit Trail (Amazon DynamoDB)**

**Service:** `AuditService` (`src/ClaimsRagBot.Infrastructure/DynamoDB/AuditService.cs`)

**Code:**
```csharp
// Step 6: Audit trail (mandatory for compliance)
await _auditService.SaveAsync(request, decision, clauses);

return decision;
```

**DynamoDB Table:** `ClaimsAuditTrail`

**Record Structure:**
```json
{
  "ClaimId": "uuid-generated",
  "Timestamp": "2026-01-30T10:30:00Z",
  "PolicyNumber": "POL-2024-001",
  "ClaimAmount": 5000,
  "ClaimDescription": "Vehicle collision...",
  "DecisionStatus": "Covered",
  "Explanation": "The claim falls under...",
  "ConfidenceScore": 0.92,
  "ClauseReferences": ["MOTOR-001"],
  "RequiredDocuments": ["Police Report", "Damage Photos"],
  "RetrievedClauses": "[{\"ClauseId\":\"MOTOR-001\",\"Score\":0.95}...]"
}
```

**DynamoDB API Call:**
```csharp
var putRequest = new PutItemRequest {
    TableName = "ClaimsAuditTrail",
    Item = auditRecord
};
await _client.PutItemAsync(putRequest);
```

**Purpose:** 
- Regulatory compliance
- Audit trail for all decisions
- Debug/troubleshooting
- Analytics

---

#### **STEP 6: Response Returns to UI**

**Response Flow:**
```
DynamoDB → Orchestrator → Controller → Angular HttpClient → ChatComponent
```

**Response Object:**
```typescript
interface ClaimDecision {
  status: string;              // "Covered" | "Not Covered" | "Manual Review"
  explanation: string;         // Detailed reasoning
  clauseReferences: string[];  // ["MOTOR-001"]
  requiredDocuments: string[]; // ["Police Report", "Damage Photos"]
  confidenceScore: number;     // 0.92
}
```

**ChatComponent Displays Result:**
```typescript
this.chatService.addBotMessage(
  `Claim Validation Result:\n\n` +
  `Decision: ${result.isApproved ? '✅ APPROVED' : '❌ DENIED'}\n` +
  `Confidence: ${(result.confidenceScore * 100).toFixed(1)}%\n` +
  `Requires Review: ${result.requiresHumanReview ? 'Yes' : 'No'}\n\n` +
  `Reasoning:\n${result.reasoning}`,
  'result',
  result
);
```

---

## Execution Flow - Document Upload

### Flow Overview
```
User → DocumentUploadComponent → ClaimsApiService → DocumentsController
→ S3 Upload → Textract → Comprehend → Rekognition → Bedrock → Validation → Response
```

### Detailed Step-by-Step Execution

#### **STEP 1: User Uploads Document**

**Component:** `DocumentUploadComponent` (`claims-chatbot-ui/src/app/components/document-upload/document-upload.component.ts`)

**User Actions:**
1. User drags PDF/JPG/PNG file onto upload zone OR clicks "Choose File"
2. User selects document type (Claim Form/Medical Bills/Police Report/Damage Photos)
3. User optionally enters User ID
4. User clicks "Upload & Extract"

**File Validation:**
```typescript
private handleFile(file: File): void {
  // Validate file type
  const validTypes = ['application/pdf', 'image/jpeg', 'image/png'];
  if (!validTypes.includes(file.type)) {
    this.chatService.addBotMessage('❌ Invalid file type...');
    return;
  }

  // Validate file size (10MB max)
  const maxSize = 10 * 1024 * 1024;
  if (file.size > maxSize) {
    this.chatService.addBotMessage('❌ File size exceeds 10MB limit.');
    return;
  }

  this.selectedFile = file;
}
```

**Code Execution:**
```typescript
uploadDocument(): void {
  this.isUploading = true;
  
  this.apiService.submitDocument(
    this.selectedFile,
    this.userId || undefined,
    this.documentType
  ).subscribe({
    next: (response) => {
      this.documentSubmitted.emit(response);
      this.reset();
    },
    error: (error) => { /* Handle error */ }
  });
}
```

---

#### **STEP 2: HTTP Request - Submit Document**

**Service:** `ClaimsApiService`

**Code:**
```typescript
submitDocument(
  file: File, 
  userId?: string, 
  documentType: DocumentType = DocumentType.ClaimForm
): Observable<SubmitDocumentResponse> {
  const formData = new FormData();
  formData.append('file', file);
  if (userId) formData.append('userId', userId);
  formData.append('documentType', documentType);
  
  return this.http.post<SubmitDocumentResponse>(
    `${this.baseUrl}/documents/submit`,  // POST /api/documents/submit
    formData
  );
}
```

**HTTP Request:**
- **Method:** POST
- **URL:** `http://localhost:5000/api/documents/submit`
- **Content-Type:** `multipart/form-data`
- **Body:** FormData with file binary + metadata

---

#### **STEP 3: .NET API Receives Upload**

**Controller:** `DocumentsController` (`src/ClaimsRagBot.Api/Controllers/DocumentsController.cs`)

**Code:**
```csharp
[HttpPost("submit")]
[RequestSizeLimit(10_485_760)] // 10MB
public async Task<ActionResult<SubmitDocumentResponse>> SubmitDocument(
    IFormFile file, 
    [FromForm] string? userId = null,
    [FromForm] string documentType = "ClaimForm")
{
    // Step 1: Upload to S3
    var uploadResult = await UploadDocument(file, userId);
    
    // Step 2: Extract claim data
    var extractionResult = await _extractionService.ExtractClaimDataAsync(
        uploadData.DocumentId, 
        docType
    );
    
    return Ok(new SubmitDocumentResponse(
        UploadResult: uploadData,
        ExtractionResult: extractionResult,
        ValidationStatus: DetermineValidationStatus(extractionResult),
        NextAction: DetermineNextAction(extractionResult)
    ));
}
```

**Sub-Step 3.1: Upload to S3**

**Service:** `DocumentUploadService` (`src/ClaimsRagBot.Infrastructure/S3/DocumentUploadService.cs`)

**Code:**
```csharp
public async Task<DocumentUploadResult> UploadAsync(
    Stream fileStream, 
    string fileName, 
    string contentType, 
    string userId)
{
    var documentId = Guid.NewGuid().ToString();
    var s3Key = $"{_uploadPrefix}{userId}/{documentId}/{fileName}";
    // Example: uploads/john_doe/abc-123-def/claim_form.pdf
    
    var putRequest = new PutObjectRequest {
        BucketName = _bucketName,  // e.g., "claims-documents-bucket"
        Key = s3Key,
        InputStream = fileStream,
        ContentType = contentType,
        ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
        Metadata = {
            ["document-id"] = documentId,
            ["user-id"] = userId,
            ["upload-timestamp"] = DateTime.UtcNow.ToString("O")
        }
    };
    
    var response = await _s3Client.PutObjectAsync(putRequest);
    
    return new DocumentUploadResult(
        DocumentId: documentId,
        S3Bucket: _bucketName,
        S3Key: s3Key,
        ContentType: contentType,
        FileSize: fileStream.Length,
        UploadedAt: DateTime.UtcNow
    );
}
```

**AWS Service:** Amazon S3
- **Bucket:** Configured in `appsettings.json` → `AWS:S3:DocumentBucket`
- **Encryption:** AES256 server-side encryption
- **Metadata:** Document ID, User ID, Timestamp

---

#### **STEP 4: Document Extraction Orchestration**

**Service:** `DocumentExtractionOrchestrator` (`src/ClaimsRagBot.Infrastructure/DocumentExtraction/DocumentExtractionOrchestrator.cs`)

This is a **multi-stage AI pipeline** that extracts structured claim data from unstructured documents. It orchestrates four AWS AI services working in sequence to convert uploaded documents (PDFs, images, forms) into validated claim data.

---

##### **Overview: The Multi-Stage Extraction Pipeline**

**Purpose:** Automatically convert uploaded claim documents into structured `ClaimRequest` objects without manual data entry.

**The 5-Stage Pipeline:**

```
Stage 1: Text Extraction (Textract)
    ↓ (Raw text + form fields)
Stage 2: Entity Recognition (Comprehend)
    ↓ (Named entities + claim fields)
Stage 3: Image Analysis (Rekognition) [Optional]
    ↓ (Damage assessment + labels)
Stage 4: AI Synthesis (Bedrock Claude)
    ↓ (Structured claim object)
Stage 5: Validation & Confidence Scoring
    ↓ (Final ClaimExtractionResult)
```

**Why Multiple Stages?**

Each AWS service excels at a specific task:
- **Textract**: Best at OCR and form understanding
- **Comprehend**: Best at finding names, dates, amounts
- **Rekognition**: Best at understanding image content
- **Bedrock Claude**: Best at reasoning and synthesis

Combining them produces better results than any single service alone.

---

##### **Entry Point: Orchestrator Receives Upload**

**Method Signature:**
```csharp
public async Task<ClaimExtractionResult> ExtractClaimDataAsync(
    DocumentUploadResult uploadResult,  // From previous STEP 3
    DocumentType documentType           // ClaimForm, PoliceReport, etc.
)
```

**What Happens:**
```csharp
Console.WriteLine($"[Orchestrator] Starting extraction for document: {uploadResult.DocumentId}");
Console.WriteLine($"[Orchestrator] S3 key: {uploadResult.S3Key}");
Console.WriteLine($"[Orchestrator] Document type: {documentType}");

// Extract S3 location
var s3Key = uploadResult.S3Key;  
// Example: "uploads/user-123/doc-456/claim_form.pdf"

var documentId = uploadResult.DocumentId;  
// Example: "a7b3c2d1-e4f5-6789-0abc-def123456789"
```

The orchestrator now knows:
- Where the document is stored in S3
- What type of document it is (determines processing strategy)
- The unique document ID for tracking

---

##### **STEP 4.1: Text Extraction (Amazon Textract)**

**Service:** `TextractService` (`src/ClaimsRagBot.Infrastructure/Textract/TextractService.cs`)

**Purpose:** Extract text and structured data (forms, tables) from the uploaded document.

---

###### **Decision: Which Textract API to Use?**

The orchestrator chooses the extraction method based on document type:

```csharp
Console.WriteLine("[Orchestrator] Step 1: Extracting text with Textract");

TextractResult textractResult;

if (documentType == DocumentType.ClaimForm || 
    documentType == DocumentType.PoliceReport)
{
    // STRUCTURED DOCUMENTS: Use advanced form/table analysis
    textractResult = await _textractService.AnalyzeDocumentAsync(
        _s3Bucket,           // "claims-documents-bucket"
        s3Key,               // "uploads/user-123/doc-456/claim_form.pdf"
        new[] { "FORMS", "TABLES" }  // Feature types to extract
    );
}
else
{
    // SIMPLE DOCUMENTS: Use basic text detection
    textractResult = await _textractService.DetectDocumentTextAsync(
        _s3Bucket, 
        s3Key
    );
}
```

**Decision Logic:**

| Document Type | API Used | Reason |
|--------------|----------|--------|
| **ClaimForm** | `AnalyzeDocument` (FORMS + TABLES) | Extracts field labels & values (e.g., "Policy Number: POL-123") |
| **PoliceReport** | `AnalyzeDocument` (FORMS + TABLES) | Extracts structured report data (incident details, officer info) |
| **DamagePhotos** | `DetectDocumentText` | Just extracts any visible text in images |
| **MedicalRecords** | `DetectDocumentText` | Simple text extraction from medical documents |

---

###### **What is AnalyzeDocument?**

**AWS API:** `AmazonTextractClient.AnalyzeDocumentAsync()`

**Capabilities:**
- **FORMS Analysis**: Understands form structure (labels paired with values)
- **TABLES Analysis**: Extracts table cells with row/column relationships
- **OCR**: Optical Character Recognition for printed/handwritten text
- **Confidence Scores**: Each extracted item has a confidence percentage

**Example Request to AWS:**
```json
{
  "Document": {
    "S3Object": {
      "Bucket": "claims-documents-bucket",
      "Name": "uploads/user-123/doc-456/claim_form.pdf"
    }
  },
  "FeatureTypes": ["FORMS", "TABLES"]
}
```

---

###### **Textract Output Structure**

**Real-World Example:**

**Input Document (claim_form.pdf):**
```
═══════════════════════════════════════
    INSURANCE CLAIM FORM
═══════════════════════════════════════

Policy Number: POL-2024-001
Policy Type:   Motor Insurance
Claim Amount:  $5,000
Date of Loss:  January 15, 2026

Description of Incident:
Vehicle was involved in a collision with
another vehicle on Highway 101. Front-end
damage to bumper and headlights.

Claimant Name: John Doe
Phone: (555) 123-4567
═══════════════════════════════════════
```

**Textract Output (`TextractResult` object):**

```csharp
TextractResult {
    // 1. ALL TEXT (sequential reading order)
    ExtractedText: @"
        INSURANCE CLAIM FORM
        Policy Number: POL-2024-001
        Policy Type: Motor Insurance
        Claim Amount: $5,000
        Date of Loss: January 15, 2026
        Description of Incident:
        Vehicle was involved in a collision with another vehicle 
        on Highway 101. Front-end damage to bumper and headlights.
        Claimant Name: John Doe
        Phone: (555) 123-4567
    ",
    
    // 2. KEY-VALUE PAIRS (structured form fields)
    KeyValuePairs: {
        ["Policy Number"] = "POL-2024-001",
        ["Policy Type"] = "Motor Insurance",
        ["Claim Amount"] = "$5,000",
        ["Date of Loss"] = "January 15, 2026",
        ["Claimant Name"] = "John Doe",
        ["Phone"] = "(555) 123-4567"
    },
    
    // 3. TABLES (if document contains tables)
    Tables: [
        // Empty in this example - no tables in document
    ],
    
    // 4. OVERALL CONFIDENCE
    Confidence: 97.3  // 97.3% confident in extraction accuracy
}
```

**How Textract Understands Forms:**

Textract uses machine learning to detect visual relationships:

```
[Label]         [Spacing/Colon]         [Value]
"Policy Number"      :              "POL-2024-001"
       ↓                                   ↓
  (Recognized as                    (Recognized as
   form field label)                 field value)
```

This is stored as: `KeyValuePairs["Policy Number"] = "POL-2024-001"`

---

###### **Confidence Scores Explained**

Each extracted piece has a confidence score:

```csharp
// Textract internally tracks confidence per field:
"Policy Number: POL-2024-001" → 99.5% confidence (clear, typed text)
"Claimant Name: John Doe" → 97.8% confidence (clear, typed text)
"Handwritten note: ..." → 82.3% confidence (harder to read)
```

The overall confidence is calculated as:
```
Overall Confidence = Average of all field confidences
```

**Why This Matters:**
- High confidence (>95%): Data is likely accurate
- Medium confidence (80-95%): May need human review
- Low confidence (<80%): Likely requires manual verification

---

##### **STEP 4.2: Entity Recognition (Amazon Comprehend)**

**Service:** `ComprehendService` (`src/ClaimsRagBot.Infrastructure/Comprehend/ComprehendService.cs`)

**Purpose:** Extract named entities and domain-specific claim fields from the text that Textract provided.

---

###### **Why Comprehend After Textract?**

**Textract provides:**
- Raw text and form fields

**Comprehend adds:**
- **Semantic understanding**: Recognizes people, places, dates, amounts
- **Domain intelligence**: Applies insurance claim knowledge
- **Contextual extraction**: Understands what data means in context

**Example:**
```
Textract extracts: "The accident occurred on January 15, 2026 near 123 Main St."

Comprehend identifies:
- DATE: "January 15, 2026"
- LOCATION: "123 Main St"
- EVENT: "accident"
```

---

###### **The Two Comprehend Operations**

```csharp
Console.WriteLine("[Orchestrator] Step 2: Extracting entities with Comprehend");

// Operation 1: Generic Named Entity Recognition
var entities = await _comprehendService.DetectEntitiesAsync(
    textractResult.ExtractedText
);

// Operation 2: Custom Claim Field Extraction
var claimFields = await _comprehendService.ExtractClaimFieldsAsync(
    textractResult.ExtractedText
);
```

---

###### **Operation 1: DetectEntitiesAsync (Generic NER)**

**AWS API:** `AmazonComprehendClient.DetectEntitiesAsync()`

**What It Does:** Identifies and categorizes named entities in the text.

**Entity Types Detected:**

| Entity Type | Description | Example from Text |
|------------|-------------|-------------------|
| **PERSON** | Names of people | "John Doe", "Officer Smith" |
| **LOCATION** | Addresses, cities, landmarks | "123 Main St", "Highway 101", "Los Angeles" |
| **DATE** | Dates and times | "January 15, 2026", "3:30 PM", "last Tuesday" |
| **QUANTITY** | Numbers with units | "$5,000", "50 mph", "2 vehicles" |
| **ORGANIZATION** | Company/institution names | "ABC Insurance Co.", "Memorial Hospital" |
| **EVENT** | Event names | "collision", "accident", "incident" |
| **COMMERCIAL_ITEM** | Products/goods | "2019 Honda Civic", "iPhone 13" |

**Real Example:**

**Input Text (from Textract):**
```
"John Doe filed a claim on January 15, 2026 for a collision 
that occurred on Highway 101 near Los Angeles. The repair 
estimate from AutoShop Inc. is $5,000."
```

**Comprehend Output:**
```csharp
List<ComprehendEntity> entities = [
    {
        Type: "PERSON",
        Text: "John Doe",
        Score: 0.99,          // 99% confidence
        BeginOffset: 0,       // Character position in text
        EndOffset: 8
    },
    {
        Type: "DATE",
        Text: "January 15, 2026",
        Score: 0.98,
        BeginOffset: 28,
        EndOffset: 45
    },
    {
        Type: "LOCATION",
        Text: "Highway 101",
        Score: 0.96,
        BeginOffset: 76,
        EndOffset: 87
    },
    {
        Type: "LOCATION",
        Text: "Los Angeles",
        Score: 0.97,
        BeginOffset: 93,
        EndOffset: 104
    },
    {
        Type: "ORGANIZATION",
        Text: "AutoShop Inc.",
        Score: 0.95,
        BeginOffset: 130,
        EndOffset: 143
    },
    {
        Type: "QUANTITY",
        Text: "$5,000",
        Score: 0.94,
        BeginOffset: 147,
        EndOffset: 153
    }
];
```

**Why Entity Scores Matter:**
- **>0.95**: Very confident (typed/printed text, clear context)
- **0.80-0.95**: Confident (may have minor ambiguity)
- **<0.80**: Less confident (handwritten, unclear, or ambiguous)

---

###### **Operation 2: ExtractClaimFieldsAsync (Custom Logic)**

**What It Does:** Uses domain-specific patterns to extract insurance claim fields.

**Implementation Strategy:**

The service applies custom regex patterns and keyword matching to find claim-specific data:

```csharp
public async Task<Dictionary<string, string>> ExtractClaimFieldsAsync(string text)
{
    var fields = new Dictionary<string, string>();
    
    // 1. Extract Policy Number (pattern matching)
    var policyMatch = Regex.Match(text, @"POL-\d{4,}-\d+");
    if (policyMatch.Success)
        fields["policyNumber"] = policyMatch.Value;
    
    // 2. Extract Claim Amount (find currency values)
    var amountMatch = Regex.Match(text, @"\$[\d,]+");
    if (amountMatch.Success)
        fields["claimAmount"] = amountMatch.Value.Replace("$", "").Replace(",", "");
    
    // 3. Extract Policy Type (keyword detection)
    if (text.Contains("Motor", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("Vehicle", StringComparison.OrdinalIgnoreCase))
        fields["policyType"] = "Motor";
    else if (text.Contains("Health", StringComparison.OrdinalIgnoreCase))
        fields["policyType"] = "Health";
    
    // 4. Extract Date of Loss (combine with Comprehend DATE entities)
    var dateEntity = entities.FirstOrDefault(e => e.Type == "DATE");
    if (dateEntity != null)
        fields["dateOfLoss"] = dateEntity.Text;
    
    // 5. Extract Location
    var locationEntity = entities.FirstOrDefault(e => e.Type == "LOCATION");
    if (locationEntity != null)
        fields["location"] = locationEntity.Text;
    
    return fields;
}
```

**Example Output:**

**Input:** Same claim form text from Textract

**Output:**
```csharp
Dictionary<string, string> claimFields = {
    ["policyNumber"] = "POL-2024-001",
    ["claimAmount"] = "5000",              // Normalized (removed $ and ,)
    ["policyType"] = "Motor",              // Detected from keywords
    ["dateOfLoss"] = "January 15, 2026",   // From Comprehend DATE entity
    ["location"] = "Highway 101"           // From Comprehend LOCATION entity
};
```

---

###### **Combined Comprehend Results**

After both operations, the orchestrator has:

```csharp
// Generic entities (people, dates, amounts)
entities = [
    { Type: "PERSON", Text: "John Doe", Score: 0.99 },
    { Type: "DATE", Text: "January 15, 2026", Score: 0.98 },
    { Type: "QUANTITY", Text: "$5,000", Score: 0.94 }
];

// Structured claim fields (ready for validation)
claimFields = {
    "policyNumber": "POL-2024-001",
    "claimAmount": "5000",
    "policyType": "Motor",
    "dateOfLoss": "2026-01-15",
    "location": "Highway 101"
};
```

**Why Both?**
- **Entities**: Provide rich context (who, what, when, where)
- **Claim Fields**: Provide structured data ready for validation

Claude (in STEP 4.4) will use both to create the final claim object.

---

##### **STEP 4.3: Image Analysis (Amazon Rekognition) - Optional**

**Service:** `RekognitionService` (`src/ClaimsRagBot.Infrastructure/Rekognition/RekognitionService.cs`)

**Purpose:** Analyze damage photos to validate and enrich claim information.

**When This Runs:** Only when processing `DocumentType.DamagePhotos`

---

###### **Conditional Execution**

```csharp
Console.WriteLine("[Orchestrator] Step 3: Analyzing images with Rekognition");

List<ImageAnalysisResult>? imageAnalysis = null;

if (documentType == DocumentType.DamagePhotos && _rekognitionService != null)
{
    // Only analyze if:
    // 1. Document is tagged as a damage photo
    // 2. Rekognition service is configured
    
    imageAnalysis = await _rekognitionService.AnalyzeImagesAsync(
        _s3Bucket,               // "claims-documents-bucket"
        new List<string> { s3Key }  // ["uploads/.../car_damage.jpg"]
    );
}
else
{
    // Skip image analysis for text documents
    Console.WriteLine("[Orchestrator] Skipping image analysis (not a damage photo)");
}
```

---

###### **AWS Service: Amazon Rekognition**

**API Used:** `AmazonRekognitionClient.DetectLabelsAsync()`

**What It Does:** Computer vision analysis that identifies:
- **Objects**: Car, Bumper, Tire, Window
- **Damage Indicators**: Dent, Scratch, Broken, Cracked
- **Context**: Accident scene, Street, Parking lot
- **Confidence Scores**: How sure Rekognition is about each detection

**Example Request to AWS:**
```json
{
  "Image": {
    "S3Object": {
      "Bucket": "claims-documents-bucket",
      "Name": "uploads/user-123/doc-456/car_damage.jpg"
    }
  },
  "MaxLabels": 20,
  "MinConfidence": 70.0
}
```

---

###### **Real-World Example**

**Input Image:** `car_damage.jpg` (photo of damaged vehicle)

**Visual Content:**
```
┌─────────────────────────────────────┐
│                                     │
│    [Damaged Car Front Bumper]      │
│                                     │
│  - Dented front bumper              │
│  - Cracked headlight                │
│  - Scratch marks on hood            │
│  - Visible in parking lot           │
│                                     │
└─────────────────────────────────────┘
```

**Rekognition Output:**

```csharp
ImageAnalysisResult {
    ImageId: "doc-456",
    
    Labels: [
        // VEHICLE DETECTION
        {
            Name: "Car",
            Confidence: 99.2,
            Instances: [
                { BoundingBox: { Left: 0.15, Top: 0.20, Width: 0.70, Height: 0.60 } }
            ]
        },
        {
            Name: "Vehicle",
            Confidence: 98.7
        },
        {
            Name: "Automobile",
            Confidence: 98.5
        },
        
        // DAMAGE INDICATORS
        {
            Name: "Damaged",
            Confidence: 92.3
        },
        {
            Name: "Dent",
            Confidence: 88.5
        },
        {
            Name: "Broken",
            Confidence: 85.1
        },
        
        // VEHICLE PARTS
        {
            Name: "Bumper",
            Confidence: 87.9
        },
        {
            Name: "Headlight",
            Confidence: 84.3
        },
        
        // CONTEXT/SCENE
        {
            Name: "Parking Lot",
            Confidence: 91.2
        },
        {
            Name: "Outdoors",
            Confidence: 96.5
        },
        {
            Name: "Asphalt",
            Confidence: 89.7
        }
    ],
    
    // DERIVED ANALYSIS
    DamageType: "Collision",      // Inferred from labels
    Confidence: 0.92              // Overall analysis confidence
}
```

---

###### **How Damage Type is Determined**

The `RekognitionService` applies business logic to classify damage:

```csharp
public string InferDamageType(List<Label> labels)
{
    var labelNames = labels.Select(l => l.Name.ToLower()).ToList();
    
    // Collision damage indicators
    if (labelNames.Any(n => n.Contains("dent") || n.Contains("crash") || n.Contains("collision")))
        return "Collision";
    
    // Fire damage indicators
    if (labelNames.Any(n => n.Contains("fire") || n.Contains("burn") || n.Contains("smoke")))
        return "Fire";
    
    // Theft/vandalism indicators
    if (labelNames.Any(n => n.Contains("broken glass") || n.Contains("shatter") || n.Contains("vandal")))
        return "Vandalism";
    
    // Weather damage indicators
    if (labelNames.Any(n => n.Contains("hail") || n.Contains("flood") || n.Contains("tree")))
        return "Weather";
    
    // Default if no specific pattern matches
    return "General Damage";
}
```

---

###### **Why Image Analysis Matters**

**Use Cases:**

1. **Damage Validation**: Confirms claim description matches photos
   ```
   Claim: "Front bumper damaged in collision"
   Image Labels: Car, Bumper, Dent, Damaged ✅ MATCHES
   ```

2. **Fraud Detection**: Flags suspicious claims
   ```
   Claim: "Vehicle totaled in severe accident"
   Image Labels: Car, Clean, Undamaged, Shiny ❌ MISMATCH
   ```

3. **Claim Enrichment**: Adds details not mentioned in description
   ```
   Claim Description: "Car damage"
   Image Analysis: "Front bumper dent, headlight crack, parking lot collision"
   ```

4. **Automated Severity Assessment**:
   ```
   Labels with high confidence:
   - "Severe Damage": 94% → High severity
   - "Minor Scratch": 88% → Low severity
   ```

---

###### **When Image Analysis is Skipped**

```csharp
// Skipped for these document types:
if (documentType == DocumentType.ClaimForm ||
    documentType == DocumentType.PoliceReport ||
    documentType == DocumentType.MedicalRecords)
{
    // These are text documents, no useful image analysis
    imageAnalysis = null;
}
```

**Result:** `imageAnalysis` remains `null`, and STEP 4.4 proceeds without image data.

---

##### **STEP 4.4: AI-Powered Data Synthesis (Amazon Bedrock Claude)**

**Purpose:** Intelligently combine data from all previous steps into a single, validated `ClaimRequest` object.

**The Challenge:** We now have data from 3 different sources that may conflict or have gaps:
- Textract: Raw text + form fields
- Comprehend: Entities + claim fields
- Rekognition: Image labels (if applicable)

Claude's job is to resolve conflicts, fill gaps, and create the best possible structured claim.

---

###### **Orchestrator Invokes Synthesis**

```csharp
Console.WriteLine("[Orchestrator] Step 4: Synthesizing data with Bedrock Claude");

var extractedClaim = await SynthesizeClaimDataAsync(
    textractResult,    // From STEP 4.1
    entities,          // From STEP 4.2 (operation 1)
    claimFields,       // From STEP 4.2 (operation 2)
    imageAnalysis,     // From STEP 4.3 (or null)
    documentType       // Original document type
);
```

---

###### **STEP 4.4.1: Build Comprehensive Prompt**

The method `BuildExtractionPrompt` creates a detailed prompt containing ALL extracted data:

```csharp
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
    
    // SECTION 1: Raw document text
    prompt.AppendLine("=== DOCUMENT TEXT ===");
    prompt.AppendLine(textractResult.ExtractedText.Length > 2000 
        ? textractResult.ExtractedText.Substring(0, 2000) + "..." 
        : textractResult.ExtractedText);
    prompt.AppendLine();
    
    // SECTION 2: Structured form fields (if any)
    if (textractResult.KeyValuePairs.Any())
    {
        prompt.AppendLine("=== FORM FIELDS ===");
        foreach (var kvp in textractResult.KeyValuePairs.Take(20))
        {
            prompt.AppendLine($"{kvp.Key}: {kvp.Value}");
        }
        prompt.AppendLine();
    }
    
    // SECTION 3: Pre-extracted claim fields
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
    
    // SECTION 4: Named entities
    if (entities.Any())
    {
        prompt.AppendLine("=== IDENTIFIED ENTITIES ===");
        foreach (var entity in entities.Take(15))
        {
            prompt.AppendLine($"{entity.Type}: {entity.Text} (confidence: {entity.Score:F2})");
        }
        prompt.AppendLine();
    }
    
    // SECTION 5: Image analysis (if present)
    if (imageAnalysis?.Any() == true)
    {
        prompt.AppendLine("=== DAMAGE PHOTO ANALYSIS ===");
        foreach (var img in imageAnalysis)
        {
            prompt.AppendLine($"Image {img.ImageId}:");
            var topLabels = string.Join(", ", img.Labels.Take(10).Select(l => l.Name));
            prompt.AppendLine($"  Detected objects: {topLabels}");
            prompt.AppendLine($"  Damage type: {img.DamageType} (confidence: {img.Confidence:F2})");
        }
        prompt.AppendLine();
    }
    
    return prompt.ToString();
}
```

---

###### **Example Prompt Sent to Claude**

**Complete Prompt:**

```
Extract claim information from the following data:

=== DOCUMENT TEXT ===
INSURANCE CLAIM FORM
Policy Number: POL-2024-001
Policy Type: Motor Insurance
Claim Amount: $5,000
Date of Loss: January 15, 2026

Description of Incident:
Vehicle was involved in a collision with another vehicle on 
Highway 101. Front-end damage to bumper and headlights.

Claimant Name: John Doe
Phone: (555) 123-4567

=== FORM FIELDS ===
Policy Number: POL-2024-001
Policy Type: Motor Insurance
Claim Amount: $5,000
Date of Loss: January 15, 2026
Claimant Name: John Doe
Phone: (555) 123-4567

=== EXTRACTED CLAIM FIELDS ===
policyNumber: POL-2024-001
claimAmount: 5000
policyType: Motor
dateOfLoss: 2026-01-15
location: Highway 101

=== IDENTIFIED ENTITIES ===
PERSON: John Doe (confidence: 0.99)
DATE: January 15, 2026 (confidence: 0.98)
LOCATION: Highway 101 (confidence: 0.96)
QUANTITY: $5,000 (confidence: 0.94)

=== DAMAGE PHOTO ANALYSIS ===
Image doc-456:
  Detected objects: Car, Damaged, Bumper, Headlight, Dent, Collision
  Damage type: Collision (confidence: 0.92)
```

---

###### **STEP 4.4.2: Claude Processes and Synthesizes**

**System Prompt to Claude:**

```csharp
var systemPrompt = @"You are an expert insurance claims data extraction system.

INSTRUCTIONS:
1. Extract and structure claim information from provided documents
2. Apply domain knowledge to resolve ambiguities
3. Ensure all monetary amounts are in USD without currency symbols
4. Normalize policy types to exactly one of: Motor, Home, Health, Life
5. Generate detailed claim descriptions from available information
6. If multiple data sources conflict, prefer structured fields over raw text
7. Output ONLY valid JSON, no markdown formatting

OUTPUT FORMAT:
{
  ""policyNumber"": ""POL-XXXX-XXX"",
  ""policyType"": ""Motor"",
  ""claimAmount"": 5000,
  ""claimDescription"": ""Detailed description of the incident including location, date, damage, etc.""
}";
```

**Claude's Reasoning Process:**

1. **Data Reconciliation:**
   ```
   Textract says: "Claim Amount: $5,000"
   Comprehend extracted: "claimAmount: 5000"
   Image labels include: "Damaged", "Collision"
   
   → All sources agree on $5,000 amount ✓
   ```

2. **Conflict Resolution:**
   ```
   Textract KeyValuePairs: "Policy Type: Motor Insurance"
   Comprehend claimFields: "policyType: Motor"
   
   → Normalize to: "Motor" (matches required format)
   ```

3. **Description Generation:**
   ```
   Combine:
   - Date: "January 15, 2026"
   - Location: "Highway 101"
   - Type: "collision"
   - Damage: "front-end damage to bumper and headlights"
   - Evidence: Image shows "Car, Damaged, Bumper, Dent"
   
   → Generate: "Vehicle collision on Highway 101 on January 15, 2026 
                resulting in front-end damage including dented bumper 
                and broken headlights. Damage verified by photo analysis."
   ```

---

###### **Claude's Output (JSON)**

```json
{
  "policyNumber": "POL-2024-001",
  "policyType": "Motor",
  "claimAmount": 5000,
  "claimDescription": "Vehicle collision on Highway 101 on January 15, 2026 resulting in front-end damage including dented bumper and broken headlights. Claimant John Doe reported collision with another vehicle. Damage verified by photo analysis showing collision-type damage to front bumper and headlight assembly."
}
```

---

###### **STEP 4.4.3: Parse Claude's Response**

```csharp
var response = await CallBedrockForExtractionAsync(prompt, systemPrompt);

// Parse JSON response into ClaimRequest object
return new ClaimRequest(
    PolicyNumber: response.PolicyNumber,      // "POL-2024-001"
    PolicyType: response.PolicyType,          // "Motor"
    ClaimAmount: response.ClaimAmount,        // 5000
    ClaimDescription: response.ClaimDescription  // Full description
);
```

---

###### **Why Use Claude for This?**

**Advantages Over Rule-Based Extraction:**

| Scenario | Rule-Based System | Claude (LLM) |
|----------|------------------|--------------|
| **Conflicting Data** | "Policy Type: Motor Insurance" vs "policyType: Motor" | ❌ Chooses first match | ✅ Understands both refer to "Motor" |
| **Missing Fields** | Policy number not in form fields | ❌ Returns empty/null | ✅ Searches document text for POL-XXXX pattern |
| **Ambiguous Dates** | "last Tuesday" | ❌ Cannot interpret | ✅ Calculates actual date |
| **Complex Descriptions** | Need to combine location + date + damage + entities | ❌ Template-based, rigid | ✅ Natural language generation |
| **Format Variations** | "$5,000.00" vs "5000 USD" vs "five thousand dollars" | ❌ Needs regex for each | ✅ Understands all formats |

**Example of Claude's Reasoning:**

**Scenario:** Missing policy number in form fields

```
Textract KeyValuePairs: { } (empty - no policy number detected)
Comprehend claimFields: { "policyNumber": null }
Textract ExtractedText: "...Reference Number: POL-2024-001..."

Claude's reasoning:
"The form fields don't have 'Policy Number', but the document text 
mentions 'Reference Number: POL-2024-001'. In insurance documents, 
'Reference Number' typically refers to the policy number. I'll extract 
POL-2024-001 as the policyNumber."

Result: policyNumber = "POL-2024-001" ✓
```

---

###### **Fallback Logic**

If Claude fails (API error, timeout, invalid JSON), the system falls back to simpler extraction:

```csharp
catch (Exception ex)
{
    Console.WriteLine($"[Orchestrator] Error in LLM synthesis: {ex.Message}");
    
    // Fallback: construct claim from extracted fields (no AI)
    return new ClaimRequest(
        PolicyNumber: claimFields.GetValueOrDefault("policyNumber", "UNKNOWN"),
        ClaimDescription: textractResult.ExtractedText.Length > 500 
            ? textractResult.ExtractedText.Substring(0, 500) 
            : textractResult.ExtractedText,
        ClaimAmount: decimal.TryParse(
            claimFields.GetValueOrDefault("claimAmount", "0"), 
            out var amt) ? amt : 0,
        PolicyType: claimFields.GetValueOrDefault("policyType", "Motor")
    );
}
```

This ensures the system always produces output, even if Claude is unavailable.

---

##### **STEP 4.5: Validation & Confidence Scoring**

**Purpose:** Assess the quality and completeness of the extracted claim data to determine if it's ready for automatic processing or needs human review.

---

###### **Orchestrator Invokes Validation**

```csharp
Console.WriteLine("[Orchestrator] Step 5: Validating extracted data");

var validationResult = ValidateExtractedData(
    extractedClaim,              // ClaimRequest from STEP 4.4
    textractResult.Confidence    // Base OCR confidence (0-100)
);

Console.WriteLine($"[Orchestrator] Extraction complete. Overall confidence: {validationResult.OverallConfidence:F2}");
```

---

###### **The Validation Method**

```csharp
private ClaimExtractionResult ValidateExtractedData(
    ClaimRequest extractedClaim, 
    float textractConfidence)
{
    var fieldConfidences = new Dictionary<string, float>();
    var ambiguousFields = new List<string>();
    
    // VALIDATION 1: Policy Number Format
    if (string.IsNullOrWhiteSpace(extractedClaim.PolicyNumber) || 
        extractedClaim.PolicyNumber == "UNKNOWN")
    {
        fieldConfidences["policyNumber"] = 0.3f;
        ambiguousFields.Add("policyNumber");
    }
    else if (Regex.IsMatch(extractedClaim.PolicyNumber, @"^POL-\d{4,}-\d+$"))
    {
        // Matches expected format: POL-YYYY-XXX
        fieldConfidences["policyNumber"] = 0.95f;
    }
    else
    {
        // Extracted but doesn't match standard format
        fieldConfidences["policyNumber"] = 0.7f;
    }
    
    // VALIDATION 2: Claim Amount Reasonableness
    if (extractedClaim.ClaimAmount <= 0)
    {
        fieldConfidences["claimAmount"] = 0.3f;
        ambiguousFields.Add("claimAmount");
    }
    else if (extractedClaim.ClaimAmount > 1_000_000)
    {
        // Suspiciously high amounts
        fieldConfidences["claimAmount"] = 0.6f;
    }
    else
    {
        fieldConfidences["claimAmount"] = 0.9f;
    }
    
    // VALIDATION 3: Policy Type Normalization
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
    
    // VALIDATION 4: Claim Description Quality
    if (string.IsNullOrWhiteSpace(extractedClaim.ClaimDescription) || 
        extractedClaim.ClaimDescription.Length < 20)
    {
        fieldConfidences["claimDescription"] = 0.4f;
        ambiguousFields.Add("claimDescription");
    }
    else
    {
        fieldConfidences["claimDescription"] = 0.85f;
    }
    
    // CALCULATE OVERALL CONFIDENCE (weighted average)
    var overallConfidence = (
        fieldConfidences.GetValueOrDefault("policyNumber", 0.5f) * 0.30f +  // 30% weight
        fieldConfidences.GetValueOrDefault("claimAmount", 0.5f) * 0.30f +    // 30% weight
        fieldConfidences.GetValueOrDefault("policyType", 0.5f) * 0.20f +     // 20% weight
        fieldConfidences.GetValueOrDefault("claimDescription", 0.5f) * 0.20f // 20% weight
    );
    
    // Factor in Textract base confidence
    overallConfidence = (overallConfidence + textractConfidence / 100f) / 2f;
    
    // Apply penalty for ambiguous fields
    if (ambiguousFields.Count > 0)
    {
        overallConfidence *= (1f - (ambiguousFields.Count * 0.1f));
    }
    
    // Clamp to 0.0 - 1.0 range
    overallConfidence = Math.Max(0, Math.Min(1, overallConfidence));
    
    return new ClaimExtractionResult(
        ExtractedClaim: extractedClaim,
        OverallConfidence: overallConfidence,
        FieldConfidences: fieldConfidences,
        AmbiguousFields: ambiguousFields,
        RawExtractedData: new Dictionary<string, object>
        {
            ["textractConfidence"] = textractConfidence
        }
    );
}
```

---

###### **Validation Rules Explained**

**Rule 1: Policy Number Format**

```csharp
Pattern: ^POL-\d{4,}-\d+$

Examples:
✅ "POL-2024-001"     → 95% confidence (perfect match)
✅ "POL-2024-12345"   → 95% confidence (perfect match)
⚠️ "POLICY-2024-001"  → 70% confidence (doesn't match pattern)
⚠️ "P24-001"          → 70% confidence (non-standard)
❌ "UNKNOWN"          → 30% confidence (missing)
❌ ""                 → 30% confidence (empty)
```

**Rule 2: Claim Amount Reasonableness**

```csharp
Validation Logic:

If amount <= 0:
    → 30% confidence (invalid amount)
    → Add to ambiguousFields

If amount > $1,000,000:
    → 60% confidence (suspiciously high - may be OCR error)
    → Example: "$5000" misread as "$500,000,000"

If $1 - $1,000,000:
    → 90% confidence (reasonable range)
```

**Rule 3: Policy Type Validation**

```csharp
Valid Types: ["Motor", "Home", "Health", "Life"]

Examples:
✅ "Motor"              → 95% confidence
✅ "Health"             → 95% confidence
⚠️ "Auto"               → 50% confidence (should be "Motor")
⚠️ "Car Insurance"      → 50% confidence (should be "Motor")
❌ null                 → 50% confidence (missing)
```

**Rule 4: Description Quality**

```csharp
Validation:

If description is null or empty:
    → 40% confidence

If description.Length < 20 characters:
    → 40% confidence (too vague, e.g., "Car damage")

If description.Length >= 20 characters:
    → 85% confidence (adequate detail)

Example:
❌ "Accident"                          → 40% (too short)
⚠️ "Car was damaged"                   → 40% (< 20 chars)
✅ "Vehicle collision on Highway 101..." → 85% (detailed)
```

---

###### **Confidence Score Calculation**

**Step 1: Weighted Field Scores**

```
overallConfidence = 
    (policyNumber confidence × 30%) +
    (claimAmount confidence × 30%) +
    (policyType confidence × 20%) +
    (claimDescription confidence × 20%)
```

**Example Calculation:**

```csharp
Field Confidences:
- policyNumber: 0.95
- claimAmount: 0.90
- policyType: 0.95
- claimDescription: 0.85

Weighted Sum:
= (0.95 × 0.30) + (0.90 × 0.30) + (0.95 × 0.20) + (0.85 × 0.20)
= 0.285 + 0.270 + 0.190 + 0.170
= 0.915  (91.5%)
```

**Step 2: Factor in Textract OCR Confidence**

```csharp
textractConfidence = 97.0  // From STEP 4.1

overallConfidence = (fieldConfidence + textractConfidence/100) / 2
                  = (0.915 + 0.97) / 2
                  = 0.9425  (94.25%)
```

**Step 3: Apply Penalties for Ambiguous Fields**

```csharp
If ambiguousFields.Count > 0:
    penalty = ambiguousFields.Count × 10%
    overallConfidence *= (1 - penalty)

Example:
If 1 ambiguous field (e.g., policyType):
    0.9425 × (1 - 0.10) = 0.8483  (84.83%)

If 2 ambiguous fields:
    0.9425 × (1 - 0.20) = 0.7540  (75.40%)
```

---

###### **Example Validation Results**

**Scenario 1: High-Quality Extraction**

```csharp
ExtractedClaim:
  PolicyNumber: "POL-2024-001"        ✓
  ClaimAmount: 5000                    ✓
  PolicyType: "Motor"                  ✓
  ClaimDescription: "Vehicle collision on Highway 101..." ✓

Textract Confidence: 97%

ClaimExtractionResult:
{
    ExtractedClaim: { ... },
    OverallConfidence: 0.94,           // 94% - High confidence
    FieldConfidences: {
        "policyNumber": 0.95,
        "claimAmount": 0.90,
        "policyType": 0.95,
        "claimDescription": 0.85
    },
    AmbiguousFields: [],               // None
    RawExtractedData: { "textractConfidence": 97 }
}
```

**Recommendation:** ✅ **Auto-process** - High confidence extraction

---

**Scenario 2: Medium-Quality Extraction**

```csharp
ExtractedClaim:
  PolicyNumber: "POLICY-24-001"       ⚠️ (non-standard format)
  ClaimAmount: 5000                    ✓
  PolicyType: "Auto"                   ⚠️ (should be "Motor")
  ClaimDescription: "Car accident"     ⚠️ (too short)

Textract Confidence: 82%

ClaimExtractionResult:
{
    ExtractedClaim: { ... },
    OverallConfidence: 0.62,           // 62% - Medium confidence
    FieldConfidences: {
        "policyNumber": 0.70,          // Non-standard format
        "claimAmount": 0.90,
        "policyType": 0.50,            // Invalid type
        "claimDescription": 0.40       // Too short
    },
    AmbiguousFields: ["policyType", "claimDescription"],
    RawExtractedData: { "textractConfidence": 82 }
}
```

**Recommendation:** ⚠️ **Manual Review Required** - Several ambiguous fields

---

**Scenario 3: Low-Quality Extraction**

```csharp
ExtractedClaim:
  PolicyNumber: "UNKNOWN"              ❌
  ClaimAmount: 0                       ❌
  PolicyType: "Car"                    ⚠️
  ClaimDescription: ""                 ❌

Textract Confidence: 68%

ClaimExtractionResult:
{
    ExtractedClaim: { ... },
    OverallConfidence: 0.29,           // 29% - Low confidence
    FieldConfidences: {
        "policyNumber": 0.30,          // Missing/unknown
        "claimAmount": 0.30,           // Invalid (0)
        "policyType": 0.50,            // Invalid type
        "claimDescription": 0.40       // Empty
    },
    AmbiguousFields: ["policyNumber", "claimAmount", "policyType", "claimDescription"],
    RawExtractedData: { "textractConfidence": 68 }
}
```

**Recommendation:** ❌ **Reject/Manual Entry** - Extraction failed, too many missing fields

---

###### **How Confidence Scores Drive Decisions**

The UI can use confidence scores to determine next steps:

```typescript
// In Angular ClaimFormComponent
handleExtractionResult(result: ClaimExtractionResult): void {
    if (result.overallConfidence >= 0.90) {
        // High confidence: Auto-populate form, allow immediate submission
        this.populateForm(result.extractedClaim);
        this.showMessage("Claim extracted successfully! Please review and submit.");
    }
    else if (result.overallConfidence >= 0.70) {
        // Medium confidence: Populate but highlight ambiguous fields
        this.populateForm(result.extractedClaim);
        this.highlightAmbiguousFields(result.ambiguousFields);
        this.showWarning("Please verify highlighted fields before submitting.");
    }
    else {
        // Low confidence: Show extracted data but require manual entry
        this.showError("Automatic extraction had low confidence. Please enter claim manually.");
        this.displayExtractedDataAsReference(result.extractedClaim);
    }
}
```

---

###### **Final Output: ClaimExtractionResult**

```csharp
public record ClaimExtractionResult(
    ClaimRequest ExtractedClaim,                    // The final structured claim
    float OverallConfidence,                        // 0.0 - 1.0
    Dictionary<string, float> FieldConfidences,     // Per-field confidence
    List<string> AmbiguousFields,                   // Fields needing review
    Dictionary<string, object> RawExtractedData     // Original data for debugging
);
```

This result is returned to the `DocumentsController` and sent back to the Angular UI.

---

#### **STEP 5: Response Returns to UI**

**Response Object:**
```typescript
interface SubmitDocumentResponse {
  uploadResult: DocumentUploadResult;
  extractionResult: ClaimExtractionResult;
  validationStatus: string;  // "ReadyForSubmission" | "ReadyForReview" | "RequiresCorrection"
  nextAction: string;         // "ReviewAndConfirm" | "CorrectFields: policyNumber, claimAmount"
}
```

**DocumentUploadComponent Emits to Parent:**
```typescript
handleDocumentSubmit(response: SubmitDocumentResponse): void {
  this.chatService.addBotMessage(
    `Document processed successfully!\n\n` +
    `Validation Status: ${response.validationStatus}\n` +
    `Overall Confidence: ${(response.extractionResult.overallConfidence * 100).toFixed(1)}%\n` +
    `Next Action: ${response.nextAction}`,
    'result',
    response
  );
}
```

**User sees:**
- Extracted claim data
- Confidence scores
- Fields requiring correction (if any)
- Option to submit or edit

---

## AWS Services Integration

### Service Matrix

| AWS Service | Purpose | API Used | Input | Output |
|------------|---------|----------|-------|--------|
| **Amazon Bedrock** (Titan Embeddings) | Generate embeddings for RAG | `InvokeModel` | Text string | float[1536] vector |
| **Amazon Bedrock** (Claude Sonnet 3.5) | LLM for decisions & extraction | `InvokeModel` | Prompt + context | JSON decision/extraction |
| **Amazon OpenSearch Serverless** | Vector database for policy retrieval | `_search` with KNN | Embedding vector | Top-K policy clauses |
| **Amazon S3** | Document storage | `PutObject`, `GetObject` | File binary | S3 URI |
| **Amazon Textract** | OCR & form extraction | `AnalyzeDocument` | S3 document | Text + key-values |
| **Amazon Comprehend** | NLP entity recognition | `DetectEntities` | Text | Entities (PERSON, DATE, etc.) |
| **Amazon Rekognition** | Image analysis | `DetectLabels` | S3 image | Labels (Car, Damage, etc.) |
| **Amazon DynamoDB** | Audit trail storage | `PutItem` | Audit record | Confirmation |

### Authentication & Configuration

**Configuration Source:** `appsettings.json`

```json
{
  "AWS": {
    "Region": "us-east-1",
    "AccessKeyId": "",  // Empty = use default credential chain
    "SecretAccessKey": "",
    "S3": {
      "DocumentBucket": "claims-documents-bucket",
      "UploadPrefix": "uploads/"
    },
    "OpenSearchEndpoint": "https://xxx.us-east-1.aoss.amazonaws.com",
    "OpenSearchIndexName": "policy-clauses"
  }
}
```

**Credential Chain Priority:**
1. `AccessKeyId` + `SecretAccessKey` in appsettings (if provided)
2. Environment variables (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`)
3. AWS credentials file (`~/.aws/credentials`)
4. IAM role (if running on EC2/ECS/Lambda)

---

## Data Storage & DynamoDB Tables

### Table 1: ClaimsAuditTrail

**Purpose:** Store every claim validation decision for compliance and audit

**Schema:**

| Attribute | Type | Description | Example |
|-----------|------|-------------|---------|
| `ClaimId` (PK) | String | Unique claim validation ID | `"abc-123-def-456"` |
| `Timestamp` (SK) | String | ISO 8601 timestamp | `"2026-01-30T10:30:00Z"` |
| `PolicyNumber` | String | Policy number from claim | `"POL-2024-001"` |
| `ClaimAmount` | Number | Claim amount in USD | `5000` |
| `ClaimDescription` | String | Claim description text | `"Vehicle collision..."` |
| `DecisionStatus` | String | Final decision | `"Covered"`, `"Manual Review"` |
| `Explanation` | String | LLM reasoning | `"Falls under MOTOR-001..."` |
| `ConfidenceScore` | Number | 0-1 confidence | `0.92` |
| `ClauseReferences` | List | Policy clause IDs used | `["MOTOR-001", "MOTOR-003"]` |
| `RequiredDocuments` | List | Documents needed | `["Police Report"]` |
| `RetrievedClauses` | String | JSON of retrieved clauses | `"[{\"ClauseId\":\"MOTOR-001\",\"Score\":0.95}]"` |

**Indexes:**
- Primary Key: `ClaimId`
- Global Secondary Index: `PolicyNumber-Timestamp-index` (for querying by policy)

**Sample Record:**
```json
{
  "ClaimId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "Timestamp": "2026-01-30T10:30:15.000Z",
  "PolicyNumber": "POL-2024-001",
  "ClaimAmount": 5000,
  "ClaimDescription": "Vehicle collision on highway resulting in front-end damage",
  "DecisionStatus": "Manual Review",
  "Explanation": "Amount $5000 exceeds auto-approval limit. The claim falls under Collision Coverage (MOTOR-001).",
  "ConfidenceScore": 0.92,
  "ClauseReferences": ["MOTOR-001"],
  "RequiredDocuments": ["Police Report", "Damage Photos", "Repair Estimate"],
  "RetrievedClauses": "[{\"ClauseId\":\"MOTOR-001\",\"Score\":0.95},{\"ClauseId\":\"MOTOR-003\",\"Score\":0.87}]"
}
```

**Data Retention:** 
- Production: 7 years (regulatory requirement)
- Development: 90 days

**Cost Optimization:**
- Use DynamoDB On-Demand pricing for unpredictable workloads
- Or Provisioned with auto-scaling for predictable traffic

---

### OpenSearch Index: policy-clauses

**Purpose:** Store policy document embeddings for RAG retrieval

**Schema:**

```json
{
  "mappings": {
    "properties": {
      "clauseId": { "type": "keyword" },
      "text": { "type": "text" },
      "embedding": { 
        "type": "knn_vector", 
        "dimension": 1536,
        "method": {
          "name": "hnsw",
          "engine": "nmslib",
          "parameters": {
            "ef_construction": 512,
            "m": 16
          }
        }
      },
      "policyType": { "type": "keyword" },
      "coverageType": { "type": "keyword" },
      "metadata": { "type": "object" }
    }
  }
}
```

**Sample Document:**
```json
{
  "clauseId": "MOTOR-001",
  "text": "Collision Coverage: Covers damage from vehicle-to-vehicle or vehicle-to-object collisions up to $50,000 per incident. Deductible applies as stated in policy schedule.",
  "embedding": [0.023, -0.145, 0.891, ...],  // 1536 floats
  "policyType": "motor",
  "coverageType": "Collision",
  "metadata": {
    "section": "Coverage Details",
    "page": 5,
    "effectiveDate": "2024-01-01"
  }
}
```

**Ingestion:** See `tools/PolicyIngestion/Program.cs` for batch ingestion process

---

## Business Rules & Validation Layer

### Rule Execution Order

```
1. LLM Decision (from Bedrock Claude)
   ↓
2. Confidence Threshold Check
   ↓
3. Amount Threshold Check
   ↓
4. Exclusion Clause Detection
   ↓
5. Final Decision
```

### Detailed Rule Specifications

#### Rule 1: Confidence Threshold
```csharp
if (decision.ConfidenceScore < 0.85f)
{
    Status = "Manual Review"
    Explanation = "Confidence below threshold (score < 0.85)"
}
```
**Rationale:** Low confidence indicates uncertainty - human review required

#### Rule 2: Auto-Approval Limit
```csharp
if (request.ClaimAmount > 5000m && decision.Status == "Covered")
{
    Status = "Manual Review"
    Explanation = "Amount $X exceeds auto-approval limit"
}
```
**Rationale:** High-value claims require human oversight regardless of AI confidence

#### Rule 3: Exclusion Clause Detection
```csharp
if (decision.ClauseReferences.Any(c => c.Contains("Exclusion")))
{
    Status = decision.Status == "Covered" ? "Manual Review" : decision.Status
    Explanation = "Potential exclusion clause detected"
}
```
**Rationale:** Exclusions are legally complex - always require review

### Decision Matrix

| LLM Decision | Confidence | Amount | Exclusion? | Final Status |
|-------------|-----------|--------|-----------|-------------|
| Covered | ≥ 0.85 | ≤ $5,000 | No | **Covered** |
| Covered | ≥ 0.85 | > $5,000 | No | **Manual Review** |
| Covered | < 0.85 | Any | No | **Manual Review** |
| Covered | Any | Any | Yes | **Manual Review** |
| Not Covered | ≥ 0.85 | Any | No | **Not Covered** |
| Not Covered | < 0.85 | Any | No | **Manual Review** |
| Manual Review | Any | Any | Any | **Manual Review** |

---

## Mock vs. Real Services

### Mock Service Detection

**OpenSearch:**
```csharp
_useRealOpenSearch = !string.IsNullOrEmpty(_opensearchEndpoint);

public async Task<List<PolicyClause>> RetrieveClausesAsync(float[] embedding, string policyType)
{
    if (!_useRealOpenSearch)
    {
        return await GetMockClausesAsync(policyType); // Returns hardcoded clauses
    }
    
    return await QueryOpenSearchAsync(embedding, policyType); // Real KNN search
}
```

**Detection Logic:** If `AWS:OpenSearchEndpoint` is empty/null in config → use mock

### Mock Data Sources

#### Mock Motor Policy Clauses
```csharp
private List<PolicyClause> GetMotorPolicyClauses()
{
    return new List<PolicyClause>
    {
        new PolicyClause(
            ClauseId: "MOTOR-001",
            Text: "Collision Coverage: Covers damage from vehicle-to-vehicle or vehicle-to-object collisions up to $50,000 per incident.",
            CoverageType: "Collision",
            Score: 0.95f
        ),
        new PolicyClause(
            ClauseId: "MOTOR-002",
            Text: "Comprehensive Coverage: Covers theft, vandalism, fire, natural disasters, and animal collisions. Maximum coverage: $30,000 per incident.",
            CoverageType: "Comprehensive",
            Score: 0.88f
        ),
        new PolicyClause(
            ClauseId: "MOTOR-003",
            Text: "Exclusions: Pre-existing damage, wear and tear, mechanical failures, racing, DUI-related incidents.",
            CoverageType: "Exclusions",
            Score: 0.75f
        ),
        new PolicyClause(
            ClauseId: "MOTOR-004",
            Text: "Deductible: $500 for collision claims, $250 for comprehensive claims.",
            CoverageType: "Deductible",
            Score: 0.70f
        ),
        new PolicyClause(
            ClauseId: "MOTOR-005",
            Text: "Required Documentation: Police report (for theft/hit-and-run), repair estimates, photos of damage.",
            CoverageType: "Documentation",
            Score: 0.65f
        )
    };
}
```

**Purpose:** Allows testing without OpenSearch infrastructure

### Service Status Indicators

When running the application, services log their status:

```
[S3] Using default credential chain for region: us-east-1
[Bedrock] Using default credential chain for region: us-east-1
[OpenSearch] Endpoint not configured - using mock data
[DynamoDB] Connected to table: ClaimsAuditTrail
```

**Real Services Required for Production:**
- ✅ Amazon Bedrock (required - no mock)
- ✅ Amazon S3 (required - no mock)
- ✅ Amazon Textract (required - no mock)
- ✅ Amazon Comprehend (required - no mock)
- 🟡 Amazon OpenSearch (has mock fallback)
- 🟡 Amazon Rekognition (optional)
- ✅ Amazon DynamoDB (required)

---

## Complete Flow Diagrams

### Diagram 1: Manual Claim Validation Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         ANGULAR UI LAYER                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  [User Input]                                                            │
│      ↓                                                                   │
│  ClaimFormComponent                                                      │
│   - Validates form (policyNumber, type, amount, description)            │
│   - Creates ClaimRequest object                                         │
│   - Emits claimSubmitted event                                          │
│      ↓                                                                   │
│  ChatComponent                                                           │
│   - Receives claim event                                                │
│   - Adds user message to chat                                           │
│   - Calls ClaimsApiService.validateClaim()                              │
│      ↓                                                                   │
│  ClaimsApiService                                                        │
│   - POST /api/claims/validate                                           │
│   - Headers: Content-Type: application/json                             │
│   - Body: { policyNumber, policyType, claimAmount, claimDescription }   │
│                                                                          │
└───────────────────────────────┬──────────────────────────────────────────┘
                                │ HTTP Request
                                ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                         .NET API LAYER                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ClaimsController                                                        │
│   - Receives ClaimRequest                                               │
│   - Logs request                                                        │
│   - Calls ClaimValidationOrchestrator.ValidateClaimAsync()              │
│                                                                          │
└───────────────────────────────┬──────────────────────────────────────────┘
                                │
                                ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                    RAG ORCHESTRATION LAYER                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ClaimValidationOrchestrator                                            │
│                                                                          │
│  Step 1: Generate Embedding                                             │
│    ├─> EmbeddingService.GenerateEmbeddingAsync()                        │
│    ├─> AWS Bedrock API: InvokeModel                                     │
│    ├─> Model: amazon.titan-embed-text-v1                                │
│    └─> Output: float[1536] embedding vector                             │
│                                                                          │
│  Step 2: Retrieve Policy Clauses                                        │
│    ├─> RetrievalService.RetrieveClausesAsync()                          │
│    ├─> AWS OpenSearch Serverless: KNN Search                            │
│    │   Query: { knn: { embedding: vector, k: 5 } }                      │
│    │   Filter: { term: { policyType: "motor" } }                        │
│    └─> Output: Top 5 relevant policy clauses with scores                │
│                                                                          │
│  Step 3: Guardrail Check                                                │
│    ├─> If no clauses found → return "Manual Review"                     │
│    └─> Continue if clauses retrieved                                    │
│                                                                          │
│  Step 4: Generate LLM Decision                                          │
│    ├─> LlmService.GenerateDecisionAsync()                               │
│    ├─> Build prompt with claim + retrieved clauses                      │
│    ├─> AWS Bedrock API: InvokeModel                                     │
│    ├─> Model: claude-3-5-sonnet-v2                                      │
│    ├─> System Prompt: "Use ONLY provided clauses, cite IDs..."          │
│    └─> Output: { status, explanation, clauseReferences, confidence }    │
│                                                                          │
│  Step 5: Apply Business Rules                                           │
│    ├─> Rule 1: Confidence < 0.85 → "Manual Review"                      │
│    ├─> Rule 2: Amount > $5000 + Covered → "Manual Review"               │
│    ├─> Rule 3: Exclusion clause detected → "Manual Review"              │
│    └─> Output: Final ClaimDecision                                      │
│                                                                          │
│  Step 6: Save Audit Trail                                               │
│    ├─> AuditService.SaveAsync()                                         │
│    ├─> AWS DynamoDB API: PutItem                                        │
│    ├─> Table: ClaimsAuditTrail                                          │
│    └─> Record: { ClaimId, Timestamp, PolicyNumber, Decision, ... }      │
│                                                                          │
│  Return: ClaimDecision                                                   │
│                                                                          │
└───────────────────────────────┬──────────────────────────────────────────┘
                                │
                                ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                      RESPONSE FLOW                                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ClaimsController                                                        │
│   - Receives ClaimDecision                                              │
│   - Logs result                                                         │
│   - Returns HTTP 200 OK with JSON body                                  │
│      ↓                                                                   │
│  Angular HttpClient                                                      │
│   - Receives response                                                   │
│   - Observable emits ClaimDecision                                      │
│      ↓                                                                   │
│  ChatComponent                                                           │
│   - Processes result                                                    │
│   - Calls ChatService.addBotMessage()                                   │
│   - Displays formatted result in chat UI                                │
│      ↓                                                                   │
│  [User sees decision]                                                    │
│   ✅ APPROVED / ❌ DENIED / ⚠️ MANUAL REVIEW                             │
│   Confidence: 92%                                                        │
│   Reasoning: Falls under Collision Coverage (MOTOR-001)...              │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

### Diagram 2: Document Upload & Extraction Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         ANGULAR UI LAYER                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  [User Upload Action]                                                    │
│      ↓                                                                   │
│  DocumentUploadComponent                                                 │
│   - User drags/selects file (PDF/JPG/PNG)                               │
│   - Validates: file type, size (< 10MB)                                 │
│   - User selects document type (ClaimForm/MedicalBills/etc.)            │
│   - User clicks "Upload & Extract"                                      │
│      ↓                                                                   │
│  ClaimsApiService                                                        │
│   - POST /api/documents/submit                                          │
│   - Content-Type: multipart/form-data                                   │
│   - Body: FormData { file: Binary, userId: string, documentType: enum } │
│                                                                          │
└───────────────────────────────┬──────────────────────────────────────────┘
                                │ HTTP Request
                                ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                         .NET API LAYER                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  DocumentsController.SubmitDocument()                                    │
│   - Validates file (size, type)                                         │
│   - Calls DocumentUploadService.UploadAsync()                           │
│      ↓                                                                   │
│  DocumentUploadService                                                   │
│   - Generates unique documentId (UUID)                                  │
│   - Constructs S3 key: uploads/{userId}/{documentId}/{fileName}         │
│   - AWS S3 API: PutObject                                               │
│   - Encryption: AES256                                                  │
│   - Metadata: documentId, userId, timestamp                             │
│   - Returns: DocumentUploadResult                                       │
│      ↓                                                                   │
│  DocumentsController                                                     │
│   - Calls DocumentExtractionOrchestrator.ExtractClaimDataAsync()        │
│                                                                          │
└───────────────────────────────┬──────────────────────────────────────────┘
                                │
                                ↓
┌─────────────────────────────────────────────────────────────────────────┐
│              DOCUMENT EXTRACTION ORCHESTRATION                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  DocumentExtractionOrchestrator                                         │
│                                                                          │
│  STAGE 1: Text Extraction (Amazon Textract)                             │
│    ├─> TextractService.AnalyzeDocumentAsync()                           │
│    ├─> AWS Textract API: AnalyzeDocument                                │
│    ├─> Features: ["FORMS", "TABLES"]                                    │
│    ├─> Input: S3 bucket + key                                           │
│    └─> Output: TextractResult {                                         │
│           ExtractedText: "INSURANCE CLAIM FORM\nPolicy: POL-2024-001...",│
│           KeyValuePairs: {                                               │
│             "Policy Number": "POL-2024-001",                             │
│             "Claim Amount": "5000",                                      │
│             "Policy Type": "Motor"                                       │
│           },                                                             │
│           Tables: [...],                                                 │
│           Confidence: 0.97                                               │
│        }                                                                 │
│                                                                          │
│  STAGE 2: Entity Recognition (Amazon Comprehend)                        │
│    ├─> ComprehendService.DetectEntitiesAsync()                          │
│    ├─> AWS Comprehend API: DetectEntities                               │
│    ├─> Input: Extracted text                                            │
│    └─> Output: List<ComprehendEntity> [                                 │
│           { Type: "PERSON", Text: "John Doe", Score: 0.99 },            │
│           { Type: "DATE", Text: "January 15, 2026", Score: 0.98 },      │
│           { Type: "QUANTITY", Text: "$5,000", Score: 0.95 }             │
│        ]                                                                 │
│                                                                          │
│    ├─> ComprehendService.ExtractClaimFieldsAsync()                      │
│    └─> Output: Dictionary<string, string> {                             │
│           "policyNumber": "POL-2024-001",                                │
│           "claimAmount": "5000",                                         │
│           "policyType": "Motor",                                         │
│           "incidentDate": "2026-01-15"                                   │
│        }                                                                 │
│                                                                          │
│  STAGE 3: Image Analysis (Amazon Rekognition) - OPTIONAL                │
│    ├─> Only if documentType == DamagePhotos                             │
│    ├─> RekognitionService.AnalyzeImagesAsync()                          │
│    ├─> AWS Rekognition API: DetectLabels                                │
│    └─> Output: ImageAnalysisResult {                                    │
│           Labels: [                                                      │
│             { Name: "Car", Confidence: 0.99 },                           │
│             { Name: "Damaged", Confidence: 0.92 },                       │
│             { Name: "Front Bumper", Confidence: 0.88 }                   │
│           ]                                                              │
│        }                                                                 │
│                                                                          │
│  STAGE 4: AI Synthesis (Amazon Bedrock Claude)                          │
│    ├─> Build comprehensive prompt with ALL extracted data:              │
│    │   - Textract text + key-value pairs                                │
│    │   - Comprehend entities                                            │
│    │   - Image labels (if present)                                      │
│    ├─> AWS Bedrock API: InvokeModel (Claude 3.5 Sonnet)                 │
│    ├─> Prompt: "Extract structured claim from this data..."             │
│    └─> Output: ClaimRequest {                                           │
│           policyNumber: "POL-2024-001",                                  │
│           policyType: "Motor",                                           │
│           claimAmount: 5000,                                             │
│           claimDescription: "Vehicle collision on highway..."            │
│        }                                                                 │
│                                                                          │
│  STAGE 5: Validation & Confidence Scoring                               │
│    ├─> Validate extracted fields:                                       │
│    │   - Policy number format                                           │
│    │   - Claim amount > 0                                               │
│    │   - Required fields present                                        │
│    ├─> Calculate overall confidence:                                    │
│    │   - Base: Textract confidence (0.97)                               │
│    │   - Penalty if ambiguous fields detected                           │
│    └─> Output: ClaimExtractionResult {                                  │
│           ExtractedClaim: ClaimRequest,                                  │
│           OverallConfidence: 0.94,                                       │
│           FieldConfidences: { policyNumber: 0.97, ... },                 │
│           AmbiguousFields: [],                                           │
│           Warnings: []                                                   │
│        }                                                                 │
│                                                                          │
│  Return: ClaimExtractionResult                                          │
│                                                                          │
└───────────────────────────────┬──────────────────────────────────────────┘
                                │
                                ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                      RESPONSE FLOW                                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  DocumentsController                                                     │
│   - Determines validation status:                                       │
│     • Confidence ≥ 0.85 → "ReadyForSubmission"                          │
│     • Confidence ≥ 0.70 → "ReadyForReview"                              │
│     • Confidence < 0.70 → "RequiresCorrection"                          │
│   - Determines next action:                                             │
│     • High confidence → "ReviewAndConfirm"                              │
│     • Ambiguous fields → "CorrectFields: policyNumber, claimAmount"    │
│     • Low confidence → "ManualEntry"                                    │
│   - Constructs SubmitDocumentResponse                                   │
│   - Returns HTTP 200 OK                                                 │
│      ↓                                                                   │
│  Angular HttpClient                                                      │
│   - Receives SubmitDocumentResponse                                     │
│      ↓                                                                   │
│  DocumentUploadComponent                                                 │
│   - Emits documentSubmitted event to ChatComponent                      │
│      ↓                                                                   │
│  ChatComponent                                                           │
│   - Displays extracted data:                                            │
│     • Policy Number: POL-2024-001                                       │
│     • Policy Type: Motor                                                │
│     • Claim Amount: $5,000                                              │
│     • Confidence: 94%                                                   │
│     • Status: ReadyForSubmission                                        │
│   - User can review and submit or edit fields                           │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

### Diagram 3: AWS Services Data Flow

```
┌────────────────────────────────────────────────────────────────────────┐
│                      AWS SERVICES ECOSYSTEM                             │
└────────────────────────────────────────────────────────────────────────┘

                    ┌─────────────────────────┐
                    │   User Input / Upload   │
                    └───────────┬─────────────┘
                                │
                ┌───────────────┴───────────────┐
                │                               │
                ▼                               ▼
        ┌──────────────┐              ┌──────────────────┐
        │ Manual Claim │              │ Document Upload  │
        └──────┬───────┘              └────────┬─────────┘
               │                               │
               │                               ▼
               │                      ┌─────────────────┐
               │                      │   Amazon S3     │
               │                      │  Document Store │
               │                      └────────┬────────┘
               │                               │
               │                      ┌────────┴────────┐
               │                      │                 │
               │                      ▼                 ▼
               │             ┌────────────────┐  ┌──────────────┐
               │             │ Amazon Textract│  │   Amazon     │
               │             │ OCR + Forms    │  │ Rekognition  │
               │             └────────┬───────┘  │ Image Labels │
               │                      │          └──────┬───────┘
               │                      │                 │
               │                      ▼                 │
               │             ┌────────────────┐         │
               │             │    Amazon      │         │
               │             │  Comprehend    │         │
               │             │   Entities     │         │
               │             └────────┬───────┘         │
               │                      │                 │
               │         ┌────────────┴─────────────────┘
               │         │
               │         ▼
               │   ┌──────────────────────┐
               │   │  Amazon Bedrock      │
               │   │  Claude 3.5 Sonnet   │
               │   │  (Data Synthesis)    │
               │   └──────────┬───────────┘
               │              │
               │              ▼
               │      ┌──────────────┐
               │      │ ClaimRequest │
               │      └──────┬───────┘
               │             │
               ▼             │
      ┌────────────────┐    │
      │ Claim          │◄───┘
      │ Description    │
      └────────┬───────┘
               │
               ▼
      ┌─────────────────────┐
      │   Amazon Bedrock    │
      │   Titan Embeddings  │
      │   (Generate Vector) │
      └──────────┬──────────┘
                 │
                 ▼ float[1536]
      ┌─────────────────────┐
      │  Amazon OpenSearch  │
      │  Serverless         │
      │  (KNN Vector Search)│
      └──────────┬──────────┘
                 │
                 ▼ Top-5 Policy Clauses
      ┌─────────────────────┐
      │   Amazon Bedrock    │
      │   Claude 3.5 Sonnet │
      │   (Generate Decision│
      │    with RAG context)│
      └──────────┬──────────┘
                 │
                 ▼ ClaimDecision
      ┌─────────────────────┐
      │  Business Rules     │
      │  Validation Layer   │
      └──────────┬──────────┘
                 │
                 ▼ Final Decision
      ┌─────────────────────┐
      │   Amazon DynamoDB   │
      │   Audit Trail Table │
      │   (Save Record)     │
      └──────────┬──────────┘
                 │
                 ▼
           ┌────────────┐
           │  Response  │
           │  to User   │
           └────────────┘
```

---

## Configuration Checklist

### Required Environment Variables / App Settings

```json
{
  "AWS": {
    "Region": "us-east-1",                          // ✅ Required
    "AccessKeyId": "",                               // 🟡 Optional (use IAM role if empty)
    "SecretAccessKey": "",                           // 🟡 Optional
    "S3": {
      "DocumentBucket": "claims-documents-bucket",   // ✅ Required
      "UploadPrefix": "uploads/"                     // ✅ Required
    },
    "OpenSearchEndpoint": "https://xxx.aoss.amazonaws.com", // 🟡 Optional (mock fallback)
    "OpenSearchIndexName": "policy-clauses"          // ✅ Required (if using OpenSearch)
  },
  "DocumentProcessing": {
    "MaxFileSizeMB": "10",                           // ✅ Required
    "AllowedContentTypes": [                         // ✅ Required
      "application/pdf",
      "image/jpeg",
      "image/png"
    ]
  }
}
```

### AWS Service Access Requirements

**IAM Permissions Needed:**
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
        "arn:aws:bedrock:us-east-1::foundation-model/amazon.titan-embed-text-v1",
        "arn:aws:bedrock:us-east-1::foundation-model/us.anthropic.claude-3-5-sonnet-20241022-v2:0"
      ]
    },
    {
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:ListBucket"
      ],
      "Resource": [
        "arn:aws:s3:::claims-documents-bucket",
        "arn:aws:s3:::claims-documents-bucket/*"
      ]
    },
    {
      "Effect": "Allow",
      "Action": [
        "textract:DetectDocumentText",
        "textract:AnalyzeDocument"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "comprehend:DetectEntities",
        "comprehend:DetectKeyPhrases"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "rekognition:DetectLabels"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "dynamodb:PutItem",
        "dynamodb:GetItem",
        "dynamodb:Query"
      ],
      "Resource": "arn:aws:dynamodb:us-east-1:*:table/ClaimsAuditTrail"
    },
    {
      "Effect": "Allow",
      "Action": [
        "aoss:APIAccessAll"
      ],
      "Resource": "arn:aws:aoss:us-east-1:*:collection/*"
    }
  ]
}
```

### Bedrock Model Access

**Required Models:**
1. `amazon.titan-embed-text-v1` - Embeddings
2. `us.anthropic.claude-3-5-sonnet-20241022-v2:0` - LLM

**Enable in AWS Console:**
- Navigate to Amazon Bedrock → Model access
- Request access to both models
- Wait for approval (usually instant for Titan, may take time for Claude)

---

## Error Handling & Logging

### Error Flow

**Frontend (Angular):**
```typescript
this.apiService.validateClaim(claim).subscribe({
  next: (result) => { /* Success */ },
  error: (error) => {
    this.chatService.addBotMessage(
      `❌ Error validating claim: ${error.error?.details || error.message}`
    );
  }
});
```

**Backend (.NET):**
```csharp
catch (AmazonBedrockRuntimeException ex)
{
    _logger.LogError(ex, "Bedrock API Error: {ErrorCode}", ex.ErrorCode);
    throw new Exception(
        $"Bedrock API Error: {ex.ErrorCode} - {ex.Message}. " +
        "Check: 1) Credentials valid, 2) Model access enabled, 3) IAM permissions",
        ex
    );
}
```

### Logging Levels

**Information:**
- Claim validation started/completed
- Document uploaded
- Extraction stages completed

**Warning:**
- OpenSearch query failed (fallback to mock)
- Low confidence scores
- Ambiguous field detection

**Error:**
- AWS service failures
- Authentication errors
- Data validation failures

---

## Testing Scenarios

### Test Case 1: Valid Motor Claim (Auto-Approved)

**Input:**
```json
{
  "policyNumber": "POL-2024-001",
  "policyType": "Motor",
  "claimAmount": 3000,
  "claimDescription": "Minor collision at parking lot, front bumper damage"
}
```

**Expected Flow:**
1. Embedding generated
2. Policy clauses retrieved (MOTOR-001: Collision Coverage)
3. Claude decides: "Covered"
4. Confidence: > 0.85
5. Amount: < $5,000
6. **Final Decision:** Covered

---

### Test Case 2: High-Value Claim (Manual Review)

**Input:**
```json
{
  "policyNumber": "POL-2024-002",
  "policyType": "Motor",
  "claimAmount": 15000,
  "claimDescription": "Major accident on highway, total front-end damage"
}
```

**Expected Flow:**
1. Embedding → Retrieval → Claude: "Covered"
2. Confidence: 0.92
3. Business Rule: Amount > $5,000
4. **Final Decision:** Manual Review (high value)

---

### Test Case 3: Document Upload (PDF Claim Form)

**Input:**
- File: `claim_form.pdf`
- Document Type: ClaimForm

**Expected Flow:**
1. Upload to S3: `uploads/john_doe/{uuid}/claim_form.pdf`
2. Textract extracts: Policy Number, Claim Amount, etc.
3. Comprehend identifies entities
4. Claude synthesizes: ClaimRequest
5. Validation: Confidence 0.94
6. **Status:** ReadyForSubmission

---

## Performance Metrics

### Expected Latencies

| Operation | Average Latency | Components Involved |
|-----------|----------------|---------------------|
| Manual Claim Validation | 3-5 seconds | Bedrock Embeddings (500ms) + OpenSearch (300ms) + Bedrock Claude (2-3s) + DynamoDB (200ms) |
| Document Upload | 1-2 seconds | S3 upload (500ms-1.5s depending on file size) |
| Document Extraction | 5-8 seconds | Textract (2-3s) + Comprehend (1s) + Bedrock (2-3s) + Validation (500ms) |
| Image Analysis | +2-3 seconds | Rekognition (2-3s additional) |

### Cost Estimation (per 1000 requests)

**Manual Claim Validation:**
- Bedrock Titan Embeddings: $0.13 (1000 tokens avg)
- Bedrock Claude 3.5 Sonnet: $3.00 (input) + $15.00 (output)
- OpenSearch: $0.24 (serverless queries)
- DynamoDB: $0.25 (on-demand writes)
- **Total:** ~$18.62 per 1000 claims

**Document Extraction:**
- S3 PUT: $0.005
- Textract: $1.50 (AnalyzeDocument)
- Comprehend: $0.50 (DetectEntities)
- Bedrock: $3.00 + $15.00
- **Total:** ~$20.00 per 1000 documents

---

## Deployment Considerations

### Environment-Specific Settings

**Development:**
- Mock OpenSearch enabled
- Verbose logging
- No cost optimization

**Staging:**
- Real AWS services
- Limited retention
- Test data only

**Production:**
- All real AWS services
- 7-year audit retention
- DynamoDB auto-scaling
- CloudWatch monitoring
- X-Ray tracing

### Scaling Strategies

**Horizontal Scaling:**
- Deploy multiple API instances behind load balancer
- DynamoDB auto-scales automatically
- OpenSearch Serverless handles scaling

**Vertical Scaling:**
- Increase API instance size for high throughput
- Bedrock has soft limits (requests per minute) - request increases from AWS

**Cost Optimization:**
- Cache frequent embeddings (Redis/ElastiCache)
- Batch document processing
- Use reserved capacity for DynamoDB if predictable load

---

## Conclusion

This document provides a **complete end-to-end execution flow** for the Claims RAG Bot application, covering:

✅ **User interactions** in Angular UI  
✅ **Component and service orchestration** in frontend  
✅ **HTTP API calls** to .NET backend  
✅ **RAG pipeline execution** with orchestrator  
✅ **AWS service integrations** (Bedrock, OpenSearch, S3, Textract, Comprehend, Rekognition, DynamoDB)  
✅ **Data storage** in DynamoDB audit trail  
✅ **Business rules** and validation layers  
✅ **Mock vs. real service** detection and fallback  
✅ **Complete flow diagrams** for visualization  

**Key Takeaways:**
- **Manual Claims:** User → Form → API → RAG (Embedding → Retrieval → LLM → Rules) → DynamoDB → Response
- **Document Upload:** User → Upload → S3 → Textract → Comprehend → Rekognition → Bedrock → Validation → Response
- **No Mocking in Production Critical Services:** Bedrock, S3, Textract, Comprehend, DynamoDB are always real
- **OpenSearch Has Mock Fallback:** For development without infrastructure
- **Business Rules Enforce Compliance:** Confidence thresholds, amount limits, exclusion detection

**For Developers:**
- Follow this document for debugging flow issues
- Use diagrams to understand service dependencies
- Reference AWS service sections for troubleshooting

**For Auditors:**
- All decisions stored in DynamoDB with full context
- Retrieved policy clauses logged
- Confidence scores tracked
- Business rule applications documented

---

**Document Version:** 1.0  
**Last Updated:** January 30, 2026  
**Maintained By:** Development Team  
**Next Review:** Quarterly or upon architecture changes
