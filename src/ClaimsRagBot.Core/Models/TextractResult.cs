namespace ClaimsRagBot.Core.Models;

public record TextractResult(
    string ExtractedText,
    Dictionary<string, string> KeyValuePairs,
    List<TableData> Tables,
    float Confidence
);

public record TableData(
    int TableIndex,
    List<List<string>> Rows,
    float Confidence
);
