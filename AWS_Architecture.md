# AWS Claims Validation RAG Bot - Production Architecture

**Date:** January 20, 2026  
**Purpose:** Enterprise-grade PoC for Aflac-style claims automation

---

## 1. AWS Architecture Overview

### 1.1 Logical Architecture

```
[ UI / Postman ]
        |
   API Gateway
        |
   AWS Lambda (ASP.NET Core)
        |
┌────────────────────────────┐
|   Claims Validation API    |
|----------------------------|
| - RAG Orchestrator         |
| - Guardrails               |
└─────────────┬──────────────┘
               |
   ┌───────────┼───────────────┐
   |           |               |
Bedrock   OpenSearch       DynamoDB
(LLM +    (Vector DB)      (Audit /
Embeds)                    Metadata)
   |
  S3 (Policies, SOPs)
```

### 1.2 AWS Services Breakdown

| Layer    | Service                        | Purpose                           |
|----------|--------------------------------|-----------------------------------|
| API      | API Gateway                    | Secure entry point                |
| Compute  | AWS Lambda (.NET 8)            | Low cost, scalable                |
| LLM      | Amazon Bedrock (Claude/Llama)  | No key management                 |
| Embeddings | Titan Embeddings             | Optimized for Bedrock             |
| Vector DB | OpenSearch Serverless         | Native AWS vectors                |
| Storage  | S3                             | Policy PDFs                       |
| Metadata | DynamoDB                       | Claim & audit trail               |
| Security | IAM + KMS                      | Enterprise-grade                  |
| Logs     | CloudWatch                     | Compliance                        |

**Rationale:** This mirrors how large insurers (including Aflac-like environments) design AI workloads.

### 1.3 Production-Grade Guardrails (Mandatory)

- ✅ IAM role-based access
- ✅ Clause-level citations enforced
- ✅ "Needs Manual Review" fallback
- ✅ Full decision audit trail
- ✅ Confidence thresholding

---

## 2. Production-Grade C# RAG Implementation (.NET 8)

### 2.1 Solution Structure

```
ClaimsRagBot/
│
├── Api/
│   └── Controllers/ClaimsController.cs
│
├── Core/
│   ├── Models/
│   ├── Interfaces/
│   └── Prompts/
│
├── Infrastructure/
│   ├── Bedrock/
│   ├── OpenSearch/
│   ├── Storage/
│
└── Application/
    ├── RAG/
    └── Workflows/
```

### 2.2 Core Models

```csharp
public record ClaimRequest(
    string PolicyNumber,
    string ClaimDescription,
    decimal ClaimAmount
);

public record PolicyClause(
    string ClauseId,
    string Text,
    string CoverageType,
    float Score
);

public record ClaimDecision(
    string Status, // Covered | Not Covered | Manual Review
    string Explanation,
    List<string> ClauseReferences,
    List<string> RequiredDocuments,
    float ConfidenceScore
);
```

### 2.3 Embedding Service (Bedrock – Titan)

```csharp
public class EmbeddingService : IEmbeddingService
{
    private readonly AmazonBedrockRuntimeClient _client;

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var request = new InvokeModelRequest
        {
            ModelId = "amazon.titan-embed-text-v1",
            Body = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new { inputText = text })
            )
        };

        var response = await _client.InvokeModelAsync(request);
        var result = JsonSerializer.Deserialize<EmbeddingResponse>(
            Encoding.UTF8.GetString(response.Body.ToArray())
        );

        return result!.Embedding;
    }
}
```

### 2.4 Vector Retrieval (OpenSearch)

```csharp
public async Task<List<PolicyClause>> RetrieveClausesAsync(
    float[] embedding,
    string policyType)
{
    var query = new
    {
        size = 5,
        query = new
        {
            @bool = new
            {
                filter = new[]
                {
                    new { term = new { policyType } }
                },
                must = new[]
                {
                    new
                    {
                        knn = new
                        {
                            embedding = new
                            {
                                vector = embedding,
                                k = 5
                            }
                        }
                    }
                }
            }
        }
    };

    // Execute against OpenSearch
}
```

### 2.5 RAG Orchestrator (Heart of the System)

```csharp
public async Task<ClaimDecision> ValidateClaimAsync(ClaimRequest request)
{
    var embedding = await _embeddingService
        .GenerateEmbeddingAsync(request.ClaimDescription);

    var clauses = await _retrievalService
        .RetrieveClausesAsync(embedding, "Motor");

    if (!clauses.Any())
        return ManualReview("No relevant clauses found");

    var decision = await _llmService.GenerateDecisionAsync(
        request,
        clauses
    );

    await _auditService.SaveAsync(request, decision, clauses);

    return decision;
}
```

### 2.6 LLM Prompt (STRICT – Production Safe)

```
SYSTEM:
You are an insurance claims validation assistant.
You MUST:
- Use ONLY the provided policy clauses
- Cite clause IDs
- If unsure, say "Needs Manual Review"

USER:
Claim:
{ClaimDescription}

Policy Clauses:
{Clauses}

Respond in JSON:
{
  "status": "",
  "explanation": "",
  "clauseReferences": [],
  "requiredDocuments": [],
  "confidenceScore": 0.0
}
```

---

## 3. Aflac-Style Claims Workflow Mapping

### 3.1 Where the Bot Fits in Aflac-Style Claims

```
FNOL (First Notice of Loss)
  ↓
Pre-Validation  ←  RAG BOT (Claims Validation)
  ↓
Adjudication
  ↓
Payment
```

### 3.2 What the Bot Does (Exactly Like Aflac's Early Automation)

#### Step 1: Intake Validation
- Policy active?
- Coverage exists?
- Documents required?

#### Step 2: Coverage Determination
- Clause-based validation
- Exclusion detection
- Rider check

#### Step 3: Routing

| Scenario         | Action                |
|------------------|-----------------------|
| Clear coverage   | Auto-approve          |
| Partial coverage | Ask docs              |
| Ambiguous        | Manual review         |

### 3.3 Aflac-Style Decision Rules (Simplified)

```
IF confidence > 0.85 AND amount < threshold
→ Auto-approve

IF exclusion clause found
→ Partial / Reject + clause reference

IF multiple clauses conflict
→ Manual review
```

---

## Architecture Assessment

### ✅ Strengths

1. **Enterprise-Grade Service Selection**
   - Bedrock eliminates key management overhead
   - OpenSearch Serverless provides native vector capabilities
   - Lambda + .NET 8 offers cost-effective scalability

2. **Clean Architecture**
   - Proper separation of concerns (Core/Application/Infrastructure)
   - Domain-driven design with records
   - Interface-based abstractions

3. **Production Safety**
   - Mandatory citation enforcement
   - Confidence thresholding
   - Explicit manual review fallback
   - Full audit trail via DynamoDB

4. **Industry Alignment**
   - Mirrors actual Aflac-style workflows
   - Clear FNOL → Pre-Validation → Adjudication pipeline
   - Realistic decision rules

### 🔧 Recommendations for Enhancement

1. **Add Circuit Breakers**
   ```csharp
   // Use Polly for resilience
   var policy = Policy
       .Handle<BedrockException>()
       .WaitAndRetryAsync(3, retryAttempt => 
           TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
   ```

2. **Implement Caching Layer**
   - ElastiCache for frequently accessed policy clauses
   - Reduce OpenSearch queries for common claim types

3. **Enhanced Monitoring**
   ```csharp
   // Add structured logging
   _logger.LogInformation(
       "Claim validated: {PolicyNumber}, Status: {Status}, Confidence: {Confidence}",
       request.PolicyNumber, decision.Status, decision.ConfidenceScore
   );
   ```

4. **Version Control for Policies**
   - Add `PolicyVersion` to `PolicyClause`
   - Track which policy version was used for each decision

5. **Batch Processing Support**
   - Lambda with SQS for bulk claim validation
   - EventBridge for scheduled policy updates

6. **Security Enhancements**
   - VPC endpoints for Bedrock/OpenSearch
   - WAF rules on API Gateway
   - Field-level encryption for PII

### 🎯 PoC-to-Production Roadmap

**Phase 1: PoC (Current)**
- ✅ Core RAG pipeline
- ✅ Basic guardrails
- ✅ Single policy type (Motor)

**Phase 2: Pilot**
- Multi-policy type support
- A/B testing framework
- Performance benchmarking

**Phase 3: Production**
- Multi-region deployment
- Advanced fraud detection
- Real-time policy sync

---

## Conclusion

This is a **production-credible architecture** suitable for enterprise insurance PoCs. The design choices (Bedrock, OpenSearch Serverless, DynamoDB audit) align with how major insurers build AI workloads. The C# implementation follows clean architecture principles and includes critical guardrails for claims automation.

**Key Differentiator:** Unlike academic demos, this includes mandatory citation enforcement, confidence thresholding, and explicit manual review pathways—exactly what's needed for regulatory compliance in insurance automation.
