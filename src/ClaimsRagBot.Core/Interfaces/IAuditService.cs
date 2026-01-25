using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

public interface IAuditService
{
    Task SaveAsync(ClaimRequest request, ClaimDecision decision, List<PolicyClause> clauses);
}
