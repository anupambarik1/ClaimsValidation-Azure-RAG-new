using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

public interface ILlmService
{
    Task<ClaimDecision> GenerateDecisionAsync(ClaimRequest request, List<PolicyClause> clauses);
}
