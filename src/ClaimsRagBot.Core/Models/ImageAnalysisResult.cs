namespace ClaimsRagBot.Core.Models;

public record ImageAnalysisResult(
    string ImageId,
    List<string> Labels,
    string DamageType,
    float Confidence,
    Dictionary<string, object> Metadata
);
