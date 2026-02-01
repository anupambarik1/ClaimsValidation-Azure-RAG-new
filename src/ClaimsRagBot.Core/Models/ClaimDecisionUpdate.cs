namespace ClaimsRagBot.Core.Models;

/// <summary>
/// Request model for updating claim decision by specialist
/// </summary>
public record ClaimDecisionUpdate(
    string NewStatus,
    string SpecialistNotes,
    string SpecialistId
);
