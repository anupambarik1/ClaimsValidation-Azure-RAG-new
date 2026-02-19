using System.Text.RegularExpressions;
using ClaimsRagBot.Core.Interfaces;

namespace ClaimsRagBot.Application.Security;

/// <summary>
/// Service for masking and redacting Personally Identifiable Information (PII) and Protected Health Information (PHI)
/// to ensure compliance with privacy regulations (HIPAA, GDPR)
/// </summary>
public class PiiMaskingService : IPiiMaskingService
{
    private static readonly Regex SsnPattern = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex EmailPattern = new(@"\b[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}\b", RegexOptions.Compiled);
    private static readonly Regex CreditCardPattern = new(@"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex DateOfBirthPattern = new(@"\b(0?[1-9]|1[0-2])[/\-](0?[1-9]|[12][0-9]|3[01])[/\-](19|20)\d{2}\b", RegexOptions.Compiled);
    private static readonly Regex ZipCodePattern = new(@"\b\d{5}(?:-\d{4})?\b", RegexOptions.Compiled);

    public string MaskMemberId(string memberId)
    {
        if (string.IsNullOrEmpty(memberId))
            return "****";

        return memberId.Length > 4
            ? $"****{memberId.Substring(memberId.Length - 4)}"
            : "****";
    }

    public string MaskPolicyNumber(string policyNumber)
    {
        if (string.IsNullOrEmpty(policyNumber))
            return "****";

        return policyNumber.Length > 4
            ? $"****{policyNumber.Substring(policyNumber.Length - 4)}"
            : "****";
    }

    public string MaskSsn(string ssn)
    {
        return "***-**-****";
    }

    public string MaskPhone(string phone)
    {
        return "***-***-****";
    }

    public string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "***@***.***";

        var parts = email.Split('@');
        return $"***@{parts[1]}";
    }

    public string RedactPii(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Redact SSN patterns
        text = SsnPattern.Replace(text, "***-**-****");

        // Redact phone numbers
        text = PhonePattern.Replace(text, "***-***-****");

        // Redact email addresses
        text = EmailPattern.Replace(text, m =>
        {
            var parts = m.Value.Split('@');
            return parts.Length == 2 ? $"***@{parts[1]}" : "***@***.***";
        });

        // Redact credit card numbers
        text = CreditCardPattern.Replace(text, "****-****-****-****");

        // Redact dates that might be DOB
        text = DateOfBirthPattern.Replace(text, "**/**/****");

        // Redact specific ZIP codes (keep first 3 digits for region)
        text = ZipCodePattern.Replace(text, m =>
        {
            var zip = m.Value.Replace("-", "");
            return zip.Length >= 3 ? $"{zip.Substring(0, 3)}**" : "*****";
        });

        return text;
    }

    public string RedactPhiFromExplanation(string explanation)
    {
        if (string.IsNullOrEmpty(explanation))
            return explanation;

        // Common PHI terms to redact
        var phiPatterns = new Dictionary<string, string>
        {
            { @"\b(?:patient|member|insured)\s+name:\s*[^\.,]+", "patient name: [REDACTED]" },
            { @"\b(?:diagnosis|diagnosed with):\s*[^\.,]+", "diagnosis: [REDACTED]" },
            { @"\b(?:prescription|medication|drug):\s*[^\.,]+", "medication: [REDACTED]" },
            { @"\b(?:procedure|treatment|surgery):\s*[^\.,]+", "procedure: [REDACTED]" },
            { @"\b(?:doctor|physician|provider)\s+(?:name:\s*)?[A-Z][a-z]+\s+[A-Z][a-z]+", "provider: [REDACTED]" }
        };

        foreach (var (pattern, replacement) in phiPatterns)
        {
            explanation = Regex.Replace(explanation, pattern, replacement, RegexOptions.IgnoreCase);
        }

        return RedactPii(explanation);
    }

    public bool ContainsSensitiveData(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return SsnPattern.IsMatch(text) ||
               CreditCardPattern.IsMatch(text) ||
               EmailPattern.IsMatch(text) ||
               PhonePattern.IsMatch(text);
    }

    public Dictionary<string, int> DetectPiiTypes(string text)
    {
        var detectedTypes = new Dictionary<string, int>();

        if (string.IsNullOrEmpty(text))
            return detectedTypes;

        detectedTypes["SSN"] = SsnPattern.Matches(text).Count;
        detectedTypes["Phone"] = PhonePattern.Matches(text).Count;
        detectedTypes["Email"] = EmailPattern.Matches(text).Count;
        detectedTypes["CreditCard"] = CreditCardPattern.Matches(text).Count;
        detectedTypes["DateOfBirth"] = DateOfBirthPattern.Matches(text).Count;
        detectedTypes["ZipCode"] = ZipCodePattern.Matches(text).Count;

        return detectedTypes.Where(kvp => kvp.Value > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
