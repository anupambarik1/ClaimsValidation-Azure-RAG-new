using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

public interface IComprehendService
{
    Task<List<ComprehendEntity>> DetectEntitiesAsync(string text);
    Task<Dictionary<string, string>> ExtractClaimFieldsAsync(string text);
}
