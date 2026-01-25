using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Application.RAG;

public class ClaimValidationOrchestrator
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IRetrievalService _retrievalService;
    private readonly ILlmService _llmService;
    private readonly IAuditService _auditService;

    public ClaimValidationOrchestrator(
        IEmbeddingService embeddingService,
        IRetrievalService retrievalService,
        ILlmService llmService,
        IAuditService auditService)
    {
        _embeddingService = embeddingService;
        _retrievalService = retrievalService;
        _llmService = llmService;
        _auditService = auditService;
    }

    public async Task<ClaimDecision> ValidateClaimAsync(ClaimRequest request)
    {
        // Step 1: Generate embedding for claim description
        var embedding = await _embeddingService.GenerateEmbeddingAsync(request.ClaimDescription);

        // Step 2: Retrieve relevant policy clauses
        var clauses = await _retrievalService.RetrieveClausesAsync(embedding, request.PolicyType);

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

        // Step 4: Generate decision using LLM
        var decision = await _llmService.GenerateDecisionAsync(request, clauses);

        // Step 5: Apply Aflac-style business rules
        decision = ApplyBusinessRules(decision, request);

        // Step 6: Audit trail (mandatory for compliance)
        await _auditService.SaveAsync(request, decision, clauses);

        return decision;
    }

    private ClaimDecision ApplyBusinessRules(ClaimDecision decision, ClaimRequest request)
    {
        // Aflac-style decision rules
        const decimal autoApprovalThreshold = 5000m;
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

        // Rule 2: High amount + covered → Manual Review (even with high confidence)
        if (request.ClaimAmount > autoApprovalThreshold && decision.Status == "Covered")
        {
            return decision with
            {
                Status = "Manual Review",
                Explanation = $"Amount ${request.ClaimAmount} exceeds auto-approval limit. " + decision.Explanation
            };
        }

        // Rule 3: Exclusion clause detected → Deny or Manual Review
        if (decision.ClauseReferences.Any(c => c.Contains("Exclusion", StringComparison.OrdinalIgnoreCase)))
        {
            return decision with
            {
                Status = decision.Status == "Covered" ? "Manual Review" : decision.Status,
                Explanation = "Potential exclusion clause detected. " + decision.Explanation
            };
        }

        return decision;
    }
}
