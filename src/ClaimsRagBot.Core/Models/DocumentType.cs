namespace ClaimsRagBot.Core.Models;

public enum DocumentType
{
    ClaimForm,           // Standard insurance claim form
    PoliceReport,        // Accident/incident report
    RepairEstimate,      // Mechanic's or contractor's estimate
    DamagePhotos,        // Images of damage
    MedicalRecords,      // For injury/health claims
    SupportingDocument,  // Generic supporting documents (bills, receipts, evidence)
    Mixed                // Multiple document types
}
