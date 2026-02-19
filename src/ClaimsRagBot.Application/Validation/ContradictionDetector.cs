using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Application.Validation;

/// <summary>
/// Detects contradictions between claim data, supporting documents, and policy clauses
/// to identify inconsistencies that require manual review
/// </summary>
public class ContradictionDetector : IContradictionDetector
{
    public List<Contradiction> DetectContradictions(
        ClaimRequest request, 
        ClaimDecision decision, 
        List<PolicyClause> clauses,
        List<string>? supportingDocumentContents = null)
    {
        var contradictions = new List<Contradiction>();

        // Check 1: Decision vs. Citations
        contradictions.AddRange(CheckDecisionVsCitations(request, decision, clauses));

        // Check 2: Exclusion clauses cited but claim marked as covered
        contradictions.AddRange(CheckExclusionContradictions(decision, clauses));

        // Check 3: Confidence vs. Status mismatch
        contradictions.AddRange(CheckConfidenceStatusMismatch(decision));

        // Check 4: Amount vs. Policy limits
        contradictions.AddRange(CheckAmountLimits(request, clauses));

        // Check 5: Supporting documents vs. claim description
        if (supportingDocumentContents?.Any() == true)
        {
            contradictions.AddRange(CheckDocumentConsistency(request, supportingDocumentContents));
        }

        return contradictions;
    }

    public bool HasCriticalContradictions(List<Contradiction> contradictions)
    {
        return contradictions.Any(c => c.Severity == "Critical" || c.Severity == "High");
    }

    public List<string> GetContradictionSummary(List<Contradiction> contradictions)
    {
        return contradictions
            .OrderByDescending(c => GetSeverityOrder(c.Severity))
            .Select(c => $"[{c.Severity}] {c.Description}: {c.SourceA} vs {c.SourceB}")
            .ToList();
    }

    private List<Contradiction> CheckDecisionVsCitations(
        ClaimRequest request, 
        ClaimDecision decision, 
        List<PolicyClause> clauses)
    {
        var contradictions = new List<Contradiction>();

        // If denied but no denial-related clauses cited
        if (decision.Status == "Denied" && decision.ClauseReferences.Any())
        {
            var citedClauses = clauses.Where(c => decision.ClauseReferences.Contains(c.ClauseId)).ToList();
            var hasExclusionClause = citedClauses.Any(c => 
                c.Text.Contains("exclusion", StringComparison.OrdinalIgnoreCase) ||
                c.Text.Contains("not covered", StringComparison.OrdinalIgnoreCase) ||
                c.Text.Contains("excluded", StringComparison.OrdinalIgnoreCase));

            if (!hasExclusionClause)
            {
                contradictions.Add(new Contradiction(
                    SourceA: "Decision Status",
                    SourceB: "Cited Policy Clauses",
                    Description: "Claim denied but cited clauses do not contain exclusion language",
                    Impact: "Decision may lack proper justification",
                    Severity: "High"
                ));
            }
        }

        // If covered but exclusion clauses are cited
        if (decision.Status == "Covered" && decision.ClauseReferences.Any())
        {
            var citedClauses = clauses.Where(c => decision.ClauseReferences.Contains(c.ClauseId)).ToList();
            var hasExclusionClause = citedClauses.Any(c => 
                c.Text.Contains("exclusion", StringComparison.OrdinalIgnoreCase));

            if (hasExclusionClause)
            {
                contradictions.Add(new Contradiction(
                    SourceA: "Decision Status (Covered)",
                    SourceB: "Policy Exclusion Clause",
                    Description: "Claim marked as covered but exclusion clause is cited",
                    Impact: "May result in incorrect approval",
                    Severity: "Critical"
                ));
            }
        }

        return contradictions;
    }

    private List<Contradiction> CheckExclusionContradictions(ClaimDecision decision, List<PolicyClause> clauses)
    {
        var contradictions = new List<Contradiction>();

        if (!decision.ClauseReferences.Any())
            return contradictions;

        var citedClauses = clauses.Where(c => decision.ClauseReferences.Contains(c.ClauseId)).ToList();

        // Check if multiple contradictory clauses are cited (e.g., coverage + exclusion)
        var hasCoverageClause = citedClauses.Any(c => 
            c.Text.Contains("covered", StringComparison.OrdinalIgnoreCase) ||
            c.Text.Contains("eligible", StringComparison.OrdinalIgnoreCase));

        var hasExclusionClause = citedClauses.Any(c => 
            c.Text.Contains("exclusion", StringComparison.OrdinalIgnoreCase) ||
            c.Text.Contains("not covered", StringComparison.OrdinalIgnoreCase));

        if (hasCoverageClause && hasExclusionClause)
        {
            contradictions.Add(new Contradiction(
                SourceA: "Coverage Policy Clause",
                SourceB: "Exclusion Policy Clause",
                Description: "Both coverage and exclusion clauses cited - requires policy interpretation",
                Impact: "Ambiguous policy application",
                Severity: "High"
            ));
        }

        return contradictions;
    }

    private List<Contradiction> CheckConfidenceStatusMismatch(ClaimDecision decision)
    {
        var contradictions = new List<Contradiction>();

        // High confidence but manual review status
        if (decision.ConfidenceScore > 0.85f && decision.Status == "Manual Review")
        {
            contradictions.Add(new Contradiction(
                SourceA: $"High Confidence Score ({decision.ConfidenceScore:F2})",
                SourceB: "Manual Review Status",
                Description: "AI is confident but decision requires manual review - may indicate conflicting business rules",
                Impact: "Potential for automated decision",
                Severity: "Medium"
            ));
        }

        // Low confidence but approved/denied (should be manual review)
        if (decision.ConfidenceScore < 0.70f && (decision.Status == "Covered" || decision.Status == "Denied"))
        {
            contradictions.Add(new Contradiction(
                SourceA: $"Low Confidence Score ({decision.ConfidenceScore:F2})",
                SourceB: $"Automated Decision ({decision.Status})",
                Description: "Low confidence decision made automatically - should trigger manual review",
                Impact: "Risk of incorrect decision",
                Severity: "High"
            ));
        }

        return contradictions;
    }

    private List<Contradiction> CheckAmountLimits(ClaimRequest request, List<PolicyClause> clauses)
    {
        var contradictions = new List<Contradiction>();

        // Extract dollar amounts from policy clauses
        foreach (var clause in clauses)
        {
            var amountMatches = System.Text.RegularExpressions.Regex.Matches(
                clause.Text, 
                @"\$[\d,]+(?:\.\d{2})?");

            foreach (System.Text.RegularExpressions.Match match in amountMatches)
            {
                var amountStr = match.Value.Replace("$", "").Replace(",", "");
                if (decimal.TryParse(amountStr, out var policyLimit))
                {
                    if (request.ClaimAmount > policyLimit && 
                        clause.Text.Contains("limit", StringComparison.OrdinalIgnoreCase))
                    {
                        contradictions.Add(new Contradiction(
                            SourceA: $"Claim Amount (${request.ClaimAmount})",
                            SourceB: $"Policy Limit (${policyLimit}) in {clause.ClauseId}",
                            Description: $"Claim amount exceeds policy limit by ${request.ClaimAmount - policyLimit}",
                            Impact: "May require partial approval or denial",
                            Severity: "High"
                        ));
                    }
                }
            }
        }

        return contradictions;
    }

    private List<Contradiction> CheckDocumentConsistency(ClaimRequest request, List<string> supportingDocumentContents)
    {
        var contradictions = new List<Contradiction>();

        var combinedDocs = string.Join(" ", supportingDocumentContents).ToLowerInvariant();
        var claimDesc = request.ClaimDescription.ToLowerInvariant();

        // Check for amount discrepancies
        var claimAmountInDesc = ExtractAmounts(claimDesc);
        var docAmounts = supportingDocumentContents.SelectMany(doc => ExtractAmounts(doc.ToLowerInvariant())).ToList();

        foreach (var docAmount in docAmounts)
        {
            if (Math.Abs(docAmount - request.ClaimAmount) > request.ClaimAmount * 0.1m) // >10% difference
            {
                contradictions.Add(new Contradiction(
                    SourceA: $"Claimed Amount (${request.ClaimAmount})",
                    SourceB: $"Document Amount (${docAmount})",
                    Description: $"Claim amount differs from supporting document by ${Math.Abs(docAmount - request.ClaimAmount)}",
                    Impact: "Verify correct claim amount",
                    Severity: "High"
                ));
            }
        }

        return contradictions;
    }

    private List<decimal> ExtractAmounts(string text)
    {
        var amounts = new List<decimal>();
        var matches = System.Text.RegularExpressions.Regex.Matches(
            text, 
            @"\$[\d,]+(?:\.\d{2})?");

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var amountStr = match.Value.Replace("$", "").Replace(",", "");
            if (decimal.TryParse(amountStr, out var amount))
            {
                amounts.Add(amount);
            }
        }

        return amounts;
    }

    private int GetSeverityOrder(string severity)
    {
        return severity switch
        {
            "Critical" => 4,
            "High" => 3,
            "Medium" => 2,
            "Low" => 1,
            _ => 0
        };
    }
}
