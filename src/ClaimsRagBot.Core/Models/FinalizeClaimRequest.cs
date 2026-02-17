namespace ClaimsRagBot.Core.Models;

/// <summary>
/// Request to finalize a claim with supporting documents
/// </summary>
public record FinalizeClaimRequest
{
    /// <summary>
    /// The claim data (from initial submission/extraction)
    /// </summary>
    public required ClaimRequest ClaimData { get; init; }

    /// <summary>
    /// List of supporting document IDs (admission records, discharge summary, bills, etc.)
    /// </summary>
    public List<string>? SupportingDocumentIds { get; init; }

    /// <summary>
    /// Optional notes from the claimant
    /// </summary>
    public string? Notes { get; init; }
}
