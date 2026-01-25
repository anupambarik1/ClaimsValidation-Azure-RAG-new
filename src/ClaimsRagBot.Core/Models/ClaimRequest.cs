using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace ClaimsRagBot.Core.Models;

/// <summary>
/// Represents a claim validation request
/// </summary>
public record ClaimRequest(
    /// <summary>
    /// Policy number associated with the claim
    /// </summary>
    /// <example>POL-2024-001</example>
    [Required]
    string PolicyNumber,
    
    /// <summary>
    /// Detailed description of the claim incident
    /// </summary>
    /// <example>Vehicle collision on highway resulting in front-end damage</example>
    [Required]
    string ClaimDescription,
    
    /// <summary>
    /// Claim amount in dollars
    /// </summary>
    /// <example>5000</example>
    [Required]
    [Range(0.01, double.MaxValue)]
    decimal ClaimAmount,
    
    /// <summary>
    /// Type of insurance policy
    /// </summary>
    /// <example>Motor</example>
    [DefaultValue("Motor")]
    string PolicyType = "Motor"
);
