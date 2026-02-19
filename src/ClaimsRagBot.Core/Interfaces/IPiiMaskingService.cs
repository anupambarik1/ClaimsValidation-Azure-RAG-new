namespace ClaimsRagBot.Core.Interfaces;

/// <summary>
/// Service interface for masking and redacting Personally Identifiable Information (PII) and Protected Health Information (PHI)
/// </summary>
public interface IPiiMaskingService
{
    /// <summary>
    /// Masks a member ID to show only the last 4 digits
    /// </summary>
    string MaskMemberId(string memberId);

    /// <summary>
    /// Masks a policy number to show only the last 4 digits
    /// </summary>
    string MaskPolicyNumber(string policyNumber);

    /// <summary>
    /// Completely masks a Social Security Number
    /// </summary>
    string MaskSsn(string ssn);

    /// <summary>
    /// Completely masks a phone number
    /// </summary>
    string MaskPhone(string phone);

    /// <summary>
    /// Partially masks an email address (keeps domain visible)
    /// </summary>
    string MaskEmail(string email);

    /// <summary>
    /// Redacts all PII patterns from a text string
    /// </summary>
    string RedactPii(string text);

    /// <summary>
    /// Redacts PHI (Protected Health Information) from explanations and decision text
    /// </summary>
    string RedactPhiFromExplanation(string explanation);

    /// <summary>
    /// Checks if text contains sensitive data
    /// </summary>
    bool ContainsSensitiveData(string text);

    /// <summary>
    /// Detects and counts types of PII found in text
    /// </summary>
    Dictionary<string, int> DetectPiiTypes(string text);
}
