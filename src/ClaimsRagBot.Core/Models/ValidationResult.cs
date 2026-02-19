namespace ClaimsRagBot.Core.Models;

/// <summary>
/// Represents the result of a validation operation with errors, warnings, and status
/// </summary>
public record ValidationResult(
    bool IsValid,
    List<string> Errors,
    List<string>? Warnings = null,
    string? WarningMessage = null
)
{
    // Convenience constructor for simple valid results
    public ValidationResult(bool isValid, List<string> errors) 
        : this(isValid, errors, null, null) { }

    // Convenience constructor for warnings only
    public ValidationResult(bool isValid, List<string> errors, string warningMessage) 
        : this(isValid, errors, null, warningMessage) { }

    public bool HasWarnings => Warnings?.Any() == true || !string.IsNullOrEmpty(WarningMessage);
    
    public int ErrorCount => Errors?.Count ?? 0;
    
    public int WarningCount => Warnings?.Count ?? 0;

    public string GetSummary()
    {
        if (IsValid && !HasWarnings)
            return "Validation passed";

        if (IsValid && HasWarnings)
            return $"Validation passed with {WarningCount} warning(s)";

        return $"Validation failed with {ErrorCount} error(s)";
    }

    public List<string> GetAllIssues()
    {
        var issues = new List<string>();
        
        if (Errors?.Any() == true)
            issues.AddRange(Errors.Select(e => $"ERROR: {e}"));
            
        if (Warnings?.Any() == true)
            issues.AddRange(Warnings.Select(w => $"WARNING: {w}"));
            
        if (!string.IsNullOrEmpty(WarningMessage))
            issues.Add($"WARNING: {WarningMessage}");

        return issues;
    }
}
