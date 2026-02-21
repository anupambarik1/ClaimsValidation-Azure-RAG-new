namespace ClaimsRagBot.Core.Models;

/// <summary>
/// Represents a claim audit record stored in DynamoDB
/// </summary>
public record ClaimAuditRecord(
    /// <summary>
    /// Unique claim identifier
    /// </summary>
    string ClaimId,
    
    /// <summary>
    /// Timestamp when claim was submitted
    /// </summary>
    DateTime Timestamp,
    
    /// <summary>
    /// Policy number associated with the claim
    /// </summary>
    string PolicyNumber,
    
    /// <summary>
    /// Claim amount requested
    /// </summary>
    decimal ClaimAmount,
    
    /// <summary>
    /// Description of the claim
    /// </summary>
    string ClaimDescription,
    
    /// <summary>
    /// Decision status: Covered, Not Covered, or Manual Review
    /// </summary>
    string DecisionStatus,
    
    /// <summary>
    /// AI-generated explanation for the decision
    /// </summary>
    string Explanation,
    
    /// <summary>
    /// AI confidence score (0.0 to 1.0)
    /// </summary>
    float ConfidenceScore,
    
    /// <summary>
    /// List of relevant policy clause references
    /// </summary>
    List<string> ClauseReferences,
    
    /// <summary>
    /// List of required documents for claim processing
    /// </summary>
    List<string> RequiredDocuments,
    
    /// <summary>
    /// IDs of uploaded supporting documents associated with this claim
    /// </summary>
    List<string>? DocumentIds = null,
    
    /// <summary>
    /// Specialist notes when decision was reviewed/updated
    /// </summary>
    string? SpecialistNotes = null,
    
    /// <summary>
    /// ID of specialist who reviewed the claim
    /// </summary>
    string? SpecialistId = null,
    
    /// <summary>
    /// Timestamp when specialist reviewed the claim
    /// </summary>
    DateTime? ReviewedAt = null
);
