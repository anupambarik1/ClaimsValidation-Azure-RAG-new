namespace ClaimsRagBot.Core.Models;

/// <summary>
/// Represents a policy clause retrieved from the knowledge base
/// </summary>
public record PolicyClause(
    /// <summary>
    /// Unique identifier for the policy clause
    /// </summary>
    /// <example>CLAUSE-3.2.1</example>
    string ClauseId,
    
    /// <summary>
    /// Full text of the policy clause
    /// </summary>
    string Text,
    
    /// <summary>
    /// Type of coverage this clause relates to
    /// </summary>
    /// <example>Collision</example>
    string CoverageType,
    
    /// <summary>
    /// Relevance score from vector search (0.0 to 1.0)
    /// </summary>
    /// <example>0.87</example>
    float Score
);
