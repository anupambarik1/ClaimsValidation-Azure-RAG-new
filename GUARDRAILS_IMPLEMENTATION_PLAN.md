# Guardrails Implementation Plan for Claims RAG Bot
## Assessment of Current vs. Required Decision-Support Guardrails

**Version:** 1.0  
**Date:** February 19, 2026  
**Status:** Implementation Planning

---

## Executive Summary

This document analyzes the guardrails outlined in `CLAIMS_DECISION_SUPPORT_PROMPT.md` against your existing AWS/Azure infrastructure and codebase to determine:

1. ✅ **What's Already Implemented** - Features you already have
2. 🟡 **What Needs Enhancement** - Existing features requiring improvements
3. 🔴 **What's Missing** - New components required
4. 💡 **Implementation Recommendations** - How to implement using existing services

---

## Current Infrastructure Analysis

### ✅ Existing AWS Services (Already Configured)

| Service | Current Usage | Guardrail Support |
|---------|---------------|-------------------|
| **Amazon Bedrock** | Claude 3.5 Sonnet for LLM decisions | ✅ LLM-based validation |
| **Amazon Bedrock** | Titan Embeddings v1 | ✅ Evidence retrieval |
| **OpenSearch Serverless** | Vector database | ✅ Policy clause retrieval |
| **DynamoDB** | Audit trail storage | ✅ Audit logging |
| **S3** | Document storage | ✅ Evidence preservation |
| **Textract** | OCR for documents | ✅ Document extraction |
| **Comprehend** | NLP entity extraction | ✅ Data validation |
| **CloudWatch** | Logging & monitoring | ✅ Observability |

### ✅ Existing Azure Services (Already Configured)

| Service | Current Usage | Guardrail Support |
|---------|---------------|-------------------|
| **Azure OpenAI** | GPT-4 Turbo for LLM decisions | ✅ LLM-based validation |
| **Azure OpenAI** | text-embedding-ada-002 | ✅ Evidence retrieval |
| **Azure AI Search** | Vector database | ✅ Policy clause retrieval |
| **Cosmos DB** | Audit trail storage | ✅ Audit logging |
| **Blob Storage** | Document storage | ✅ Evidence preservation |
| **Document Intelligence** | OCR for documents | ✅ Document extraction |
| **Language Service** | NLP entity extraction | ✅ Data validation |
| **Application Insights** | Logging & monitoring | ✅ Observability |

---

## Guardrail-by-Guardrail Implementation Status

### 1. 🟡 **No Hallucinations** - PARTIALLY IMPLEMENTED

#### Current State:
- ✅ LLM system prompts instruct: "Use ONLY the provided policy clauses"
- ✅ Retrieval service pulls policy clauses from vector DB
- ✅ Prompt includes: "If unsure, say 'Needs Manual Review'"

**Evidence:**
```csharp
// LlmService.cs (Line 65-70)
system = @"You are an insurance claims validation assistant.
You MUST:
- Use ONLY the provided policy clauses
- Cite clause IDs
- If unsure, say 'Needs Manual Review'
- Respond in valid JSON format only"
```

#### ⚠️ Gaps:
- ❌ No post-processing validation to ensure citations are present
- ❌ No rejection mechanism if LLM response lacks evidence citations
- ❌ "Not provided" standardization not enforced

#### 💡 Implementation Needed:
```csharp
// New: ResponseValidationService.cs
public class ResponseValidationService
{
    public ValidationResult ValidateLlmResponse(ClaimDecision decision, List<PolicyClause> availableClauses)
    {
        var errors = new List<string>();
        
        // Enforce citation requirement
        if (!decision.ClauseReferences.Any())
        {
            errors.Add("LLM response missing required policy citations");
        }
        
        // Verify cited clauses actually exist
        foreach (var citation in decision.ClauseReferences)
        {
            if (!availableClauses.Any(c => c.ClauseId == citation))
            {
                errors.Add($"Cited clause '{citation}' not found in retrieved policy clauses");
            }
        }
        
        // Check for hallucination indicators
        if (decision.Explanation.Contains("I think") || 
            decision.Explanation.Contains("probably") ||
            decision.Explanation.Contains("generally"))
        {
            errors.Add("Response contains uncertainty language suggesting potential hallucination");
        }
        
        return new ValidationResult(errors.Count == 0, errors);
    }
}
```

**Services Needed:** ✅ **None - use existing code infrastructure**

---

### 2. ✅ **Evidence-First** - WELL IMPLEMENTED

#### Current State:
- ✅ RAG pipeline retrieves policy clauses with clause IDs
- ✅ ClaimDecision model includes `ClauseReferences` list
- ✅ Audit trail stores retrieved clauses: `await _auditService.SaveAsync(request, decision, clauses);`

**Evidence:**
```csharp
// ClaimValidationOrchestrator.cs (Line 29-35)
var embedding = await _embeddingService.GenerateEmbeddingAsync(request.ClaimDescription);
var clauses = await _retrievalService.RetrieveClausesAsync(embedding, request.PolicyType);
```

```csharp
// ClaimDecision.cs
public record ClaimDecision(
    string Status,
    string Explanation,
    List<string> ClauseReferences,  // ✅ Evidence citations
    List<string> RequiredDocuments,
    float ConfidenceScore
);
```

#### 💡 Enhancement Needed:
Add structured citation format in prompt:

```csharp
// Update LlmService.cs prompt
var enhancedPrompt = @"
CITATION FORMAT REQUIRED:
- Every statement must reference a clause: [ClauseID: policy_section_X]
- Example: 'This treatment is covered [ClauseID: life_policy_003]'
- If no clause supports a statement, do not make the statement
";
```

**Services Needed:** ✅ **None - enhance existing LlmService**

---

### 3. ✅ **No Hiding Critical Info** - IMPLEMENTED

#### Current State:
- ✅ Manual Review status triggers on low confidence
- ✅ Business rules check for contradictions (exclusion clauses)
- ✅ Missing document detection via `RequiredDocuments` list

**Evidence:**
```csharp
// ClaimValidationOrchestrator.cs (Line 36-48)
// Guardrail - if no clauses found, manual review required
if (!clauses.Any())
{
    return new ClaimDecision(
        Status: "Manual Review",
        Explanation: "No relevant policy clauses found for this claim type",
        RequiredDocuments: new List<string> { "Policy Document", "Claim Evidence" }
    );
}
```

```csharp
// Business rule for exclusions (Line 188-196)
if (decision.ClauseReferences.Any(c => c.Contains("Exclusion", StringComparison.OrdinalIgnoreCase)))
{
    return decision with
    {
        Status = decision.Status == "Covered" ? "Manual Review" : decision.Status,
        Explanation = "Potential exclusion clause detected. " + decision.Explanation
    };
}
```

#### 💡 Enhancement Needed:
Add structured contradictions field to ClaimDecision:

```csharp
// Enhanced ClaimDecision model
public record ClaimDecision(
    string Status,
    string Explanation,
    List<string> ClauseReferences,
    List<string> RequiredDocuments,
    float ConfidenceScore,
    List<Contradiction> Contradictions = null,  // NEW
    List<string> MissingEvidence = null  // NEW
);

public record Contradiction(
    string SourceA,
    string SourceB,
    string Description,
    string Impact
);
```

**Services Needed:** ✅ **None - model enhancement only**

---

### 4. ✅ **Uncertainty & Escalation** - WELL IMPLEMENTED

#### Current State:
- ✅ Confidence scoring in ClaimDecision model
- ✅ Business rule: confidence < 0.85 → Manual Review
- ✅ High-value claims (>$5000) → Manual Review

**Evidence:**
```csharp
// ClaimValidationOrchestrator.cs (Line 149-157)
const float confidenceThreshold = 0.85f;

// Rule 1: Low confidence → Manual Review
if (decision.ConfidenceScore < confidenceThreshold)
{
    return decision with
    {
        Status = "Manual Review",
        Explanation = $"Confidence below threshold ({decision.ConfidenceScore:F2} < {confidenceThreshold}). " + decision.Explanation
    };
}
```

```csharp
// Rule 4: High amount + covered → Manual Review
if (request.ClaimAmount > autoApprovalThreshold && decision.Status == "Covered")
{
    return decision with
    {
        Status = "Manual Review",
        Explanation = $"Amount ${request.ClaimAmount} exceeds auto-approval limit..."
    };
}
```

#### 💡 Enhancement Needed:
Add "what's missing to increase confidence" field:

```csharp
public record ClaimDecision(
    // ... existing fields
    List<string> ConfidenceGaps = null  // NEW: What would resolve uncertainty
);

// In orchestrator
if (decision.ConfidenceScore < confidenceThreshold)
{
    var gaps = new List<string>();
    if (!supportingDocuments.Any()) gaps.Add("Upload medical records");
    if (string.IsNullOrEmpty(request.DiagnosisCode)) gaps.Add("Provide ICD-10 diagnosis code");
    
    return decision with
    {
        Status = "Manual Review",
        ConfidenceGaps = gaps
    };
}
```

**Services Needed:** ✅ **None - code-only enhancement**

---

### 5. ✅ **No Policy Invention** - IMPLEMENTED

#### Current State:
- ✅ Policies ingested into vector DB via `PolicyIngestionService`
- ✅ RAG retrieves only indexed policy clauses
- ✅ LLM instructed: "Use ONLY the provided policy clauses"

**Evidence:**
```csharp
// RetrievalService retrieves from OpenSearch/AI Search
var clauses = await _retrievalService.RetrieveClausesAsync(embedding, request.PolicyType);

// PolicyClause model ensures structured policy data
public record PolicyClause(
    string ClauseId,
    string PolicyType,
    string Content,
    Dictionary<string, object>? Metadata
);
```

#### ⚠️ Gap:
- ❌ No explicit check if policy type requested exists in database

#### 💡 Implementation Needed:
```csharp
// Add to RetrievalService
public async Task<bool> PolicyTypeExistsAsync(string policyType)
{
    // Query vector DB for policy type existence
    var query = new
    {
        size = 1,
        query = new
        {
            term = new { policy_type = policyType }
        }
    };
    
    var result = await SearchAsync(query);
    return result.Hits.Any();
}

// Use in orchestrator
if (!await _retrievalService.PolicyTypeExistsAsync(request.PolicyType))
{
    return new ClaimDecision(
        Status: "Error",
        Explanation: $"Policy type '{request.PolicyType}' not found. Available types: Health, Life, Dental",
        ConfidenceScore: 0.0f
    );
}
```

**Services Needed:** ✅ **Use existing OpenSearch/AI Search**

---

### 6. ✅ **Privacy** - PARTIALLY IMPLEMENTED

#### Current State:
- ✅ Document storage in S3/Blob Storage with access controls
- ✅ Audit trail in DynamoDB/Cosmos DB
- ⚠️ PII masking: **NOT IMPLEMENTED**

**Evidence:**
```csharp
// Audit service stores full claim data
await _auditService.SaveAsync(request, decision, clauses);

// S3/Blob storage handles document encryption at rest
await _documentUploadService.UploadAsync(file);
```

#### ⚠️ Gaps:
- ❌ No PII masking in logs/responses
- ❌ No field-level encryption for sensitive data
- ❌ Member ID not masked to last 4 digits

#### 💡 Implementation Needed:
```csharp
// New: PiiMaskingService.cs
public class PiiMaskingService
{
    public string MaskMemberId(string memberId)
    {
        return memberId.Length > 4 
            ? $"****{memberId.Substring(memberId.Length - 4)}"
            : "****";
    }
    
    public string MaskSsn(string ssn)
    {
        return "***-**-****";
    }
    
    public string RedactPii(string text)
    {
        // Redact SSN patterns
        text = Regex.Replace(text, @"\d{3}-\d{2}-\d{4}", "***-**-****");
        
        // Redact phone numbers
        text = Regex.Replace(text, @"\d{3}-\d{3}-\d{4}", "***-***-****");
        
        // Redact emails
        text = Regex.Replace(text, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", "***@***.***");
        
        return text;
    }
}

// Use in API responses
public async Task<IActionResult> ValidateClaim([FromBody] ClaimRequest request)
{
    var maskedRequest = new ClaimRequest
    {
        MemberId = _piiMaskingService.MaskMemberId(request.MemberId),
        ClaimDescription = _piiMaskingService.RedactPii(request.ClaimDescription),
        // ... other fields
    };
    
    var decision = await _orchestrator.ValidateClaimAsync(request); // Full data
    
    // Return masked version to UI
    return Ok(new { 
        decision, 
        maskedMemberId = maskedRequest.MemberId 
    });
}
```

**Services Needed:** ✅ **None - use AWS KMS / Azure Key Vault for encryption (already available)**

---

### 7. ✅ **Security (Prompt Injection Protection)** - PARTIALLY IMPLEMENTED

#### Current State:
- ✅ Input validation in controllers
- ✅ File type/size validation for uploads
- ⚠️ **No prompt injection detection**

**Evidence:**
```csharp
// DocumentsController.cs has basic validation
if (file == null || file.Length == 0)
    return BadRequest("No file uploaded");

if (file.Length > 10 * 1024 * 1024) // 10MB
    return BadRequest("File too large");
```

#### ⚠️ Gaps:
- ❌ No detection of malicious prompts in claim descriptions
- ❌ No sanitization of user input before LLM processing
- ❌ No rate limiting on API endpoints

#### 💡 Implementation Needed:
```csharp
// New: PromptInjectionDetector.cs
public class PromptInjectionDetector
{
    private static readonly List<string> DangerousPatterns = new()
    {
        "ignore previous instructions",
        "ignore all previous",
        "disregard all",
        "you are now",
        "forget everything",
        "new instructions:",
        "system:",
        "admin mode",
        "<script>",
        "eval(",
        "execute("
    };
    
    public (bool IsClean, List<string> Threats) ScanInput(string input)
    {
        var threats = new List<string>();
        var normalized = input.ToLowerInvariant();
        
        foreach (var pattern in DangerousPatterns)
        {
            if (normalized.Contains(pattern))
            {
                threats.Add($"Detected suspicious pattern: '{pattern}'");
            }
        }
        
        // Check for unusual unicode characters
        if (Regex.IsMatch(input, @"[\u200B-\u200D\uFEFF]"))
        {
            threats.Add("Contains hidden unicode characters");
        }
        
        return (threats.Count == 0, threats);
    }
}

// Use in controller
[HttpPost("validate")]
public async Task<IActionResult> ValidateClaim([FromBody] ClaimRequest request)
{
    var (isClean, threats) = _promptDetector.ScanInput(request.ClaimDescription);
    
    if (!isClean)
    {
        _logger.LogWarning($"Potential prompt injection detected: {string.Join(", ", threats)}");
        return BadRequest(new { error = "Invalid input detected", threats });
    }
    
    // Continue with validation...
}
```

**AWS Service:** ✅ **Use AWS WAF (Web Application Firewall)** - can add as API Gateway layer  
**Azure Service:** ✅ **Use Azure Front Door + WAF** - already available

---

### 8. ✅ **Format (Structured Output)** - WELL IMPLEMENTED

#### Current State:
- ✅ Strongly-typed `ClaimDecision` model (C# records)
- ✅ JSON serialization enforced
- ✅ LLM instructed to return JSON

**Evidence:**
```csharp
public record ClaimDecision(
    string Status,
    string Explanation,
    List<string> ClauseReferences,
    List<string> RequiredDocuments,
    float ConfidenceScore
);

// LLM prompt includes:
"Respond in valid JSON format only"
```

#### 💡 Enhancement Needed:
Expand model to match full decision-support template:

```csharp
// Enhanced model matching CLAIMS_DECISION_SUPPORT_PROMPT.md
public record EnhancedClaimDecision(
    string ClaimId,
    string Status,
    string Explanation,
    float ConfidenceScore,
    string ConfidenceRationale,
    
    // Evidence section
    List<EvidenceItem> KeyEvidence,
    
    // Validation checks
    List<ValidationCheck> ValidationChecks,
    
    // Risk assessment
    List<Contradiction> Contradictions,
    List<string> MissingInfo,
    List<PolicyAlignment> PolicyAlignments,
    List<RiskIndicator> Risks,
    
    // Actions
    List<NextAction> NextActions,
    List<string> WhatCouldChange,
    
    // Audit
    AuditTrail AuditInfo
);

public record EvidenceItem(string Description, string Citation, string Detail);
public record ValidationCheck(string CheckName, string Result, string Citation, string Notes);
public record PolicyAlignment(string Criterion, string PolicyCitation, string Evidence, string Assessment);
public record RiskIndicator(string Type, string Description, string Severity);
public record NextAction(string Role, string Action, string ExpectedOutcome, string Timeline);
```

**Services Needed:** ✅ **None - model expansion only**

---

## 🔴 Missing Components (New Implementation Required)

### 1. **Citation Validation Engine**
**Status:** Not implemented  
**Priority:** HIGH  
**Complexity:** Low  
**Services Needed:** None - pure code logic

### 2. **PII Masking Service**
**Status:** Not implemented  
**Priority:** HIGH (Compliance requirement)  
**Complexity:** Low  
**Services Needed:** AWS KMS or Azure Key Vault (already available)

### 3. **Prompt Injection Detector**
**Status:** Not implemented  
**Priority:** HIGH (Security requirement)  
**Complexity:** Medium  
**Services Needed:** AWS WAF or Azure Front Door (available but not configured)

### 4. **Rate Limiting Middleware**
**Status:** Not implemented  
**Priority:** MEDIUM  
**Complexity:** Low  
**Services Needed:** Use built-in ASP.NET Core rate limiting (available in .NET 8)

### 5. **Enhanced Structured Output**
**Status:** Partially implemented  
**Priority:** MEDIUM  
**Complexity:** Medium  
**Services Needed:** None - prompt engineering + model expansion

### 6. **Contradiction Detection Logic**
**Status:** Basic implementation (exclusion clause check)  
**Priority:** MEDIUM  
**Complexity:** Medium  
**Services Needed:** AWS Comprehend or Azure Language Service (already configured)

---

## Implementation Roadmap

### Phase 1: High-Priority Security & Compliance (Week 1)
**No new AWS/Azure services needed**

| Task | Effort | Files to Create/Modify |
|------|--------|------------------------|
| PII Masking Service | 1 day | `Security/PiiMaskingService.cs` |
| Prompt Injection Detector | 1 day | `Security/PromptInjectionDetector.cs` |
| Citation Validator | 1 day | `Validation/CitationValidator.cs` |
| Rate Limiting | 0.5 day | `Program.cs` (middleware) |
| Input Sanitization | 0.5 day | `Controllers/*` |

**Total:** 4 days

### Phase 2: Enhanced Decision Support (Week 2)
**No new AWS/Azure services needed**

| Task | Effort | Files to Create/Modify |
|------|--------|------------------------|
| Expand ClaimDecision model | 1 day | `Core/Models/EnhancedClaimDecision.cs` |
| Update LLM prompts | 1 day | `Infrastructure/Bedrock/LlmService.cs`, `Infrastructure/Azure/AzureLlmService.cs` |
| Add contradiction detection | 2 days | `Application/RAG/ContradictionDetector.cs` |
| Confidence gap analysis | 1 day | `Application/RAG/ConfidenceAnalyzer.cs` |

**Total:** 5 days

### Phase 3: Observability & Monitoring (Week 3)
**Use existing CloudWatch / Application Insights**

| Task | Effort | Files to Create/Modify |
|------|--------|------------------------|
| Enhanced audit logging | 1 day | `Infrastructure/DynamoDB/AuditService.cs` |
| Guardrail metrics | 1 day | `Monitoring/GuardrailMetrics.cs` |
| Alert configuration | 0.5 day | CloudWatch Alarms / Azure Alerts |
| Dashboard creation | 0.5 day | CloudWatch Dashboard / Azure Dashboard |

**Total:** 3 days

---

## Configuration Changes Required

### 1. Enable .NET 8 Rate Limiting (No new service)

```csharp
// Program.cs additions
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));
});

var app = builder.Build();
app.UseRateLimiter();
```

### 2. Configure AWS WAF (Optional - adds cost)

```bash
# Using AWS CLI
aws wafv2 create-web-acl \
  --name claims-bot-waf \
  --scope REGIONAL \
  --default-action Allow={} \
  --rules file://waf-rules.json \
  --region us-east-1

# Associate with API Gateway
aws wafv2 associate-web-acl \
  --web-acl-arn arn:aws:wafv2:us-east-1:ACCOUNT:webacl/claims-bot-waf/ID \
  --resource-arn arn:aws:apigateway:us-east-1::/restapis/API_ID/stages/prod
```

**Cost:** ~$5/month base + $1 per million requests

### 3. Azure Front Door + WAF (Optional - adds cost)

```powershell
# Using Azure CLI
az network front-door waf-policy create `
  --resource-group claims-rg `
  --name claimsbotWafPolicy `
  --mode Prevention

# Associate with Front Door
az network front-door update `
  --name claims-bot-fd `
  --resource-group claims-rg `
  --set frontendEndpoints[0].webApplicationFirewallPolicyLink.id=/subscriptions/SUB_ID/resourceGroups/claims-rg/providers/Microsoft.Network/frontDoorWebApplicationFirewallPolicies/claimsbotWafPolicy
```

**Cost:** ~$35/month for Front Door + WAF

---

## Summary: Do You Need New Services?

### ✅ **NO NEW AWS/AZURE SERVICES REQUIRED** for core guardrails

All required guardrails can be implemented using:
1. ✅ **Existing AWS/Azure services** (already configured)
2. ✅ **Built-in .NET 8 features** (rate limiting, validation)
3. ✅ **Custom C# code** (PII masking, prompt detection, citation validation)

### 🟡 **OPTIONAL ENHANCEMENTS** (add cost but improve security)

| Service | Purpose | Cost Impact | Priority |
|---------|---------|-------------|----------|
| **AWS WAF** | Advanced threat protection | ~$5-10/month | Medium |
| **Azure Front Door + WAF** | DDoS protection, geo-filtering | ~$35/month | Medium |
| **AWS GuardDuty** | Threat detection | ~$4/month | Low |
| **Azure Sentinel** | SIEM for compliance | ~$200+/month | Low |

### 💡 **Recommended Approach**

**Phase 1 (Immediate):** Implement all code-based guardrails using existing services
- Zero additional cost
- 2-3 weeks development time
- Covers 90% of security requirements

**Phase 2 (Future):** Add WAF if production traffic justifies cost
- Evaluate after 3 months of production use
- Add if serving >1M requests/month
- Defer until compliance audit requires it

---

## Code Structure Recommendations

```
src/
├── ClaimsRagBot.Core/
│   ├── Models/
│   │   ├── EnhancedClaimDecision.cs  # NEW
│   │   └── ValidationResult.cs        # NEW
│   └── Interfaces/
│       ├── IPiiMaskingService.cs      # NEW
│       └── IPromptInjectionDetector.cs # NEW
│
├── ClaimsRagBot.Application/
│   ├── RAG/
│   │   ├── ClaimValidationOrchestrator.cs  # MODIFY
│   │   ├── CitationValidator.cs             # NEW
│   │   ├── ContradictionDetector.cs         # NEW
│   │   └── ConfidenceAnalyzer.cs            # NEW
│   └── Security/
│       ├── PiiMaskingService.cs             # NEW
│       └── PromptInjectionDetector.cs       # NEW
│
└── ClaimsRagBot.Infrastructure/
    ├── Bedrock/
    │   └── LlmService.cs                    # MODIFY (enhanced prompts)
    └── Azure/
        └── AzureLlmService.cs               # MODIFY (enhanced prompts)
```

---

## Next Steps

1. **Review this plan** with your team
2. **Decide on optional services** (WAF, Front Door)
3. **Start Phase 1 implementation** (4 days, no new services)
4. **Test guardrails** with sample claims
5. **Deploy incrementally** to production

---

## Questions to Consider

1. **Compliance:** Do you need to meet HIPAA, SOC 2, or other specific standards?
   - If yes: May need Azure Sentinel or AWS GuardDuty
   - If no: Code-based guardrails are sufficient

2. **Scale:** Expected request volume?
   - <10K/month: No WAF needed
   - >100K/month: Consider WAF for DDoS protection

3. **Budget:** Acceptable monthly cost for security?
   - $0: Use code-based guardrails only
   - $50-100: Add WAF
   - $500+: Full security stack (WAF, SIEM, GuardDuty)

4. **Timeline:** When do you need to go live?
   - <2 weeks: Implement Phase 1 only
   - 1 month: Implement Phase 1 + Phase 2
   - 2+ months: Full implementation + optional services

---

**Conclusion:** Your existing AWS/Azure infrastructure is **sufficient** to implement all critical decision-support guardrails. No new services are required for the MVP. Focus on code-based implementations first, then evaluate paid security services after production deployment.
