using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

public interface ILlmService
{
    Task<ClaimDecision> GenerateDecisionAsync(ClaimRequest request, List<PolicyClause> clauses);
    
    /// <summary>
    /// Generates a claim decision using supporting documents for holistic validation
    /// </summary>
    Task<ClaimDecision> GenerateDecisionWithSupportingDocumentsAsync(
        ClaimRequest request, 
        List<PolicyClause> clauses, 
        List<string> supportingDocumentContents);
    
    /// <summary>
    /// Generates a response from LLM using custom prompts (for document extraction)
    /// </summary>
    Task<string> GenerateResponseAsync(string systemPrompt, string userPrompt);
}
