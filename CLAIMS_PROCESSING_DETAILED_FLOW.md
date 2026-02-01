# Claims RAG Bot - Complete Processing Flow Documentation

## Table of Contents
1. [Overview](#overview)
2. [Architecture Diagram](#architecture-diagram)
3. [Claim Submission & Validation Flow (DETAILED)](#claim-submission--validation-flow-detailed)
4. [Rules Engine & Decision Logic](#rules-engine--decision-logic)
5. [Data Storage & Retrieval](#data-storage--retrieval)
6. [All Functional Flows](#all-functional-flows)
7. [Code File Reference Map](#code-file-reference-map)

---

## Overview

The Claims RAG Bot is an AI-powered insurance claims validation system that uses:
- **RAG (Retrieval-Augmented Generation)**: Retrieves relevant policy clauses before AI decision-making
- **AWS Bedrock**: Claude 3.5 Sonnet for AI reasoning and decision generation
- **OpenSearch**: Vector database for semantic search of policy clauses
- **DynamoDB**: Audit trail and claims storage
- **Business Rules Engine**: Aflac-style validation rules overlaid on AI decisions

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          USER INTERFACE (Angular)                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌───────────────┐        ┌────────────────┐        ┌──────────────────┐   │
│  │ Role Selection│   →    │  Submit Claim  │   →    │ Claims Dashboard │   │
│  │   Component   │        │   (Chat/Form)  │        │   (Specialist)   │   │
│  └───────────────┘        └────────────────┘        └──────────────────┘   │
│         │                          │                          │              │
│         └──────────────────────────┴──────────────────────────┘              │
│                                    │                                         │
└────────────────────────────────────┼─────────────────────────────────────────┘
                                     │
                                     │ HTTP POST /api/claims/validate
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        ASP.NET CORE WEB API                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    ClaimsController.cs                               │   │
│  │  - ValidateClaim(ClaimRequest) → ClaimDecision                       │   │
│  │  - GetAllClaims(status?) → List<ClaimAuditRecord>                    │   │
│  │  - GetClaimById(claimId) → ClaimAuditRecord                          │   │
│  │  - UpdateClaimDecision(claimId, update) → Success                    │   │
│  └──────────────────────────────┬───────────────────────────────────────┘   │
│                                 │                                            │
│                                 │ Calls Orchestrator                         │
│                                 ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │              ClaimValidationOrchestrator.cs                          │   │
│  │                                                                       │   │
│  │  ORCHESTRATION FLOW:                                                 │   │
│  │  1. Generate embedding for claim description                         │   │
│  │  2. Retrieve relevant policy clauses (semantic search)               │   │
│  │  3. Check guardrail - no clauses found?                              │   │
│  │  4. Generate AI decision using LLM                                   │   │
│  │  5. Apply business rules (thresholds, exclusions)                    │   │
│  │  6. Save to audit trail (DynamoDB)                                   │   │
│  │  7. Return final decision                                            │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘
                    │                  │                  │
                    │                  │                  │
        ┌───────────┘                  │                  └───────────┐
        │                              │                              │
        ▼                              ▼                              ▼
┌───────────────┐          ┌───────────────────┐          ┌──────────────────┐
│ EmbeddingService       │ RetrievalService   │          │  LlmService      │
│ (Bedrock)     │          │ (OpenSearch)      │          │  (Bedrock)       │
└───────┬───────┘          └─────────┬─────────┘          └────────┬─────────┘
        │                            │                             │
        │                            │                             │
        ▼                            ▼                             ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              AWS SERVICES                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌──────────────────┐   ┌──────────────────┐   ┌──────────────────────┐   │
│  │  Amazon Bedrock  │   │   OpenSearch     │   │     DynamoDB         │   │
│  │  (AI/LLM)        │   │   Serverless     │   │  (Audit Trail)       │   │
│  ├──────────────────┤   ├──────────────────┤   ├──────────────────────┤   │
│  │ • Claude 3.5     │   │ • Vector Search  │   │ Table:               │   │
│  │   Sonnet         │   │ • Embeddings     │   │  ClaimsAuditTrail    │   │
│  │ • Titan Embed    │   │ • Policy Clauses │   │                      │   │
│  │   (Embeddings)   │   │   Index          │   │ Key: ClaimId (HASH)  │   │
│  │                  │   │                  │   │ GSI: PolicyNumber    │   │
│  │ Endpoints:       │   │ Index:           │   │                      │   │
│  │ - InvokeModel    │   │  policy-clauses  │   │ Operations:          │   │
│  │ - Generate       │   │                  │   │ - PutItem            │   │
│  │   Embedding      │   │ Filters:         │   │ - GetItem            │   │
│  │                  │   │ - policyType     │   │ - Scan               │   │
│  │                  │   │ - k=5            │   │ - UpdateItem         │   │
│  └──────────────────┘   └──────────────────┘   └──────────────────────┘   │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Claim Submission & Validation Flow (DETAILED)

### Step-by-Step Processing with Code References

This is the **CORE FLOW** when a user submits a claim for validation.

---

### **STEP 1: User Submits Claim (Frontend)**

**User Action**: User fills out claim form or types in chat interface

**UI Component**: 
- File: `claims-chatbot-ui/src/app/components/chat/chat.component.ts`
- File: `claims-chatbot-ui/src/app/components/claim-form/claim-form.component.ts`

**Frontend Service Call**:
```typescript
// File: claims-chatbot-ui/src/app/services/claims-api.service.ts
validateClaim(claim: ClaimRequest): Observable<ClaimDecision> {
  return this.http.post<ClaimDecision>(`${this.baseUrl}/claims/validate`, claim);
}
```

**Request Payload (ClaimRequest)**:
```json
{
  "policyNumber": "AFLAC-HOSP-2024-001",
  "claimDescription": "Hospital confinement for 3 days due to pneumonia",
  "claimAmount": 1500,
  "policyType": "Health"
}
```

**Data Model**: `claims-chatbot-ui/src/app/models/claim.model.ts`

---

### **STEP 2: API Controller Receives Request (Backend)**

**Controller**: 
- File: `src/ClaimsRagBot.Api/Controllers/ClaimsController.cs`
- Method: `ValidateClaim([FromBody] ClaimRequest request)`
- Endpoint: `POST /api/claims/validate`

**Code Flow**:
```csharp
[HttpPost("validate")]
public async Task<ActionResult<ClaimDecision>> ValidateClaim([FromBody] ClaimRequest request)
{
    _logger.LogInformation("Validating claim for policy {PolicyNumber}, amount: ${Amount}",
        request.PolicyNumber, request.ClaimAmount);
    
    // Call orchestrator
    var decision = await _orchestrator.ValidateClaimAsync(request);
    
    return Ok(decision);
}
```

**What Happens**:
1. Request validation (model binding)
2. Logging of claim submission
3. Call to orchestrator
4. Return JSON response

---

### **STEP 3: Orchestration Begins (Application Layer)**

**Orchestrator**: 
- File: `src/ClaimsRagBot.Application/RAG/ClaimValidationOrchestrator.cs`
- Method: `ValidateClaimAsync(ClaimRequest request)`

This is the **BRAIN** of the entire system. It coordinates all steps.

**Complete Code Flow**:

```csharp
public async Task<ClaimDecision> ValidateClaimAsync(ClaimRequest request)
{
    // ============================================
    // STEP 3.1: Generate Embedding
    // ============================================
    var embedding = await _embeddingService.GenerateEmbeddingAsync(request.ClaimDescription);
    
    // ============================================
    // STEP 3.2: Retrieve Policy Clauses
    // ============================================
    var clauses = await _retrievalService.RetrieveClausesAsync(embedding, request.PolicyType);
    
    // ============================================
    // STEP 3.3: Guardrail Check
    // ============================================
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
    
    // ============================================
    // STEP 3.4: Generate AI Decision
    // ============================================
    var decision = await _llmService.GenerateDecisionAsync(request, clauses);
    
    // ============================================
    // STEP 3.5: Apply Business Rules
    // ============================================
    decision = ApplyBusinessRules(decision, request);
    
    // ============================================
    // STEP 3.6: Save Audit Trail
    // ============================================
    await _auditService.SaveAsync(request, decision, clauses);
    
    return decision;
}
```

---

### **STEP 3.1: Generate Embedding for Claim Description**

**Purpose**: Convert claim text into a vector (array of numbers) for semantic search

**Service**: 
- File: `src/ClaimsRagBot.Infrastructure/Bedrock/EmbeddingService.cs`
- Method: `GenerateEmbeddingAsync(string text)`

**AWS Service**: **Amazon Bedrock - Titan Embeddings**

**What Happens**:
1. Claim description text sent to AWS Bedrock
2. Model: `amazon.titan-embed-text-v1`
3. Returns: 1536-dimension vector (float array)

**Code**:
```csharp
public async Task<float[]> GenerateEmbeddingAsync(string text)
{
    var requestBody = new
    {
        inputText = text  // "Hospital confinement for 3 days due to pneumonia"
    };

    var request = new InvokeModelRequest
    {
        ModelId = "amazon.titan-embed-text-v1",
        Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestBody))),
        ContentType = "application/json"
    };

    var response = await _client.InvokeModelAsync(request);
    
    using var reader = new StreamReader(response.Body);
    var responseBody = await reader.ReadToEndAsync();
    var result = JsonSerializer.Deserialize<EmbeddingResponse>(responseBody);

    return result?.Embedding ?? Array.Empty<float>();
}
```

**Example Output**:
```
[0.0234, -0.0567, 0.1234, ..., 0.0891]  // 1536 numbers
```

---

### **STEP 3.2: Retrieve Relevant Policy Clauses (RAG Retrieval)**

**Purpose**: Find the most relevant policy clauses using semantic similarity

**Service**: 
- File: `src/ClaimsRagBot.Infrastructure/OpenSearch/RetrievalService.cs`
- Method: `RetrieveClausesAsync(float[] embedding, string policyType)`

**AWS Service**: **Amazon OpenSearch Serverless**

**What Happens**:
1. Takes embedding vector from Step 3.1
2. Performs k-Nearest Neighbor (kNN) search in OpenSearch
3. Filters by policy type (Health, Motor, etc.)
4. Returns top 5 most relevant clauses

**Code**:
```csharp
private async Task<List<PolicyClause>> QueryOpenSearchAsync(float[] embedding, string policyType)
{
    var searchQuery = new
    {
        size = 5,  // Return top 5 results
        query = new
        {
            @bool = new
            {
                must = new object[]
                {
                    new
                    {
                        knn = new  // k-Nearest Neighbor search
                        {
                            embedding = new
                            {
                                vector = embedding,  // The 1536-dimensional vector
                                k = 5
                            }
                        }
                    }
                },
                filter = new[]
                {
                    new
                    {
                        term = new
                        {
                            policyType = policyType.ToLower()  // "health"
                        }
                    }
                }
            }
        },
        _source = new[] { "clauseId", "text", "coverageType", "policyType" }
    };

    var requestUri = $"{_opensearchEndpoint}/{_indexName}/_search";
    var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
    {
        Content = new StringContent(JsonSerializer.Serialize(searchQuery), Encoding.UTF8, "application/json")
    };

    await SignRequestAsync(request);  // AWS SigV4 authentication
    var response = await _httpClient.SendAsync(request);
    
    var responseContent = await response.Content.ReadAsStringAsync();
    var searchResult = JsonSerializer.Deserialize<OpenSearchResponse>(responseContent);

    return searchResult?.Hits?.Hits?
        .Select((hit, index) => new PolicyClause(
            ClauseId: hit.Source?.ClauseId ?? $"UNKNOWN-{index}",
            Text: hit.Source?.Text ?? "",
            CoverageType: hit.Source?.CoverageType ?? "",
            Score: hit.Score ?? 0.0f  // Similarity score
        ))
        .ToList() ?? new List<PolicyClause>();
}
```

**Example Output**:
```csharp
[
  {
    ClauseId: "AFLAC-HOSP-2.1",
    Text: "Hospital Indemnity benefit pays $500 per day for hospital confinement",
    CoverageType: "Hospital Indemnity",
    Score: 0.92
  },
  {
    ClauseId: "AFLAC-HOSP-3.2",
    Text: "Covered conditions include pneumonia, cardiac events, and surgical procedures",
    CoverageType: "Covered Conditions",
    Score: 0.89
  },
  ...
]
```

---

### **STEP 3.3: Guardrail Check - No Clauses Found**

**Purpose**: Safety mechanism if no relevant policy clauses exist

**Code** (in `ClaimValidationOrchestrator.cs`):
```csharp
if (!clauses.Any())
{
    var manualReviewDecision = new ClaimDecision(
        Status: "Manual Review",
        Explanation: "No relevant policy clauses found for this claim type",
        ClauseReferences: new List<string>(),
        RequiredDocuments: new List<string> { "Policy Document", "Claim Evidence" },
        ConfidenceScore: 0.0f
    );
    
    // Still save to audit trail
    await _auditService.SaveAsync(request, manualReviewDecision, clauses);
    return manualReviewDecision;
}
```

**Why This Matters**:
- Prevents AI from making decisions without policy context
- Regulatory compliance requirement
- Ensures human review for edge cases

---

### **STEP 3.4: Generate AI Decision Using LLM**

**Purpose**: Use AI to analyze the claim against retrieved policy clauses

**Service**: 
- File: `src/ClaimsRagBot.Infrastructure/Bedrock/LlmService.cs`
- Method: `GenerateDecisionAsync(ClaimRequest request, List<PolicyClause> clauses)`

**AWS Service**: **Amazon Bedrock - Claude 3.5 Sonnet**

**What Happens**:
1. Build a prompt combining claim details + policy clauses
2. Send to Claude AI model
3. Receive structured JSON decision

**Prompt Construction**:
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

**Example Prompt Sent to Claude**:
```
Claim:
Policy Number: AFLAC-HOSP-2024-001
Claim Amount: $1500
Description: Hospital confinement for 3 days due to pneumonia

Policy Clauses:
[AFLAC-HOSP-2.1] Hospital Indemnity: Hospital Indemnity benefit pays $500 per day for hospital confinement

[AFLAC-HOSP-3.2] Covered Conditions: Covered conditions include pneumonia, cardiac events, and surgical procedures

Respond in JSON:
{
  "status": "Covered" | "Not Covered" | "Manual Review",
  "explanation": "<explanation>",
  "clauseReferences": ["<clause_id>"],
  "requiredDocuments": ["<document>"],
  "confidenceScore": 0.0-1.0
}
```

**Bedrock API Call**:
```csharp
public async Task<ClaimDecision> GenerateDecisionAsync(ClaimRequest request, List<PolicyClause> clauses)
{
    var prompt = BuildPrompt(request, clauses);
    
    var requestBody = new
    {
        anthropic_version = "bedrock-2023-05-31",
        max_tokens = 1024,
        messages = new[]
        {
            new { role = "user", content = prompt }
        },
        system = @"You are an insurance claims validation assistant.
You MUST:
- Use ONLY the provided policy clauses
- Cite clause IDs
- If unsure, say 'Needs Manual Review'
- Respond in valid JSON format only"
    };

    var invokeRequest = new InvokeModelRequest
    {
        ModelId = "us.anthropic.claude-3-5-sonnet-20241022-v2:0",
        Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestBody))),
        ContentType = "application/json",
        Accept = "application/json"
    };

    var response = await _client.InvokeModelAsync(invokeRequest);
    
    using var reader = new StreamReader(response.Body);
    var responseBody = await reader.ReadToEndAsync();
    var result = JsonSerializer.Deserialize<ClaudeResponse>(responseBody);
    var content = result?.Content?.FirstOrDefault()?.Text ?? "{}";
    
    // Remove markdown code blocks if present
    content = content.Replace("```json", "").Replace("```", "").Trim();
    
    var decision = JsonSerializer.Deserialize<ClaimDecision>(content);
    
    return decision ?? new ClaimDecision(
        Status: "Manual Review",
        Explanation: "Failed to parse LLM response",
        ClauseReferences: new List<string>(),
        RequiredDocuments: new List<string>(),
        ConfidenceScore: 0.0f
    );
}
```

**Example Claude Response**:
```json
{
  "status": "Covered",
  "explanation": "Hospital Indemnity benefit pays $500/day for 3 days of confinement due to pneumonia, which is a covered condition. Total benefit: $1500.",
  "clauseReferences": ["AFLAC-HOSP-2.1", "AFLAC-HOSP-3.2"],
  "requiredDocuments": ["Hospital admission records", "Discharge summary", "Diagnosis documentation"],
  "confidenceScore": 0.96
}
```

---

### **STEP 3.5: Apply Business Rules (Rules Engine)**

**Purpose**: Overlay Aflac-specific business logic on top of AI decision

**File**: `src/ClaimsRagBot.Application/RAG/ClaimValidationOrchestrator.cs`
**Method**: `ApplyBusinessRules(ClaimDecision decision, ClaimRequest request)`

**Code**:
```csharp
private ClaimDecision ApplyBusinessRules(ClaimDecision decision, ClaimRequest request)
{
    const decimal autoApprovalThreshold = 5000m;
    const float confidenceThreshold = 0.85f;

    // ============================================
    // RULE 1: Low Confidence → Manual Review
    // ============================================
    if (decision.ConfidenceScore < confidenceThreshold)
    {
        return decision with
        {
            Status = "Manual Review",
            Explanation = $"Confidence below threshold ({decision.ConfidenceScore:F2} < {confidenceThreshold}). " 
                        + decision.Explanation
        };
    }

    // ============================================
    // RULE 2: High Amount + Covered → Manual Review
    // ============================================
    if (request.ClaimAmount > autoApprovalThreshold && decision.Status == "Covered")
    {
        return decision with
        {
            Status = "Manual Review",
            Explanation = $"Amount ${request.ClaimAmount} exceeds auto-approval limit. " 
                        + decision.Explanation
        };
    }

    // ============================================
    // RULE 3: Exclusion Clause Detected
    // ============================================
    if (decision.ClauseReferences.Any(c => c.Contains("Exclusion", StringComparison.OrdinalIgnoreCase)))
    {
        return decision with
        {
            Status = decision.Status == "Covered" ? "Manual Review" : decision.Status,
            Explanation = "Potential exclusion clause detected. " + decision.Explanation
        };
    }

    return decision;  // No rule modifications needed
}
```

**Rule Logic Explained**:

| Rule | Condition | Action | Reason |
|------|-----------|--------|--------|
| **Rule 1** | Confidence < 85% | Set to "Manual Review" | AI is not confident enough |
| **Rule 2** | Amount > $5000 AND Status = "Covered" | Set to "Manual Review" | Large claims need human approval |
| **Rule 3** | Clause contains "Exclusion" | Set to "Manual Review" | Potential exclusion needs review |

**Example Application**:

**Scenario 1**: Low Confidence
```
AI Decision: { Status: "Covered", ConfidenceScore: 0.71 }
After Rule 1: { Status: "Manual Review", Explanation: "Confidence below threshold (0.71 < 0.85)..." }
```

**Scenario 2**: High Amount
```
Claim Amount: $25,000
AI Decision: { Status: "Covered", ConfidenceScore: 0.98 }
After Rule 2: { Status: "Manual Review", Explanation: "Amount $25000 exceeds auto-approval limit..." }
```

**Scenario 3**: Clean Approval
```
Claim Amount: $1,500
AI Decision: { Status: "Covered", ConfidenceScore: 0.96 }
After All Rules: { Status: "Covered", ConfidenceScore: 0.96 }  ✓ No changes
```

---

### **STEP 3.6: Save to Audit Trail (DynamoDB)**

**Purpose**: Store every claim decision for compliance, tracking, and specialist review

**Service**: 
- File: `src/ClaimsRagBot.Infrastructure/DynamoDB/AuditService.cs`
- Method: `SaveAsync(ClaimRequest request, ClaimDecision decision, List<PolicyClause> clauses)`

**AWS Service**: **Amazon DynamoDB - ClaimsAuditTrail Table**

**Table Schema**:
```
Table Name: ClaimsAuditTrail
Primary Key: ClaimId (HASH)
Global Secondary Index: PolicyNumber-index
```

**Code**:
```csharp
public async Task SaveAsync(ClaimRequest request, ClaimDecision decision, List<PolicyClause> clauses)
{
    var auditRecord = new Dictionary<string, AttributeValue>
    {
        ["ClaimId"] = new AttributeValue { S = Guid.NewGuid().ToString() },  // Unique claim ID
        ["Timestamp"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
        ["PolicyNumber"] = new AttributeValue { S = request.PolicyNumber },
        ["ClaimAmount"] = new AttributeValue { N = request.ClaimAmount.ToString() },
        ["ClaimDescription"] = new AttributeValue { S = request.ClaimDescription },
        ["DecisionStatus"] = new AttributeValue { S = decision.Status },
        ["Explanation"] = new AttributeValue { S = decision.Explanation },
        ["ConfidenceScore"] = new AttributeValue { N = decision.ConfidenceScore.ToString() },
        ["ClauseReferences"] = new AttributeValue 
        { 
            L = decision.ClauseReferences.Select(c => new AttributeValue { S = c }).ToList() 
        },
        ["RequiredDocuments"] = new AttributeValue 
        { 
            L = decision.RequiredDocuments.Select(d => new AttributeValue { S = d }).ToList() 
        },
        ["RetrievedClauses"] = new AttributeValue 
        { 
            S = JsonSerializer.Serialize(clauses.Select(c => new { c.ClauseId, c.Score })) 
        }
    };

    var putRequest = new PutItemRequest
    {
        TableName = "ClaimsAuditTrail",
        Item = auditRecord
    };

    await _client.PutItemAsync(putRequest);
}
```

**Example DynamoDB Record**:
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

**Why This Is Critical**:
- ✅ **Compliance**: Regulatory requirement for insurance claims
- ✅ **Auditability**: Full trace of AI reasoning
- ✅ **Transparency**: Shows which policy clauses were considered
- ✅ **Specialist Review**: Enables human override later
- ✅ **Analytics**: Track approval rates, common denials, etc.

---

### **STEP 4: Return Decision to Frontend**

**Backend Response** (ClaimsController):
```csharp
return Ok(decision);  // HTTP 200 with ClaimDecision JSON
```

**Response JSON**:
```json
{
  "status": "Covered",
  "explanation": "Hospital Indemnity benefit pays $500/day for 3 days of confinement due to pneumonia, which is a covered condition. Total benefit: $1500.",
  "clauseReferences": ["AFLAC-HOSP-2.1", "AFLAC-HOSP-3.2"],
  "requiredDocuments": ["Hospital admission records", "Discharge summary", "Diagnosis documentation"],
  "confidenceScore": 0.96
}
```

**Frontend Receives** (`claims-api.service.ts`):
```typescript
this.http.post<ClaimDecision>(`${this.baseUrl}/claims/validate`, claim)
  .subscribe(decision => {
    // Display result in UI
    console.log('Claim Decision:', decision);
  });
```

**UI Display** (`claim-result.component.ts`):
- Shows status badge (green for Covered, red for Not Covered, yellow for Manual Review)
- Displays explanation
- Lists required documents
- Shows confidence score

---

## Rules Engine & Decision Logic

### Business Rules Overview

The system implements a **two-tier decision framework**:

1. **AI Reasoning Layer** (Claude 3.5 Sonnet)
   - Semantic understanding of claim vs. policy
   - Natural language reasoning
   - Confidence scoring based on policy match

2. **Business Rules Layer** (Hardcoded Logic)
   - Threshold enforcement
   - Exclusion detection
   - Escalation triggers

### Rules Implementation Matrix

| Rule Category | Logic Location | AWS Service | Purpose |
|---------------|---------------|-------------|---------|
| **AI Decision** | `LlmService.cs` | Bedrock | Semantic claim-policy matching |
| **Confidence Threshold** | `ClaimValidationOrchestrator.cs` | None | Override low-confidence AI decisions |
| **Amount Threshold** | `ClaimValidationOrchestrator.cs` | None | Escalate high-value claims |
| **Exclusion Detection** | `ClaimValidationOrchestrator.cs` | None | Flag potential exclusions |
| **Policy Retrieval** | `RetrievalService.cs` | OpenSearch | Ensure relevant context |

### Decision Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                      CLAIM VALIDATION FLOW                          │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
                    ┌─────────────────────────┐
                    │  Retrieve Policy Clauses │
                    │  (OpenSearch kNN)        │
                    └────────────┬─────────────┘
                                 │
                         ┌───────┴────────┐
                         │   Clauses      │
                         │   Found?       │
                         └───────┬────────┘
                                 │
                    ┌────────────┴────────────┐
                    │ NO                      │ YES
                    ▼                         ▼
          ┌─────────────────┐      ┌──────────────────┐
          │ Manual Review   │      │ Generate AI      │
          │ (No Context)    │      │ Decision (Claude)│
          └─────────────────┘      └─────────┬────────┘
                    │                        │
                    │                        ▼
                    │              ┌──────────────────┐
                    │              │ AI Returns JSON: │
                    │              │ - Status         │
                    │              │ - Explanation    │
                    │              │ - Confidence     │
                    │              └─────────┬────────┘
                    │                        │
                    │                        ▼
                    │              ┌──────────────────────┐
                    │              │ RULE 1: Confidence?  │
                    │              │ Score < 0.85?        │
                    │              └─────────┬────────────┘
                    │                        │
                    │              ┌─────────┴─────────┐
                    │              │ YES               │ NO
                    │              ▼                   ▼
                    │      ┌──────────────┐   ┌─────────────────────┐
                    │      │ Manual Review│   │ RULE 2: Amount?     │
                    │      │ (Low Conf.)  │   │ Amount > $5000 AND  │
                    │      └──────────────┘   │ Status = Covered?   │
                    │              │           └─────────┬───────────┘
                    │              │                     │
                    │              │           ┌─────────┴──────────┐
                    │              │           │ YES                │ NO
                    │              │           ▼                    ▼
                    │              │   ┌──────────────┐    ┌────────────────┐
                    │              │   │ Manual Review│    │ RULE 3:        │
                    │              │   │ (High Amount)│    │ Exclusion?     │
                    │              │   └──────────────┘    └────────┬───────┘
                    │              │           │                    │
                    │              │           │          ┌─────────┴──────┐
                    │              │           │          │ YES            │ NO
                    │              │           │          ▼                ▼
                    │              │           │  ┌──────────────┐  ┌─────────┐
                    │              │           │  │ Manual Review│  │ APPROVE │
                    │              │           │  │ (Exclusion)  │  │ or DENY │
                    │              │           │  └──────────────┘  └─────────┘
                    │              │           │          │               │
                    └──────────────┴───────────┴──────────┴───────────────┘
                                            │
                                            ▼
                                ┌───────────────────────┐
                                │ Save to DynamoDB      │
                                │ (Audit Trail)         │
                                └───────────────────────┘
                                            │
                                            ▼
                                ┌───────────────────────┐
                                │ Return Decision       │
                                │ to User               │
                                └───────────────────────┘
```

---

## Data Storage & Retrieval

### DynamoDB Table: ClaimsAuditTrail

**Purpose**: Permanent audit trail of all claim decisions

**Schema**:
```
Table Name: ClaimsAuditTrail
Partition Key: ClaimId (String)
Global Secondary Index: PolicyNumber-index
  - Partition Key: PolicyNumber (String)
  - Sort Key: Timestamp (String)
```

**Attributes**:
| Attribute | Type | Description | Example |
|-----------|------|-------------|---------|
| `ClaimId` | String | Unique claim identifier | `"a7b3c4d5-e6f7-8g9h-0i1j-2k3l4m5n6o7p"` |
| `Timestamp` | String (ISO 8601) | Submission timestamp | `"2026-02-01T14:30:00.000Z"` |
| `PolicyNumber` | String | Policy identifier | `"AFLAC-HOSP-2024-001"` |
| `ClaimAmount` | Number | Claim dollar amount | `1500` |
| `ClaimDescription` | String | Claim details | `"Hospital confinement 3 days pneumonia"` |
| `DecisionStatus` | String | Final decision | `"Covered"`, `"Not Covered"`, `"Manual Review"` |
| `Explanation` | String | AI reasoning | `"Hospital Indemnity 500/day approved..."` |
| `ConfidenceScore` | Number | AI confidence | `0.96` |
| `ClauseReferences` | List | Policy clause IDs | `["AFLAC-HOSP-2.1", "AFLAC-HOSP-3.2"]` |
| `RequiredDocuments` | List | Documents needed | `["Hospital Records", "Discharge Summary"]` |
| `RetrievedClauses` | String (JSON) | RAG retrieval metadata | `"[{\"ClauseId\":\"...\", \"Score\":0.92}]"` |
| `SpecialistNotes` | String (optional) | Specialist override notes | `"Approved after reviewing medical records"` |
| `SpecialistId` | String (optional) | Reviewing specialist | `"SPEC-001"` |
| `ReviewedAt` | String (optional) | Review timestamp | `"2026-02-02T10:15:00.000Z"` |

### Data Access Patterns

**File**: `src/ClaimsRagBot.Infrastructure/DynamoDB/AuditService.cs`

#### 1. Save Claim Decision
```csharp
Task SaveAsync(ClaimRequest request, ClaimDecision decision, List<PolicyClause> clauses)
```
- **Operation**: `PutItem`
- **Use Case**: Store new claim validation result
- **Called By**: `ClaimValidationOrchestrator.ValidateClaimAsync`

#### 2. Get Claim by ID
```csharp
Task<ClaimAuditRecord?> GetByClaimIdAsync(string claimId)
```
- **Operation**: `GetItem`
- **Use Case**: Retrieve single claim for detail view
- **Called By**: `ClaimsController.GetClaimById`

#### 3. Get All Claims (with Optional Filter)
```csharp
Task<List<ClaimAuditRecord>> GetAllClaimsAsync(string? statusFilter = null)
```
- **Operation**: `Scan` (with FilterExpression if status provided)
- **Use Case**: Load claims dashboard
- **Called By**: `ClaimsController.GetAllClaims`

#### 4. Get Claims by Policy Number
```csharp
Task<List<ClaimAuditRecord>> GetByPolicyNumberAsync(string policyNumber)
```
- **Operation**: `Query` on GSI `PolicyNumberIndex`
- **Use Case**: Search all claims for a specific policy
- **Called By**: `ClaimsController.SearchByPolicyNumber`

#### 5. Update Claim Decision (Specialist Override)
```csharp
Task<bool> UpdateClaimDecisionAsync(string claimId, string newStatus, string specialistNotes, string specialistId)
```
- **Operation**: `UpdateItem`
- **Use Case**: Specialist overrides AI decision
- **Called By**: `ClaimsController.UpdateClaimDecision`

**Update Expression**:
```csharp
UpdateExpression = "SET DecisionStatus = :status, SpecialistNotes = :notes, SpecialistId = :specialistId, ReviewedAt = :reviewedAt"
```

---

## All Functional Flows

### 1. Role Selection Flow

**User Action**: User lands on application

**UI Component**: 
- File: `claims-chatbot-ui/src/app/components/role-selection/role-selection.component.ts`
- Route: `/role-selection`

**Flow**:
1. User sees two role cards: Claimant and Claims Specialist
2. User selects role
3. Router navigates:
   - **Claimant** → `/chat` (Submit Claims)
   - **Claims Specialist** → `/claims` (Dashboard)

**Code**:
```typescript
selectRole(role: 'claimant' | 'specialist'): void {
  if (role === 'claimant') {
    this.router.navigate(['/chat']);
  } else if (role === 'specialist') {
    this.router.navigate(['/claims']);
  }
}
```

**Navigation Bar**: Hidden on role selection page, visible after role chosen

---

### 2. Submit Claim via Chat

**User Action**: Claimant types claim in chat interface

**UI Component**: 
- File: `claims-chatbot-ui/src/app/components/chat/chat.component.ts`
- Route: `/chat`

**Flow**:
1. User types message: "I was hospitalized for 3 days due to pneumonia, claim $1500"
2. Frontend calls `validateClaim()` API
3. **[FULL VALIDATION FLOW AS DESCRIBED IN STEP 3]**
4. Result displayed in chat bubble
5. Claim stored in DynamoDB

**Files Involved**:
- Frontend: `chat.component.ts`, `claims-api.service.ts`
- Backend: `ClaimsController.cs`, `ClaimValidationOrchestrator.cs`
- AWS: Bedrock (embedding + LLM), OpenSearch, DynamoDB

---

### 3. Submit Claim via Form

**User Action**: Claimant fills structured form

**UI Component**: 
- File: `claims-chatbot-ui/src/app/components/claim-form/claim-form.component.ts`
- Route: `/chat` (embedded)

**Flow**:
1. User fills form:
   - Policy Number
   - Claim Type (dropdown)
   - Claim Amount
   - Description
2. Click "Submit Claim"
3. **[FULL VALIDATION FLOW AS DESCRIBED IN STEP 3]**
4. Result displayed
5. Claim stored in DynamoDB

**Same backend flow as chat submission**

---

### 4. Upload Claim Document

**User Action**: Claimant uploads PDF/image of claim

**UI Component**: 
- File: `claims-chatbot-ui/src/app/components/document-upload/document-upload.component.ts`

**Flow**:
1. User drags/drops file or clicks upload
2. Frontend calls `uploadDocument()` API
3. Backend uploads to S3
4. Textract extracts text
5. Comprehend extracts entities (amounts, dates, policy numbers)
6. Pre-filled claim form shown to user
7. User confirms and submits
8. **[FULL VALIDATION FLOW]**

**Files Involved**:
- Frontend: `document-upload.component.ts`, `claims-api.service.ts`
- Backend: `DocumentsController.cs`, `S3Service.cs`, `TextractService.cs`, `ComprehendService.cs`
- AWS: S3, Textract, Comprehend

**Code** (`DocumentsController.cs`):
```csharp
[HttpPost("submit")]
public async Task<ActionResult<SubmitDocumentResponse>> SubmitDocument(IFormFile file, string userId, DocumentType documentType)
{
    // Step 1: Upload to S3
    var uploadResult = await _s3Service.UploadDocumentAsync(file.OpenReadStream(), file.FileName, file.ContentType);
    
    // Step 2: Extract text with Textract
    var extractedText = await _textractService.ExtractTextAsync(uploadResult.BucketName, uploadResult.S3Key);
    
    // Step 3: Extract entities with Comprehend
    var entities = await _comprehendService.ExtractEntitiesAsync(extractedText);
    
    // Step 4: Build ClaimExtractionResult
    var extraction = new ClaimExtractionResult(
        PolicyNumber: entities.FirstOrDefault(e => e.Type == "POLICY_NUMBER")?.Text ?? "",
        ClaimAmount: ParseAmount(entities.FirstOrDefault(e => e.Type == "QUANTITY")?.Text),
        ClaimDescription: extractedText,
        ExtractedEntities: entities
    );
    
    return Ok(new SubmitDocumentResponse(uploadResult, extraction));
}
```

---

### 5. View Claims Dashboard

**User Action**: Claims Specialist wants to see all claims

**UI Component**: 
- File: `claims-chatbot-ui/src/app/components/claims-list/claims-list.component.ts`
- Route: `/claims`

**Flow**:
1. Specialist navigates to `/claims`
2. Frontend calls `getAllClaims()` API
3. Backend scans DynamoDB
4. Returns all claims sorted by timestamp (newest first)
5. UI displays in table:
   - Claim ID
   - Policy Number
   - Status badge (color-coded)
   - Explanation snippet
   - Specialist review indicator
6. Specialist can filter by status dropdown

**Backend Code** (`ClaimsController.cs`):
```csharp
[HttpGet("list")]
public async Task<ActionResult<List<ClaimAuditRecord>>> GetAllClaims([FromQuery] string? status = null)
{
    var claims = await _auditService.GetAllClaimsAsync(status);
    return Ok(claims);
}
```

**DynamoDB Operation** (`AuditService.cs`):
```csharp
public async Task<List<ClaimAuditRecord>> GetAllClaimsAsync(string? statusFilter = null)
{
    var request = new ScanRequest { TableName = "ClaimsAuditTrail" };
    
    if (!string.IsNullOrEmpty(statusFilter))
    {
        request.FilterExpression = "DecisionStatus = :status";
        request.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            [":status"] = new AttributeValue { S = statusFilter }
        };
    }
    
    var response = await _client.ScanAsync(request);
    return response.Items.Select(MapToClaimAuditRecord).OrderByDescending(c => c.Timestamp).ToList();
}
```

---

### 6. Filter Claims by Status

**User Action**: Specialist selects filter dropdown

**UI Component**: `claims-list.component.ts`

**Flow**:
1. Specialist selects status (All, Covered, Not Covered, Manual Review)
2. Frontend calls `getAllClaims(status)` API with status parameter
3. Backend filters DynamoDB results
4. UI updates table

**Frontend Code**:
```typescript
onFilterChange(status: string): void {
  this.selectedStatus = status;
  this.loadClaims();
}

loadClaims(): void {
  const statusParam = this.selectedStatus === 'All' ? undefined : this.selectedStatus;
  this.claimsApiService.getAllClaims(statusParam).subscribe(claims => {
    this.claims = claims;
  });
}
```

---

### 7. View Claim Details

**User Action**: Specialist clicks "View Details" on a claim

**UI Component**: 
- File: `claims-chatbot-ui/src/app/components/claim-detail/claim-detail.component.ts`
- Route: `/claims/:id`

**Flow**:
1. User clicks claim row
2. Router navigates to `/claims/{claimId}`
3. Frontend calls `getClaimById(claimId)` API
4. Backend queries DynamoDB by ClaimId
5. Returns full claim record
6. UI displays:
   - Claim information card
   - AI decision with explanation
   - Confidence score
   - Clause references
   - Required documents
   - Specialist review section (if reviewed)
   - Decision form (if not reviewed)

**Backend Code**:
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<ClaimAuditRecord>> GetClaimById(string id)
{
    var claim = await _auditService.GetByClaimIdAsync(id);
    
    if (claim == null)
    {
        return NotFound(new { error = $"Claim {id} not found" });
    }
    
    return Ok(claim);
}
```

**DynamoDB Operation**:
```csharp
public async Task<ClaimAuditRecord?> GetByClaimIdAsync(string claimId)
{
    var request = new GetItemRequest
    {
        TableName = "ClaimsAuditTrail",
        Key = new Dictionary<string, AttributeValue>
        {
            ["ClaimId"] = new AttributeValue { S = claimId }
        }
    };
    
    var response = await _client.GetItemAsync(request);
    
    if (response.Item == null || !response.Item.Any())
        return null;
    
    return MapToClaimAuditRecord(response.Item);
}
```

---

### 8. Specialist Override Decision

**User Action**: Specialist reviews claim and changes decision

**UI Component**: `claim-detail.component.ts`

**Flow**:
1. Specialist on claim detail page
2. Clicks "Make Decision" or "Update Decision"
3. Form appears:
   - Status dropdown (Covered, Not Covered, Manual Review)
   - Notes textarea
   - Specialist ID input
4. Specialist fills form and submits
5. Frontend calls `updateClaimDecision()` API
6. Backend updates DynamoDB record with:
   - New decision status
   - Specialist notes
   - Specialist ID
   - Review timestamp
7. Success message shown
8. Claim details refresh

**Frontend Code**:
```typescript
submitDecision(): void {
  this.claimsApiService
    .updateClaimDecision(this.claimId, this.decisionUpdate)
    .subscribe({
      next: () => {
        this.successMessage = 'Claim decision updated successfully!';
        this.loadClaimDetails();  // Refresh
      },
      error: (error) => {
        this.errorMessage = 'Failed to update claim decision';
      }
    });
}
```

**Backend Code**:
```csharp
[HttpPut("{id}/decision")]
public async Task<IActionResult> UpdateClaimDecision(string id, [FromBody] ClaimDecisionUpdate updateRequest)
{
    var success = await _auditService.UpdateClaimDecisionAsync(
        id,
        updateRequest.NewStatus,
        updateRequest.SpecialistNotes,
        updateRequest.SpecialistId
    );
    
    if (!success)
    {
        return NotFound(new { error = $"Claim {id} not found or update failed" });
    }
    
    return Ok(new { message = "Claim decision updated successfully" });
}
```

**DynamoDB Operation**:
```csharp
public async Task<bool> UpdateClaimDecisionAsync(string claimId, string newStatus, string specialistNotes, string specialistId)
{
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
    return true;
}
```

---

### 9. Search Claims by Policy Number

**User Action**: Specialist searches for all claims under a policy

**UI Component**: 
- File: `claims-chatbot-ui/src/app/components/claim-search/claim-search.component.ts`

**Flow**:
1. Specialist enters policy number in search box
2. Frontend calls `searchByPolicyNumber()` API
3. Backend queries DynamoDB GSI (PolicyNumberIndex)
4. Returns all claims for that policy
5. UI displays results in table

**Backend Code**:
```csharp
[HttpGet("search/policy/{policyNumber}")]
public async Task<ActionResult<List<ClaimAuditRecord>>> SearchByPolicyNumber(string policyNumber)
{
    var claims = await _auditService.GetByPolicyNumberAsync(policyNumber);
    return Ok(claims);
}
```

**DynamoDB Operation**:
```csharp
public async Task<List<ClaimAuditRecord>> GetByPolicyNumberAsync(string policyNumber)
{
    var request = new QueryRequest
    {
        TableName = "ClaimsAuditTrail",
        IndexName = "PolicyNumberIndex",  // GSI
        KeyConditionExpression = "PolicyNumber = :policyNumber",
        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            [":policyNumber"] = new AttributeValue { S = policyNumber }
        },
        ScanIndexForward = false  // Most recent first
    };
    
    var response = await _client.QueryAsync(request);
    return response.Items.Select(MapToClaimAuditRecord).ToList();
}
```

---

## Code File Reference Map

### Frontend (Angular)

| Component | File Path | Purpose |
|-----------|-----------|---------|
| **Role Selection** | `claims-chatbot-ui/src/app/components/role-selection/role-selection.component.ts` | Landing page for role selection |
| **Chat Interface** | `claims-chatbot-ui/src/app/components/chat/chat.component.ts` | Chat-based claim submission |
| **Claim Form** | `claims-chatbot-ui/src/app/components/claim-form/claim-form.component.ts` | Structured claim form |
| **Document Upload** | `claims-chatbot-ui/src/app/components/document-upload/document-upload.component.ts` | File upload with OCR |
| **Claims List** | `claims-chatbot-ui/src/app/components/claims-list/claims-list.component.ts` | Dashboard table view |
| **Claim Detail** | `claims-chatbot-ui/src/app/components/claim-detail/claim-detail.component.ts` | Single claim detail page |
| **Claim Search** | `claims-chatbot-ui/src/app/components/claim-search/claim-search.component.ts` | Policy number search |
| **Claim Result** | `claims-chatbot-ui/src/app/components/claim-result/claim-result.component.ts` | Display validation result |
| **API Service** | `claims-chatbot-ui/src/app/services/claims-api.service.ts` | HTTP client for backend API |
| **Data Models** | `claims-chatbot-ui/src/app/models/claim.model.ts` | TypeScript interfaces |
| **App Config** | `claims-chatbot-ui/src/app/app.config.ts` | Routing configuration |
| **App Component** | `claims-chatbot-ui/src/app/app.component.ts` | Root component with navigation |

### Backend (ASP.NET Core)

| Layer | File Path | Purpose |
|-------|-----------|---------|
| **Claims Controller** | `src/ClaimsRagBot.Api/Controllers/ClaimsController.cs` | REST API endpoints |
| **Documents Controller** | `src/ClaimsRagBot.Api/Controllers/DocumentsController.cs` | File upload endpoints |
| **Orchestrator** | `src/ClaimsRagBot.Application/RAG/ClaimValidationOrchestrator.cs` | Main validation logic |
| **Embedding Service** | `src/ClaimsRagBot.Infrastructure/Bedrock/EmbeddingService.cs` | Generate embeddings |
| **LLM Service** | `src/ClaimsRagBot.Infrastructure/Bedrock/LlmService.cs` | AI decision generation |
| **Retrieval Service** | `src/ClaimsRagBot.Infrastructure/OpenSearch/RetrievalService.cs` | Vector search |
| **Audit Service** | `src/ClaimsRagBot.Infrastructure/DynamoDB/AuditService.cs` | DynamoDB operations |
| **S3 Service** | `src/ClaimsRagBot.Infrastructure/S3/S3Service.cs` | Document storage |
| **Textract Service** | `src/ClaimsRagBot.Infrastructure/Textract/TextractService.cs` | OCR extraction |
| **Comprehend Service** | `src/ClaimsRagBot.Infrastructure/Comprehend/ComprehendService.cs` | NLP entity extraction |
| **Core Models** | `src/ClaimsRagBot.Core/Models/` | Domain models |
| **Core Interfaces** | `src/ClaimsRagBot.Core/Interfaces/` | Service contracts |

### Infrastructure

| Resource | File Path | Purpose |
|----------|-----------|---------|
| **CloudFormation** | `template.yaml` | AWS infrastructure as code |
| **App Settings** | `src/ClaimsRagBot.Api/appsettings.json` | Configuration |
| **Proxy Config** | `claims-chatbot-ui/proxy.conf.json` | Dev server proxy |

### AWS Services Used

| Service | Purpose | Primary Code File |
|---------|---------|-------------------|
| **Bedrock (Titan Embed)** | Generate embeddings | `EmbeddingService.cs` |
| **Bedrock (Claude 3.5)** | AI decision reasoning | `LlmService.cs` |
| **OpenSearch Serverless** | Vector search policy clauses | `RetrievalService.cs` |
| **DynamoDB** | Audit trail storage | `AuditService.cs` |
| **S3** | Document storage | `S3Service.cs` |
| **Textract** | OCR extraction | `TextractService.cs` |
| **Comprehend** | NLP entity extraction | `ComprehendService.cs` |

---

## Key Takeaways

### How Claims Are Processed (Summary)

1. **User Submits Claim** → Frontend sends ClaimRequest to API
2. **Generate Embedding** → AWS Bedrock Titan converts claim text to vector
3. **Retrieve Policy Clauses** → OpenSearch finds 5 most relevant clauses via kNN search
4. **AI Decision** → AWS Bedrock Claude analyzes claim against clauses, returns JSON decision
5. **Apply Business Rules** → C# code applies thresholds and exclusion logic
6. **Save to Audit Trail** → DynamoDB stores complete record
7. **Return to User** → Frontend displays decision

### Rules Engine Location

| Rule Type | Code Location | Decision Point |
|-----------|---------------|----------------|
| **Semantic Matching** | `LlmService.cs` (Bedrock Claude) | AI understands claim vs. policy |
| **Confidence Threshold** | `ClaimValidationOrchestrator.cs` | Score < 85% → Manual Review |
| **Amount Threshold** | `ClaimValidationOrchestrator.cs` | Amount > $5000 + Covered → Manual Review |
| **Exclusion Detection** | `ClaimValidationOrchestrator.cs` | Clause contains "Exclusion" → Manual Review |

### Data Storage & Access

| Operation | Method | DynamoDB Action | Use Case |
|-----------|--------|-----------------|----------|
| **Save Claim** | `SaveAsync()` | `PutItem` | Store new validation |
| **Get by ID** | `GetByClaimIdAsync()` | `GetItem` | View detail |
| **Get All** | `GetAllClaimsAsync()` | `Scan` | Dashboard list |
| **Filter by Status** | `GetAllClaimsAsync(status)` | `Scan + FilterExpression` | Filter dashboard |
| **Search by Policy** | `GetByPolicyNumberAsync()` | `Query on GSI` | Policy lookup |
| **Update Decision** | `UpdateClaimDecisionAsync()` | `UpdateItem` | Specialist override |

---

## End-to-End Flow Example

### Scenario: Hospital Confinement Claim

**User Input**:
```
Policy Number: AFLAC-HOSP-2024-001
Claim Amount: $1,500
Description: Hospital confinement for 3 days due to pneumonia
Policy Type: Health
```

**Processing Steps**:

1. **Embedding Generated** (Bedrock Titan):
   ```
   [0.0234, -0.0567, 0.1234, ..., 0.0891]  // 1536 dimensions
   ```

2. **OpenSearch Query** (Vector Search):
   ```json
   {
     "query": {
       "knn": { "embedding": { "vector": [...], "k": 5 } },
       "filter": { "policyType": "health" }
     }
   }
   ```

3. **Retrieved Clauses**:
   ```
   [AFLAC-HOSP-2.1] Hospital Indemnity: $500/day for confinement (Score: 0.92)
   [AFLAC-HOSP-3.2] Covered Conditions: pneumonia, cardiac, surgical (Score: 0.89)
   ```

4. **Claude AI Decision**:
   ```json
   {
     "status": "Covered",
     "explanation": "Hospital Indemnity pays $500/day × 3 days = $1500. Pneumonia is covered.",
     "clauseReferences": ["AFLAC-HOSP-2.1", "AFLAC-HOSP-3.2"],
     "requiredDocuments": ["Hospital Records", "Discharge Summary"],
     "confidenceScore": 0.96
   }
   ```

5. **Business Rules Applied**:
   - ✅ Confidence 0.96 > 0.85 threshold
   - ✅ Amount $1,500 < $5,000 threshold
   - ✅ No exclusion clauses
   - **Result**: APPROVED (no rule changes)

6. **Saved to DynamoDB**:
   ```
   ClaimId: a7b3c4d5-...
   Status: Covered
   Timestamp: 2026-02-01T14:30:00Z
   ```

7. **Returned to User**:
   ```
   ✅ COVERED
   "Hospital Indemnity pays $500/day × 3 days = $1500. Pneumonia is covered."
   Confidence: 96%
   ```

---

**End of Documentation**
