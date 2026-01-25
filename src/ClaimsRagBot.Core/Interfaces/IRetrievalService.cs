using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

public interface IRetrievalService
{
    Task<List<PolicyClause>> RetrieveClausesAsync(float[] embedding, string policyType);
}
