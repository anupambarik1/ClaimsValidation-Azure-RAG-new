import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ClaimRequest,
  ClaimDecision,
  DocumentUploadResult,
  SubmitDocumentResponse,
  ClaimExtractionResult,
  DocumentType
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
}
