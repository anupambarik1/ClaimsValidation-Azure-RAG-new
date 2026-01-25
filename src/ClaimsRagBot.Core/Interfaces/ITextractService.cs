using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

public interface ITextractService
{
    Task<TextractResult> AnalyzeDocumentAsync(string s3Bucket, string s3Key, string[] featureTypes);
    Task<TextractResult> DetectDocumentTextAsync(string s3Bucket, string s3Key);
}
