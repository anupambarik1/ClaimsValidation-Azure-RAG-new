using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

public interface IRekognitionService
{
    Task<ImageAnalysisResult> AnalyzeImageAsync(string s3Bucket, string s3Key);
    Task<List<ImageAnalysisResult>> AnalyzeImagesAsync(string s3Bucket, List<string> s3Keys);
}
