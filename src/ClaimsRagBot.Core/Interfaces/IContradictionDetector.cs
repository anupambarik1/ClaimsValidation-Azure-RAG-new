using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

/// <summary>
/// Service interface for detecting contradictions in claim data, decisions, and supporting evidence
/// </summary>
public interface IContradictionDetector
{
    /// <summary>
    /// Detects contradictions between claim request, decision, policy clauses, and supporting documents
    /// </summary>
    List<Contradiction> DetectContradictions(
        ClaimRequest request,
        ClaimDecision decision,
        List<PolicyClause> clauses,
        List<string>? supportingDocumentContents = null);

    /// <summary>
    /// Determines if any contradictions are critical enough to require manual review
    /// </summary>
    bool HasCriticalContradictions(List<Contradiction> contradictions);

    /// <summary>
    /// Returns a human-readable summary of detected contradictions
    /// </summary>
    List<string> GetContradictionSummary(List<Contradiction> contradictions);
}
