namespace ClaimsRagBot.Core.Models;

/// <summary>
/// Represents the AI-generated claim validation decision with enhanced guardrails
/// </summary>
public record ClaimDecision(
    /// <summary>
    /// Decision status: Covered, Not Covered, Denied, or Manual Review
    /// </summary>
    /// <example>Covered</example>
    string Status,
    
    /// <summary>
    /// AI-generated explanation for the decision
    /// </summary>
    /// <example>The claim is covered under comprehensive collision coverage as per clause 3.2</example>
    string Explanation,
    
    /// <summary>
    /// List of relevant policy clause references (evidence citations)
    /// </summary>
    List<string> ClauseReferences,
    
    /// <summary>
    /// List of required documents for claim processing
    /// </summary>
    List<string> RequiredDocuments,
    
    /// <summary>
    /// AI confidence score (0.0 to 1.0)
    /// </summary>
    /// <example>0.92</example>
    float ConfidenceScore,
    
    /// <summary>
    /// Detected contradictions between sources (guardrail)
    /// </summary>
    List<Contradiction>? Contradictions = null,
    
    /// <summary>
    /// Missing information that would improve decision confidence (guardrail)
    /// </summary>
    List<string>? MissingEvidence = null,
    
    /// <summary>
    /// Validation warnings from guardrail checks
    /// </summary>
    List<string>? ValidationWarnings = null,
    
    /// <summary>
    /// Rationale for the confidence score (guardrail)
    /// </summary>
    string? ConfidenceRationale = null
);
