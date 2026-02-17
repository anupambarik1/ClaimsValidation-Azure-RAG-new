export interface ClaimRequest {
  policyNumber: string;
  claimDescription: string;
  claimAmount: number;
  policyType: 'Motor' | 'Home' | 'Health' | 'Life';
}

export interface ClaimDecision {
  status: string;  // "Covered", "Not Covered", "Manual Review"
  explanation: string;
  clauseReferences: string[];
  requiredDocuments: string[];
  confidenceScore: number;
}

// Helper properties for UI
export interface ClaimDecisionUI extends ClaimDecision {
  isApproved: boolean;
  requiresHumanReview: boolean;
}

export interface ClaimAuditRecord {
  claimId: string;
  timestamp: string;
  policyNumber: string;
  claimAmount: number;
  claimDescription: string;
  decisionStatus: string;  // "Covered", "Not Covered", "Manual Review"
  explanation: string;
  confidenceScore: number;
  clauseReferences: string[];
  requiredDocuments: string[];
  
  // Additional fields from inserted data
  claimantName?: string;
  claimType?: string;
  incidentDate?: string;
  reasons?: string[];
  
  // Specialist review fields
  specialistNotes?: string;
  specialistId?: string;
  reviewedAt?: string;
}

export interface ClaimDecisionUpdate {
  newStatus: string;
  specialistNotes: string;
  specialistId: string;
}

export interface FinalizeClaimRequest {
  claimData: ClaimRequest;
  supportingDocumentIds?: string[];
  notes?: string;
}

export interface PolicyClause {
  clauseId: string;
  content: string;
  similarity: number;
}

export interface DocumentUploadResult {
  documentId: string;
  s3Bucket: string;
  s3Key: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
}

export interface ClaimExtractionResult {
  extractedClaim: ClaimRequest;
  overallConfidence: number;
  fieldConfidences: { [key: string]: number };
  ambiguousFields: string[];
  rawExtractedData: any;
}

export interface SubmitDocumentResponse {
  uploadResult: DocumentUploadResult;
  extractionResult: ClaimExtractionResult;
  validationStatus: 'ReadyForSubmission' | 'ReadyForReview' | 'RequiresCorrection';
  nextAction: string;
}

export enum DocumentType {
  ClaimForm = 'ClaimForm',
  PoliceReport = 'PoliceReport',
  RepairEstimate = 'RepairEstimate',
  DamagePhotos = 'DamagePhotos',
  MedicalRecords = 'MedicalRecords',
  Mixed = 'Mixed'
}

export interface ChatMessage {
  id: string;
  content: string;
  sender: 'user' | 'bot';
  timestamp: Date;
  type: 'text' | 'document' | 'claim' | 'result';
  data?: any;
}
