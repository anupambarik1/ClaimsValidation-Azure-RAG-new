namespace ClaimsRagBot.Core.Models;

/// <summary>
/// Represents a contradiction detected between different sources of information in a claim
/// </summary>
public record Contradiction(
    string SourceA,
    string SourceB,
    string Description,
    string Impact,
    string Severity = "Medium"
)
{
    /// <summary>
    /// Gets whether this contradiction is critical (requires immediate attention)
    /// </summary>
    public bool IsCritical => Severity == "Critical" || Severity == "High";

    /// <summary>
    /// Gets a formatted summary of the contradiction
    /// </summary>
    public string GetSummary() => $"[{Severity}] {Description} - {SourceA} conflicts with {SourceB}. Impact: {Impact}";
}
