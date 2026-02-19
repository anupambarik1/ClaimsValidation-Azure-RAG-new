using System.Text.RegularExpressions;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;

namespace ClaimsRagBot.Application.Security;

/// <summary>
/// Detects and prevents prompt injection attacks in user inputs before they reach the LLM
/// </summary>
public class PromptInjectionDetector : IPromptInjectionDetector
{
    private static readonly List<string> DangerousPatterns = new()
    {
        "ignore previous instructions",
        "ignore all previous",
        "disregard all",
        "forget everything",
        "forget all previous",
        "you are now",
        "new instructions:",
        "new role:",
        "system:",
        "system prompt",
        "admin mode",
        "developer mode",
        "jailbreak",
        "override",
        "sudo mode",
        "<script>",
        "eval(",
        "execute(",
        "exec(",
        "system(",
        "import os",
        "subprocess",
        "__import__",
        "base64.b64decode",
        "<!--",
        "*/",
        "/*",
        "';",
        "\"; ",
        "../../",
        "../"
    };

    private static readonly List<string> SuspiciousRoleChanges = new()
    {
        "you are a",
        "act as",
        "pretend to be",
        "simulate",
        "roleplay as",
        "imagine you are"
    };

    private static readonly Regex HiddenUnicodePattern = new(@"[\u200B-\u200D\uFEFF\u2060-\u2069]", RegexOptions.Compiled);
    private static readonly Regex ExcessiveRepeatingPattern = new(@"(.)\1{20,}", RegexOptions.Compiled);
    private static readonly Regex Base64Pattern = new(@"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$", RegexOptions.Compiled);

    public (bool IsClean, List<string> Threats) ScanInput(string input)
    {
        var threats = new List<string>();

        if (string.IsNullOrEmpty(input))
            return (true, threats);

        var normalized = input.ToLowerInvariant();

        // Check for dangerous instruction patterns
        foreach (var pattern in DangerousPatterns)
        {
            if (normalized.Contains(pattern))
            {
                threats.Add($"Detected suspicious pattern: '{pattern}'");
            }
        }

        // Check for role manipulation attempts
        foreach (var pattern in SuspiciousRoleChanges)
        {
            if (normalized.Contains(pattern) && normalized.Contains("ignore"))
            {
                threats.Add($"Detected potential role manipulation: '{pattern}'");
            }
        }

        // Check for hidden unicode characters
        if (HiddenUnicodePattern.IsMatch(input))
        {
            threats.Add("Contains hidden unicode characters that may be used for obfuscation");
        }

        // Check for excessive character repetition (potential DoS)
        if (ExcessiveRepeatingPattern.IsMatch(input))
        {
            threats.Add("Contains excessive character repetition (potential DoS attempt)");
        }

        // Check for suspiciously long inputs (>10,000 characters)
        if (input.Length > 10000)
        {
            threats.Add($"Input exceeds safe length limit (Length: {input.Length}, Limit: 10000)");
        }

        // Check for potential encoding obfuscation
        if (input.Length > 100 && Base64Pattern.IsMatch(input.Replace("\n", "").Replace("\r", "")))
        {
            threats.Add("Input appears to be base64 encoded (potential obfuscation)");
        }

        // Check for SQL injection patterns (in case of database queries)
        var sqlPatterns = new[] { "drop table", "delete from", "insert into", "update ", "'; --", "1=1", "union select" };
        foreach (var pattern in sqlPatterns)
        {
            if (normalized.Contains(pattern))
            {
                threats.Add($"Detected SQL-like pattern: '{pattern}'");
            }
        }

        // Check for excessive special characters (>30% of text)
        var specialCharCount = input.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
        var specialCharRatio = (double)specialCharCount / input.Length;
        if (specialCharRatio > 0.3)
        {
            threats.Add($"Excessive special characters detected ({specialCharRatio:P0} of input)");
        }

        return (threats.Count == 0, threats);
    }

    public ValidationResult ValidateClaimDescription(string description)
    {
        var (isClean, threats) = ScanInput(description);

        if (!isClean)
        {
            return new ValidationResult(
                IsValid: false,
                Errors: threats,
                WarningMessage: "Claim description contains potentially malicious content"
            );
        }

        // Additional validation for claim-specific content
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(description))
        {
            return new ValidationResult(false, new List<string> { "Claim description cannot be empty" });
        }

        if (description.Length < 10)
        {
            warnings.Add("Claim description is very short (minimum 10 characters recommended)");
        }

        if (description.Length > 5000)
        {
            return new ValidationResult(false, new List<string> { "Claim description exceeds maximum length (5000 characters)" });
        }

        return new ValidationResult(true, warnings);
    }

    public bool ContainsPromptInjection(string text)
    {
        var (isClean, _) = ScanInput(text);
        return !isClean;
    }

    public string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove hidden unicode characters
        input = HiddenUnicodePattern.Replace(input, "");

        // Normalize whitespace
        input = Regex.Replace(input, @"\s+", " ");

        // Remove potential script tags
        input = Regex.Replace(input, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase);

        // Truncate to safe length
        if (input.Length > 10000)
        {
            input = input.Substring(0, 10000);
        }

        return input.Trim();
    }
}
