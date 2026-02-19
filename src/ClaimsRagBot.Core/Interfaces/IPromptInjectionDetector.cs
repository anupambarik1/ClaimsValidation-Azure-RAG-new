using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Core.Interfaces;

/// <summary>
/// Service interface for detecting and preventing prompt injection attacks
/// </summary>
public interface IPromptInjectionDetector
{
    /// <summary>
    /// Scans input text for prompt injection patterns
    /// </summary>
    /// <returns>Tuple indicating if input is clean and list of detected threats</returns>
    (bool IsClean, List<string> Threats) ScanInput(string input);

    /// <summary>
    /// Validates a claim description for security threats and content requirements
    /// </summary>
    ValidationResult ValidateClaimDescription(string description);

    /// <summary>
    /// Quick check if text contains prompt injection patterns
    /// </summary>
    bool ContainsPromptInjection(string text);

    /// <summary>
    /// Sanitizes input by removing dangerous patterns and normalizing content
    /// </summary>
    string SanitizeInput(string input);
}
