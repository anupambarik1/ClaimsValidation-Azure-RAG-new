using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

public interface IAuditService
{
    Task SaveAsync(ClaimRequest request, ClaimDecision decision, List<PolicyClause> clauses);
    Task<ClaimAuditRecord?> GetByClaimIdAsync(string claimId);
    Task<List<ClaimAuditRecord>> GetByPolicyNumberAsync(string policyNumber);
    Task<List<ClaimAuditRecord>> GetAllClaimsAsync(string? statusFilter = null);
    Task<bool> UpdateClaimDecisionAsync(string claimId, string newStatus, string specialistNotes, string specialistId);
}
