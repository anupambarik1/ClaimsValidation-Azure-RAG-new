using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

/// <summary>
/// Service interface for validating LLM citations and preventing hallucinations
/// </summary>
public interface ICitationValidator
{
    /// <summary>
    /// Validates that an LLM decision includes proper citations backed by actual policy clauses
    /// </summary>
    ValidationResult ValidateLlmResponse(ClaimDecision decision, List<PolicyClause> availableClauses);

    /// <summary>
    /// Checks if all citations reference actual clauses from the policy database
    /// </summary>
    bool AreCitationsValid(List<string> citations, List<PolicyClause> availableClauses);

    /// <summary>
    /// Returns list of citations that don't exist in available clauses (hallucinated)
    /// </summary>
    List<string> GetMissingCitations(List<string> citations, List<PolicyClause> availableClauses);

    /// <summary>
    /// Detects language patterns that indicate potential hallucination
    /// </summary>
    List<string> DetectHallucinationIndicators(string explanation);

    /// <summary>
    /// Enhances explanation text with full citation details
    /// </summary>
    string EnhanceExplanationWithCitations(string explanation, List<PolicyClause> citedClauses);
}
