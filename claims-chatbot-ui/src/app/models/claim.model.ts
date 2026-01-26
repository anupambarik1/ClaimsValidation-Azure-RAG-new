export interface ClaimRequest {
  policyNumber: string;
  claimDescription: string;
  claimAmount: number;
  policyType: 'Motor' | 'Home' | 'Health' | 'Life';
}

export interface ClaimDecision {
  isApproved: boolean;
  confidenceScore: number;
  reasoning: string;
  suggestedAmount?: number;
  requiresHumanReview: boolean;
  matchedClauses: PolicyClause[];
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
