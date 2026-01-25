namespace ClaimsRagBot.Core.Models;

public record ClaimExtractionResult(
    ClaimRequest ExtractedClaim,
    float OverallConfidence,
    Dictionary<string, float> FieldConfidences,
    List<string> AmbiguousFields,
    Dictionary<string, object> RawExtractedData
);
