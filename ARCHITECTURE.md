# Claims RAG Bot - System Architecture

## Overview

The Claims RAG Bot is an AI-powered insurance claims validation system built using **RAG (Retrieval-Augmented Generation)** architecture. It combines vector search, LLM reasoning, and business rules to automate claim decision-making while maintaining compliance and auditability.

## Architecture Pattern

**Clean Architecture** with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                    API Layer (ASP.NET Core)                  │
│                   ClaimsRagBot.Api                           │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│              Application Layer (Orchestration)               │
│                ClaimsRagBot.Application                      │
│              ClaimValidationOrchestrator                     │
└──────┬────────┬────────┬────────┬──────────────────────────┘
       │        │        │        │
┌──────▼────┐ ┌▼────┐ ┌─▼─────┐ ┌▼────────┐
│ Embedding │ │ LLM │ │Retriev│ │  Audit  │
│  Service  │ │Serv.│ │ Service│ │ Service │
└──────┬────┘ └┬────┘ └─┬─────┘ └┬────────┘
       │       │        │         │
┌──────▼───────▼────────▼─────────▼────────────────────────┐
│           Infrastructure Layer                             │
│          ClaimsRagBot.Infrastructure                       │
│  ┌──────────┐  ┌──────────┐  ┌─────────────┐            │
│  │ Bedrock  │  │OpenSearch│  │  DynamoDB   │            │
│  │(AWS SDK) │  │(AWS SDK) │  │  (AWS SDK)  │            │
│  └──────────┘  └──────────┘  └─────────────┘            │
└────────────────────────────────────────────────────────────┘
       │              │                │
┌──────▼──────┐ ┌────▼─────┐    ┌─────▼──────┐
│AWS Bedrock  │ │OpenSearch│    │ DynamoDB   │
│Claude 3.5   │ │Serverless│    │ Table      │
│Titan Embed. │ │          │    │            │
└─────────────┘ └──────────┘    └────────────┘
```

## Core Components

### 1. **API Layer** (`ClaimsRagBot.Api`)

**Entry Point:** `ClaimsController`

**Responsibilities:**
- Expose REST API endpoints
- Request validation
- Response formatting
- Error handling and logging

**Key Endpoint:**
```http
POST /api/claims/validate
Content-Type: application/json

{
  "policyNumber": "POL-12345",
  "claimDescription": "Car accident - front bumper damage",
  "claimAmount": 2500,
  "policyType": "Motor"
}
```

### 2. **Application Layer** (`ClaimsRagBot.Application`)

**Core Component:** `ClaimValidationOrchestrator`

**Responsibilities:**
- Orchestrate the RAG workflow
- Apply business rules and guardrails
- Coordinate between services
- Ensure compliance requirements

### 3. **Core Layer** (`ClaimsRagBot.Core`)

**Contains:**
- **Interfaces:** Service contracts (ILlmService, IEmbeddingService, etc.)
- **Models:** Domain entities (ClaimRequest, ClaimDecision, PolicyClause)

**Key Models:**

- **ClaimRequest:** Input data (policy number, description, amount)
- **ClaimDecision:** AI decision (status, explanation, confidence, required docs)
- **PolicyClause:** Retrieved policy text (clause ID, text, coverage type, relevance score)

### 4. **Infrastructure Layer** (`ClaimsRagBot.Infrastructure`)

**Implementation of interfaces using AWS services:**

#### a. **EmbeddingService** (`Bedrock/EmbeddingService.cs`)
- **AWS Service:** Amazon Bedrock - Titan Embeddings G1
- **Purpose:** Convert text to vector embeddings
- **Model:** `amazon.titan-embed-text-v1`

#### b. **LlmService** (`Bedrock/LlmService.cs`)
- **AWS Service:** Amazon Bedrock - Claude 3.5 Sonnet
- **Purpose:** Generate claim decisions with reasoning
- **Model:** `us.anthropic.claude-3-5-sonnet-20241022-v2:0`

#### c. **RetrievalService** (`OpenSearch/RetrievalService.cs`)
- **AWS Service:** Amazon OpenSearch Serverless
- **Purpose:** Vector similarity search for policy clauses
- **Fallback:** Mock data if OpenSearch not configured

#### d. **AuditService** (`DynamoDB/AuditService.cs`)
- **AWS Service:** Amazon DynamoDB
- **Purpose:** Store audit trail for compliance
- **Table:** `ClaimsAuditTrail`

## Complete Data Flow

### Step-by-Step Processing Pipeline

```
┌──────────────────────────────────────────────────────────────┐
│  1. API Request Reception                                     │
│     ClaimsController.ValidateClaim()                          │
│     Input: ClaimRequest (JSON)                                │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│  2. Orchestration Start                                       │
│     ClaimValidationOrchestrator.ValidateClaimAsync()          │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│  STEP 1: Generate Embedding                                   │
│  ─────────────────────────────────────────────────────       │
│  Service: EmbeddingService                                    │
│  Input: claim.ClaimDescription (text)                         │
│  AWS Call: Bedrock → Titan Embeddings                         │
│  Output: float[] embedding (1536 dimensions)                  │
│                                                               │
│  Example:                                                     │
│  "Car accident - front bumper damage"                         │
│       ↓                                                       │
│  [0.023, -0.145, 0.891, ..., 0.234] (1536 floats)           │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│  STEP 2: Retrieve Relevant Policy Clauses                    │
│  ─────────────────────────────────────────────────────────   │
│  Service: RetrievalService                                    │
│  Input: embedding (vector), policyType (string)               │
│  AWS Call: OpenSearch → KNN Vector Search                     │
│  Query:                                                       │
│    - Vector similarity search (k=5)                           │
│    - Filter by policyType = "Motor"                           │
│  Output: List<PolicyClause> (top 5 most relevant)            │
│                                                               │
│  Example Retrieved Clauses:                                   │
│  1. MOT-001: "Collision coverage: $500 deductible" (0.92)   │
│  2. MOT-004: "Physical damage covered" (0.87)               │
│  3. MOT-007: "Repair estimate required" (0.81)              │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│  STEP 3: Guardrail Check                                      │
│  ─────────────────────────────────────────────────────────   │
│  IF no clauses found:                                         │
│    → Return "Manual Review" immediately                       │
│    → Log to DynamoDB audit trail                              │
│    → Exit pipeline                                            │
└────────────────────┬─────────────────────────────────────────┘
                     │ (clauses found)
                     ▼
┌──────────────────────────────────────────────────────────────┐
│  STEP 4: Generate AI Decision                                 │
│  ─────────────────────────────────────────────────────────   │
│  Service: LlmService                                          │
│  Input: ClaimRequest + List<PolicyClause>                    │
│  AWS Call: Bedrock → Claude 3.5 Sonnet                        │
│                                                               │
│  Prompt Construction:                                         │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Claim:                                                  │  │
│  │ Policy: POL-12345                                       │  │
│  │ Amount: $2500                                           │  │
│  │ Description: Car accident - front bumper damage        │  │
│  │                                                         │  │
│  │ Policy Clauses:                                         │  │
│  │ [MOT-001] Collision: $500 deductible applies...        │  │
│  │ [MOT-004] Physical Damage: Covered under policy...     │  │
│  │                                                         │  │
│  │ Respond in JSON:                                        │  │
│  │ {                                                       │  │
│  │   "status": "Covered" | "Not Covered" | "Manual Review"│  │
│  │   "explanation": "...",                                 │  │
│  │   "clauseReferences": ["MOT-001"],                      │  │
│  │   "requiredDocuments": ["Police report"],              │  │
│  │   "confidenceScore": 0.0-1.0                           │  │
│  │ }                                                       │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                               │
│  Claude Response (parsed from JSON):                          │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Status: "Covered"                                       │  │
│  │ Explanation: "Front bumper damage falls under           │  │
│  │              collision coverage. $500 deductible..."    │  │
│  │ ClauseReferences: ["MOT-001", "MOT-004"]               │  │
│  │ RequiredDocuments: ["Police report", "Repair estimate"]│  │
│  │ ConfidenceScore: 0.95                                   │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                               │
│  Output: ClaimDecision (parsed from LLM JSON response)       │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│  STEP 5: Apply Business Rules                                 │
│  ─────────────────────────────────────────────────────────   │
│  Method: ApplyBusinessRules()                                 │
│                                                               │
│  Rule 1: Low Confidence Check                                │
│    IF confidenceScore < 0.85                                  │
│    → Override to "Manual Review"                              │
│                                                               │
│  Rule 2: High Amount Check                                    │
│    IF claimAmount > $5000 AND status = "Covered"             │
│    → Override to "Manual Review"                              │
│    → Reason: Exceeds auto-approval threshold                  │
│                                                               │
│  Rule 3: Exclusion Clause Detection                           │
│    IF any clauseReference contains "Exclusion"                │
│    → Force "Manual Review" or keep "Not Covered"              │
│                                                               │
│  Example:                                                     │
│    Input: Status="Covered", Amount=$2500, Confidence=0.95     │
│    Output: Status="Covered" (passes all rules)                │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│  STEP 6: Audit Trail (Compliance)                             │
│  ─────────────────────────────────────────────────────────   │
│  Service: AuditService                                        │
│  AWS Call: DynamoDB → PutItem                                 │
│  Table: ClaimsAuditTrail                                      │
│                                                               │
│  Stored Data:                                                 │
│  - ClaimId (generated UUID)                                   │
│  - Timestamp (ISO 8601)                                       │
│  - PolicyNumber, ClaimAmount, ClaimDescription                │
│  - DecisionStatus, Explanation, ConfidenceScore               │
│  - ClauseReferences (array)                                   │
│  - RequiredDocuments (array)                                  │
│  - RetrievedClauses (with scores)                             │
│                                                               │
│  Purpose: Regulatory compliance, dispute resolution, audit    │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│  7. Return Response to Client                                 │
│  ─────────────────────────────────────────────────────────   │
│  HTTP 200 OK                                                  │
│  Content-Type: application/json                               │
│                                                               │
│  {                                                            │
│    "status": "Covered",                                       │
│    "explanation": "Front bumper damage falls under...",       │
│    "clauseReferences": ["MOT-001", "MOT-004"],               │
│    "requiredDocuments": ["Police report", "Repair estimate"],│
│    "confidenceScore": 0.95                                    │
│  }                                                            │
└──────────────────────────────────────────────────────────────┘
```

## AWS Services Integration

### 1. **Amazon Bedrock**

**Models Used:**

- **Claude 3.5 Sonnet v2** (`us.anthropic.claude-3-5-sonnet-20241022-v2:0`)
  - Purpose: Claims reasoning and decision generation
  - Input: Claim + Policy clauses
  - Output: Structured JSON decision
  
- **Titan Embeddings G1** (`amazon.titan-embed-text-v1`)
  - Purpose: Text vectorization
  - Input: Claim description (text)
  - Output: 1536-dimensional embedding vector

**Configuration:**
```json
{
  "AWS": {
    "Region": "us-east-1",
    "AccessKeyId": "AKIA...",
    "SecretAccessKey": "...",
  }
}
```

### 2. **Amazon OpenSearch Serverless**

**Purpose:** Vector similarity search for policy retrieval

**Index Structure:**
```json
{
  "clauseId": "MOT-001",
  "text": "Collision coverage with $500 deductible",
  "coverageType": "Collision",
  "policyType": "motor",
  "embedding": [0.023, -0.145, ...]  // 1536 dims
}
```

**Query Type:** k-NN (k-Nearest Neighbors) with filters

**Fallback:** Mock data used if OpenSearch not configured

### 3. **Amazon DynamoDB**

**Table:** `ClaimsAuditTrail`

**Schema:**
- **Primary Key:** `ClaimId` (String, HASH)
- **Sort Key:** `Timestamp` (String, RANGE)
- **Attributes:** All claim and decision data

**Billing:** Pay-per-request (on-demand)

## Key Design Patterns

### 1. **RAG (Retrieval-Augmented Generation)**
- Combines vector search (retrieval) with LLM reasoning (generation)
- Grounds AI decisions in actual policy documents
- Reduces hallucinations by providing context

### 2. **Clean Architecture**
- **Core:** Domain models and interfaces (no dependencies)
- **Application:** Business logic and orchestration
- **Infrastructure:** AWS SDK implementations
- **API:** HTTP interface

### 3. **Dependency Injection**
All services registered in `Program.cs`:
```csharp
builder.Services.AddSingleton<IEmbeddingService>(sp =>
    new EmbeddingService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<ILlmService>(sp =>
    new LlmService(sp.GetRequiredService<IConfiguration>()));
// ... etc
```

### 4. **Guardrails & Business Rules**
- AI output validated against business rules
- Compliance checks (amount thresholds, confidence scores)
- Fallback to manual review when uncertain

### 5. **Observability**
- Structured logging (`ILogger`)
- Console debug output for development
- Audit trail for compliance

## Data Models

### Input: ClaimRequest
```csharp
record ClaimRequest(
    string PolicyNumber,      // "POL-12345"
    string ClaimDescription,  // "Car accident..."
    decimal ClaimAmount,      // 2500.00
    string PolicyType         // "Motor"
);
```

### Output: ClaimDecision
```csharp
record ClaimDecision(
    string Status,                  // "Covered" | "Not Covered" | "Manual Review"
    string Explanation,             // AI reasoning
    List<string> ClauseReferences,  // ["MOT-001", "MOT-004"]
    List<string> RequiredDocuments, // ["Police report"]
    float ConfidenceScore          // 0.95
);
```

### Internal: PolicyClause
```csharp
record PolicyClause(
    string ClauseId,      // "MOT-001"
    string Text,          // Full clause text
    string CoverageType,  // "Collision"
    float Score           // 0.92 (relevance from vector search)
);
```

## Configuration

### appsettings.json
```json
{
  "AWS": {
    "Region": "us-east-1",
    "AccessKeyId": "AKIA...",
    "SecretAccessKey": "...",
    "OpenSearchEndpoint": "https://....us-east-1.aoss.amazonaws.com",
    "OpenSearchIndexName": "policy-clauses"
  },
  "ClaimsValidation": {
    "AutoApprovalThreshold": 5000,
    "ConfidenceThreshold": 0.85
  }
}
```

## Error Handling

**Levels of Error Handling:**

1. **AWS Service Errors:** Caught and wrapped with helpful messages
2. **JSON Parsing Errors:** Logged with problematic content
3. **Business Logic Errors:** Fallback to manual review
4. **API Errors:** HTTP 500 with detailed error messages

**Example Error Response:**
```json
{
  "error": "Internal server error during claim validation",
  "details": "Bedrock API Error: ValidationException - Model not found",
  "timestamp": "2026-01-25T10:30:00Z"
}
```

## Performance Characteristics

**Typical Request Processing Time:**

1. Embedding Generation: ~200-500ms
2. Vector Search: ~100-300ms (OpenSearch) or ~10ms (mock)
3. LLM Decision: ~2-5 seconds (Claude 3.5 Sonnet)
4. Business Rules: ~10ms
5. Audit Save: ~100-200ms (DynamoDB)

**Total:** ~3-6 seconds per claim validation

## Security & Compliance

1. **Authentication:** AWS IAM credentials
2. **Audit Trail:** Every decision logged to DynamoDB
3. **Data Privacy:** No PII stored in vectors
4. **Explainability:** AI decisions include reasoning and clause references
5. **Guardrails:** Business rules prevent incorrect auto-approvals

## Future Enhancements

1. **Streaming Responses:** Use `InvokeModelWithResponseStream` for faster UX
2. **Caching:** Cache embeddings for common claim descriptions
3. **Batch Processing:** Process multiple claims in parallel
4. **Human-in-the-Loop:** Review interface for manual review cases
5. **A/B Testing:** Compare different LLM models
6. **Analytics Dashboard:** Track decision accuracy and patterns

## Testing

**Swagger UI:** http://localhost:5184/swagger

**Sample Request:**
```bash
curl -X POST http://localhost:5184/api/claims/validate \
  -H "Content-Type: application/json" \
  -d '{
    "policyNumber": "POL-12345",
    "claimDescription": "Car accident - front bumper damage",
    "claimAmount": 2500,
    "policyType": "Motor"
  }'
```

## Tech Stack Summary

| Layer | Technology |
|-------|------------|
| API | ASP.NET Core 10.0 |
| Language | C# 12 |
| Architecture | Clean Architecture |
| AI Model | Claude 3.5 Sonnet v2 |
| Embeddings | Titan Embeddings G1 |
| Vector DB | OpenSearch Serverless |
| Audit Store | DynamoDB |
| Cloud | AWS |
| DI Container | Built-in .NET DI |
| API Docs | Swagger/OpenAPI |

---

**Version:** 1.0  
**Last Updated:** January 2026  
**Author:** Claims RAG Bot Team
