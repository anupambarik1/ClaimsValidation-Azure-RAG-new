import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ClaimRequest,
  ClaimDecision,
  ClaimAuditRecord,
  DocumentUploadResult,
  SubmitDocumentResponse,
  ClaimExtractionResult,
  DocumentType,
  ClaimDecisionUpdate,
  FinalizeClaimRequest,
  BlobMetadata,
  DocumentUrlResponse
} from '../models/claim.model';

@Injectable({
  providedIn: 'root'
})
export class ClaimsApiService {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) { }

  // Validate claim using RAG
  validateClaim(claim: ClaimRequest): Observable<ClaimDecision> {
    return this.http.post<ClaimDecision>(`${this.baseUrl}/claims/validate`, claim);
  }

  // Finalize claim with supporting documents
  finalizeClaim(request: FinalizeClaimRequest): Observable<ClaimDecision> {
    return this.http.post<ClaimDecision>(`${this.baseUrl}/claims/finalize`, request);
  }

  // Upload document only
  uploadDocument(file: File, userId?: string): Observable<DocumentUploadResult> {
    const formData = new FormData();
    formData.append('file', file);
    if (userId) {
      formData.append('userId', userId);
    }
    
    return this.http.post<DocumentUploadResult>(`${this.baseUrl}/documents/upload`, formData);
  }

  // Extract claim data from uploaded document
  extractFromDocument(documentId: string, documentType: DocumentType): Observable<ClaimExtractionResult> {
    return this.http.post<ClaimExtractionResult>(`${this.baseUrl}/documents/extract`, {
      documentId,
      documentType
    });
  }

  // Upload and extract in one call
  submitDocument(file: File, userId?: string, documentType: DocumentType = DocumentType.ClaimForm): Observable<SubmitDocumentResponse> {
    const formData = new FormData();
    formData.append('file', file);
    if (userId) {
      formData.append('userId', userId);
    }
    formData.append('documentType', documentType);
    
    return this.http.post<SubmitDocumentResponse>(`${this.baseUrl}/documents/submit`, formData);
  }

  // Delete document
  deleteDocument(documentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/documents/${documentId}`);
  }

  // Search for claim by Claim ID
  searchByClaimId(claimId: string): Observable<ClaimAuditRecord> {
    return this.http.get<ClaimAuditRecord>(`${this.baseUrl}/claims/search/${claimId}`);
  }

  // Search for claims by Policy Number
  searchByPolicyNumber(policyNumber: string): Observable<ClaimAuditRecord[]> {
    return this.http.get<ClaimAuditRecord[]>(`${this.baseUrl}/claims/search/policy/${policyNumber}`);
  }

  // Get all claims with optional status filter
  getAllClaims(status?: string): Observable<ClaimAuditRecord[]> {
    if (status) {
      return this.http.get<ClaimAuditRecord[]>(`${this.baseUrl}/claims/list`, {
        params: { status }
      });
    }
    return this.http.get<ClaimAuditRecord[]>(`${this.baseUrl}/claims/list`);
  }

  // Get single claim details by ID
  getClaimById(claimId: string): Observable<ClaimAuditRecord> {
    return this.http.get<ClaimAuditRecord>(`${this.baseUrl}/claims/${claimId}`);
  }

  // Update claim decision by specialist
  updateClaimDecision(claimId: string, update: ClaimDecisionUpdate): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/claims/${claimId}/decision`, update);
  }

  // Get all documents associated with a claim
  getClaimDocuments(claimId: string): Observable<BlobMetadata[]> {
    return this.http.get<BlobMetadata[]>(`${this.baseUrl}/claims/${claimId}/documents`);
  }

  // Get secure download URL for a document
  getDocumentUrl(documentId: string): Observable<DocumentUrlResponse> {
    return this.http.get<DocumentUrlResponse>(`${this.baseUrl}/documents/${documentId}/url`);
  }
}
