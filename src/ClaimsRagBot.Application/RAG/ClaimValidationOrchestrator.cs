using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Application.RAG;

public class ClaimValidationOrchestrator
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IRetrievalService _retrievalService;
    private readonly ILlmService _llmService;
    private readonly IAuditService _auditService;
    private readonly IDocumentExtractionService? _documentExtractionService;
    private readonly ICitationValidator _citationValidator;
    private readonly IContradictionDetector _contradictionDetector;

    public ClaimValidationOrchestrator(
        IEmbeddingService embeddingService,
        IRetrievalService retrievalService,
        ILlmService llmService,
        IAuditService auditService,
        IDocumentExtractionService? documentExtractionService,
        ICitationValidator citationValidator,
        IContradictionDetector contradictionDetector)
    {
        _embeddingService = embeddingService;
        _retrievalService = retrievalService;
        _llmService = llmService;
        _auditService = auditService;
        _documentExtractionService = documentExtractionService;
        _citationValidator = citationValidator;
        _contradictionDetector = contradictionDetector;
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

        // Step 5: GUARDRAIL - Validate citations to prevent hallucinations
        var citationValidation = _citationValidator.ValidateLlmResponse(decision, clauses);
        if (!citationValidation.IsValid)
        {
            Console.WriteLine($"[Guardrail] Citation validation failed: {string.Join(", ", citationValidation.Errors)}");
            
            return new ClaimDecision(
                Status: "Manual Review",
                Explanation: "AI response failed citation validation. " + string.Join(" ", citationValidation.Errors),
                ClauseReferences: decision.ClauseReferences,
                RequiredDocuments: decision.RequiredDocuments,
                ConfidenceScore: 0.0f,
                ValidationWarnings: citationValidation.Errors,
                ConfidenceRationale: "Citation validation failed - potential hallucination detected"
            );
        }

        // Step 6: GUARDRAIL - Detect contradictions
        var contradictions = _contradictionDetector.DetectContradictions(request, decision, clauses);
        if (_contradictionDetector.HasCriticalContradictions(contradictions))
        {
            Console.WriteLine($"[Guardrail] Critical contradictions detected: {contradictions.Count}");
            
            decision = decision with
            {
                Status = "Manual Review",
                Explanation = "Critical contradictions detected. " + decision.Explanation,
                Contradictions = contradictions,
                ValidationWarnings = _contradictionDetector.GetContradictionSummary(contradictions)
            };
        }
        else if (contradictions.Any())
        {
            decision = decision with
            {
                Contradictions = contradictions,
                ValidationWarnings = _contradictionDetector.GetContradictionSummary(contradictions)
            };
        }

        // Step 7: Add citation warnings if present
        if (citationValidation.HasWarnings)
        {
            var warnings = decision.ValidationWarnings?.ToList() ?? new List<string>();
            warnings.AddRange(citationValidation.Warnings ?? new List<string>());
            decision = decision with { ValidationWarnings = warnings };
        }

        // Step 8: Apply Aflac-style business rules
        decision = ApplyBusinessRules(decision, request);

        // Step 9: Audit trail (mandatory for compliance)
        await _auditService.SaveAsync(request, decision, clauses);

        return decision;
    }

    /// <summary>
    /// Validates a claim with supporting documents for holistic AI-powered decision making
    /// </summary>
    public async Task<ClaimDecision> ValidateClaimWithSupportingDocumentsAsync(
        ClaimRequest request, 
        List<string> supportingDocumentIds)
    {
        if (_documentExtractionService == null)
        {
            throw new InvalidOperationException("Document extraction service not available for supporting document validation");
        }

        Console.WriteLine($"[Orchestrator] Validating claim with {supportingDocumentIds.Count} supporting documents");

        // Step 1: Extract content from all supporting documents
        var documentContents = new List<string>();
        var successfulExtractions = 0;
        
        foreach (var docId in supportingDocumentIds)
        {
            try
            {
                Console.WriteLine($"[Orchestrator] Extracting content from document: {docId}");
                
                // NEW: Use the proper content extraction method
                var documentText = await _documentExtractionService.ExtractDocumentContentAsync(docId);
                
                var docContent = $"Document ID: {docId}\n{documentText}";
                documentContents.Add(docContent);
                successfulExtractions++;
                
                Console.WriteLine($"[Orchestrator] Successfully extracted {documentText.Length} characters from {docId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Orchestrator] Warning: Failed to extract document {docId}: {ex.Message}");
                documentContents.Add($"Document ID: {docId}\n[Extraction failed - document unavailable for validation]");
            }
        }
        
        Console.WriteLine($"[Orchestrator] Successfully extracted {successfulExtractions} of {supportingDocumentIds.Count} documents");

        // Step 2: Generate embedding for claim + supporting evidence
        var combinedText = $"{request.ClaimDescription}\n\nSupporting Evidence:\n{string.Join("\n", documentContents)}";
        var embedding = await _embeddingService.GenerateEmbeddingAsync(combinedText);

        // Step 3: Retrieve relevant policy clauses
        var clauses = await _retrievalService.RetrieveClausesAsync(embedding, request.PolicyType);

        // Step 4: Guardrail - if no clauses found, manual review required
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

        // Step 5: Generate decision using LLM with supporting documents
        Console.WriteLine($"[Orchestrator] Generating AI decision with {documentContents.Count} supporting documents");
        var decision = await _llmService.GenerateDecisionWithSupportingDocumentsAsync(request, clauses, documentContents);

        // Step 6: GUARDRAIL - Validate citations to prevent hallucinations
        var citationValidation = _citationValidator.ValidateLlmResponse(decision, clauses);
        if (!citationValidation.IsValid)
        {
            Console.WriteLine($"[Guardrail] Citation validation failed: {string.Join(", ", citationValidation.Errors)}");
            
            return new ClaimDecision(
                Status: "Manual Review",
                Explanation: "AI response failed citation validation. " + string.Join(" ", citationValidation.Errors),
                ClauseReferences: decision.ClauseReferences,
                RequiredDocuments: decision.RequiredDocuments,
                ConfidenceScore: 0.0f,
                ValidationWarnings: citationValidation.Errors,
                ConfidenceRationale: "Citation validation failed with supporting documents"
            );
        }

        // Step 7: GUARDRAIL - Detect contradictions (including document consistency)
        var contradictions = _contradictionDetector.DetectContradictions(request, decision, clauses, documentContents);
        if (_contradictionDetector.HasCriticalContradictions(contradictions))
        {
            Console.WriteLine($"[Guardrail] Critical contradictions detected with supporting docs: {contradictions.Count}");
            
            decision = decision with
            {
                Status = "Manual Review",
                Explanation = "Critical contradictions detected in supporting documents. " + decision.Explanation,
                Contradictions = contradictions,
                ValidationWarnings = _contradictionDetector.GetContradictionSummary(contradictions)
            };
        }
        else if (contradictions.Any())
        {
            decision = decision with
            {
                Contradictions = contradictions,
                ValidationWarnings = _contradictionDetector.GetContradictionSummary(contradictions)
            };
        }

        // Step 8: Add citation warnings if present
        if (citationValidation.HasWarnings)
        {
            var warnings = decision.ValidationWarnings?.ToList() ?? new List<string>();
            warnings.AddRange(citationValidation.Warnings ?? new List<string>());
            decision = decision with { ValidationWarnings = warnings };
        }

        // Step 9: Apply enhanced business rules
        decision = ApplyBusinessRules(decision, request, hasSupportingDocuments: true);

        // Step 10: Audit trail (mandatory for compliance)
        await _auditService.SaveAsync(request, decision, clauses);

        Console.WriteLine($"[Orchestrator] Claim validated with supporting docs - Status: {decision.Status}, Confidence: {decision.ConfidenceScore:F2}");

        return decision;
    }

    private ClaimDecision ApplyBusinessRules(ClaimDecision decision, ClaimRequest request, bool hasSupportingDocuments = false)
    {
        // Aflac-style decision rules with amount-based enhancements
        const decimal autoApprovalThreshold = 5000m;
        const decimal lowValueThreshold = 500m;
        const decimal moderateValueThreshold = 1000m;
        const float confidenceThreshold = 0.85f;
        const float highConfidenceThreshold = 0.90f;

        var missingEvidence = new List<string>();
        string? confidenceRationale = null;

        // Rule 1: Low confidence → Manual Review
        if (decision.ConfidenceScore < confidenceThreshold)
        {
            confidenceRationale = $"Confidence {decision.ConfidenceScore:F2} below threshold {confidenceThreshold}";
            
            if (!hasSupportingDocuments)
                missingEvidence.Add("Supporting medical documents would increase confidence");
            if (decision.ClauseReferences.Count < 2)
                missingEvidence.Add("Additional policy clause references would strengthen decision");
                
            return decision with
            {
                Status = "Manual Review",
                Explanation = $"Confidence below threshold ({decision.ConfidenceScore:F2} < {confidenceThreshold}). " + decision.Explanation,
                MissingEvidence = missingEvidence,
                ConfidenceRationale = confidenceRationale
            };
        }

        // Rule 2: Low-value claims with high confidence and supporting docs → Auto-approve
        if (request.ClaimAmount < lowValueThreshold && 
            decision.ConfidenceScore >= highConfidenceThreshold && 
            decision.Status == "Covered" &&
            hasSupportingDocuments)
        {
            confidenceRationale = $"High confidence ({decision.ConfidenceScore:F2}) with supporting documents for low-value claim";
            
            return decision with
            {
                Explanation = $"Auto-approved: Low-value claim (${request.ClaimAmount}) with high confidence ({decision.ConfidenceScore:F2}) and supporting documentation. " + decision.Explanation,
                ConfidenceRationale = confidenceRationale
            };
        }

        // Rule 3: Moderate-value claims with good confidence → Reduced review requirements
        if (request.ClaimAmount < moderateValueThreshold && 
            decision.ConfidenceScore >= confidenceThreshold && 
            decision.Status == "Covered")
        {
            confidenceRationale = $"Good confidence ({decision.ConfidenceScore:F2}) for moderate-value claim";
            
            return decision with
            {
                Explanation = $"Moderate-value claim (${request.ClaimAmount}) with good confidence ({decision.ConfidenceScore:F2}). " + decision.Explanation,
                ConfidenceRationale = confidenceRationale
            };
        }

        // Rule 4: High amount + covered → Manual Review (even with high confidence)
        if (request.ClaimAmount > autoApprovalThreshold && decision.Status == "Covered")
        {
            confidenceRationale = $"High-value claim (${request.ClaimAmount}) requires manual review regardless of confidence";
            missingEvidence.Add("Specialist review required for high-value claims");
            
            return decision with
            {
                Status = "Manual Review",
                Explanation = $"Amount ${request.ClaimAmount} exceeds auto-approval limit (${autoApprovalThreshold}). " + decision.Explanation,
                MissingEvidence = missingEvidence,
                ConfidenceRationale = confidenceRationale
            };
        }

        // Rule 5: Exclusion clause detected → Deny or Manual Review
        if (decision.ClauseReferences.Any(c => c.Contains("Exclusion", StringComparison.OrdinalIgnoreCase)))
        {
            confidenceRationale = "Exclusion clause detected - requires careful interpretation";
            
            return decision with
            {
                Status = decision.Status == "Covered" ? "Manual Review" : decision.Status,
                Explanation = "Potential exclusion clause detected. " + decision.Explanation,
                ConfidenceRationale = confidenceRationale
            };
        }

        return decision;
    }
}
