namespace ClaimsRagBot.Core.Models;

public enum DocumentType
{
    ClaimForm,           // Standard insurance claim form
    PoliceReport,        // Accident/incident report
    RepairEstimate,      // Mechanic's or contractor's estimate
    DamagePhotos,        // Images of damage
    MedicalRecords,      // For injury/health claims
    Mixed                // Multiple document types
}
