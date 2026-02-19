using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Application.Validation;

/// <summary>
/// Validates that LLM responses include proper evidence citations to prevent hallucinations
/// and ensure all claims are backed by policy clauses
/// </summary>
public class CitationValidator : ICitationValidator
{
    public ValidationResult ValidateLlmResponse(ClaimDecision decision, List<PolicyClause> availableClauses)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Rule 1: Require at least one citation for decisions other than errors
        if (decision.Status != "Error" && !decision.ClauseReferences.Any())
        {
            errors.Add("LLM response missing required policy citations. All decisions must be backed by policy clauses.");
        }

        // Rule 2: Verify all cited clauses actually exist in retrieved clauses
        var availableClauseIds = availableClauses.Select(c => c.ClauseId).ToHashSet();
        
        foreach (var citation in decision.ClauseReferences)
        {
            if (!availableClauseIds.Contains(citation))
            {
                errors.Add($"Cited clause '{citation}' not found in retrieved policy clauses. This may indicate hallucination.");
            }
        }

        // Rule 3: Check for low-confidence decisions with high citation count (possible hallucination)
        if (decision.ConfidenceScore < 0.5f && decision.ClauseReferences.Count > 5)
        {
            warnings.Add($"Low confidence ({decision.ConfidenceScore:F2}) with many citations ({decision.ClauseReferences.Count}) may indicate over-fitting or hallucination.");
        }

        // Rule 4: Check explanation contains reference to citations
        if (decision.ClauseReferences.Any() && !ContainsCitationReferences(decision.Explanation))
        {
            warnings.Add("Explanation does not reference the cited policy clauses. Consider improving citation integration.");
        }

        // Rule 5: Check for hallucination indicators in explanation
        var hallucinationIndicators = DetectHallucinationIndicators(decision.Explanation);
        if (hallucinationIndicators.Any())
        {
            warnings.AddRange(hallucinationIndicators.Select(h => $"Potential hallucination indicator: {h}"));
        }

        // Rule 6: Covered decisions must have citations
        if (decision.Status == "Covered" && decision.ClauseReferences.Count == 0)
        {
            errors.Add("'Covered' decisions must cite at least one policy clause supporting coverage.");
        }

        // Rule 7: Denied decisions should cite exclusions or limitations
        if (decision.Status == "Denied" && decision.ClauseReferences.Count == 0)
        {
            warnings.Add("'Denied' decisions should cite policy exclusions or limitations for transparency.");
        }

        return new ValidationResult(
            IsValid: errors.Count == 0,
            Errors: errors,
            Warnings: warnings,
            WarningMessage: warnings.Any() ? "Citation quality issues detected" : null
        );
    }

    public bool AreCitationsValid(List<string> citations, List<PolicyClause> availableClauses)
    {
        if (!citations.Any())
            return false;

        var availableClauseIds = availableClauses.Select(c => c.ClauseId).ToHashSet();
        
        return citations.All(citation => availableClauseIds.Contains(citation));
    }

    public List<string> GetMissingCitations(List<string> citations, List<PolicyClause> availableClauses)
    {
        var availableClauseIds = availableClauses.Select(c => c.ClauseId).ToHashSet();
        
        return citations.Where(citation => !availableClauseIds.Contains(citation)).ToList();
    }

    public List<string> DetectHallucinationIndicators(string explanation)
    {
        var indicators = new List<string>();

        if (string.IsNullOrEmpty(explanation))
            return indicators;

        var normalized = explanation.ToLowerInvariant();

        // Check for uncertainty language suggesting guessing
        var uncertaintyPhrases = new[]
        {
            "i think", "i believe", "probably", "maybe", "possibly",
            "it seems", "appears to be", "likely", "might be", "could be",
            "generally", "typically", "usually", "in most cases"
        };

        foreach (var phrase in uncertaintyPhrases)
        {
            if (normalized.Contains(phrase))
            {
                indicators.Add($"Uncertainty phrase: '{phrase}'");
            }
        }

        // Check for personal knowledge claims (LLM should only use provided policy)
        var personalKnowledgePhrases = new[]
        {
            "i know that", "i understand", "in my experience",
            "i recall", "i remember", "based on my knowledge"
        };

        foreach (var phrase in personalKnowledgePhrases)
        {
            if (normalized.Contains(phrase))
            {
                indicators.Add($"Personal knowledge claim: '{phrase}'");
            }
        }

        // Check for vague references instead of specific citations
        var vagueReferences = new[]
        {
            "according to the policy", "the policy states",
            "policy guidelines", "standard practice",
            "insurance regulations", "common practice"
        };

        var hasVagueReference = vagueReferences.Any(vr => normalized.Contains(vr));
        var hasSpecificCitation = normalized.Contains("clause") || 
                                 normalized.Contains("section") || 
                                 normalized.Contains("[") ||
                                 normalized.Contains("policy_");

        if (hasVagueReference && !hasSpecificCitation)
        {
            indicators.Add("Vague policy reference without specific clause citation");
        }

        return indicators;
    }

    public string EnhanceExplanationWithCitations(string explanation, List<PolicyClause> citedClauses)
    {
        if (string.IsNullOrEmpty(explanation) || !citedClauses.Any())
            return explanation;

        var enhancedExplanation = explanation;

        // Append citation details at the end
        enhancedExplanation += "\n\nPolicy References:\n";
        
        foreach (var clause in citedClauses)
        {
            var clausePreview = clause.Text.Length > 100 
                ? clause.Text.Substring(0, 100) + "..." 
                : clause.Text;
                
            enhancedExplanation += $"- [{clause.ClauseId}] {clausePreview}\n";
        }

        return enhancedExplanation;
    }

    private bool ContainsCitationReferences(string explanation)
    {
        if (string.IsNullOrEmpty(explanation))
            return false;

        // Check for citation formats: [ClauseID], Clause: ID, Section X, etc.
        var citationPatterns = new[]
        {
            @"\[.*?\]",           // [clause_id]
            @"clause[:\s]",       // clause: or clause
            @"section[:\s]\d+",   // section: 4 or section 4
            @"policy_\w+",        // policy_life_001
            @"health_policy_\w+", // health_policy_001
            @"life_policy_\w+"    // life_policy_001
        };

        return citationPatterns.Any(pattern => 
            System.Text.RegularExpressions.Regex.IsMatch(explanation, pattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }
}
