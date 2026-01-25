namespace ClaimsRagBot.Core.Models;

public record ComprehendEntity(
    string Type,
    string Text,
    float Score,
    int BeginOffset,
    int EndOffset
);
