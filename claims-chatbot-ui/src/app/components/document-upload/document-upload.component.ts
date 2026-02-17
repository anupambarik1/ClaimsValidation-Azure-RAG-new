import { Component, Output, EventEmitter, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { ClaimsApiService } from '../../services/claims-api.service';
import { ChatService } from '../../services/chat.service';
import { DocumentType, SubmitDocumentResponse, DocumentUploadResult } from '../../models/claim.model';

@Component({
  selector: 'app-document-upload',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatChipsModule
  ],
  templateUrl: './document-upload.component.html',
  styleUrls: ['./document-upload.component.scss']
})
export class DocumentUploadComponent {
  @Input() mode: 'claim' | 'supporting' = 'claim'; // claim = extract claim, supporting = just upload
  @Output() documentSubmitted = new EventEmitter<SubmitDocumentResponse>();
  @Output() supportingDocsUploaded = new EventEmitter<DocumentUploadResult[]>();

  selectedFiles: File[] = [];
  documentType: DocumentType = DocumentType.ClaimForm;
  userId = '';
  isDragOver = false;
  isUploading = false;
  uploadProgress = 0;
  uploadedDocs: DocumentUploadResult[] = [];

  constructor(
    private apiService: ClaimsApiService,
    private chatService: ChatService
  ) {}

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      // In claim mode, only accept one file; in supporting mode, accept multiple
      if (this.mode === 'claim' && files.length > 1) {
        this.chatService.addBotMessage('⚠️ Please upload only ONE claim form at a time.');
        return;
      }
      Array.from(files).forEach(file => this.handleFile(file));
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      // In claim mode, only accept one file; in supporting mode, accept multiple
      if (this.mode === 'claim' && input.files.length > 1) {
        this.chatService.addBotMessage('⚠️ Please upload only ONE claim form at a time.');
        return;
      }
      Array.from(input.files).forEach(file => this.handleFile(file));
    }
  }

  private handleFile(file: File): void {
    // Validate file type
    const validTypes = ['application/pdf', 'image/jpeg', 'image/png', 'text/plain'];
    if (!validTypes.includes(file.type)) {
      this.chatService.addBotMessage('❌ Invalid file type. Please upload PDF, JPG, PNG, or TXT files only.');
      return;
    }

    // Validate file size (10MB max)
    const maxSize = 10 * 1024 * 1024;
    if (file.size > maxSize) {
      this.chatService.addBotMessage('❌ File size exceeds 10MB limit.');
      return;
    }

    // In claim mode, replace file; in supporting mode, add to list
    if (this.mode === 'claim') {
      this.selectedFiles = [file];
    } else {
      // Check for duplicates
      if (!this.selectedFiles.find(f => f.name === file.name && f.size === file.size)) {
        this.selectedFiles.push(file);
      }
    }
  }

  removeFile(event: Event, index?: number): void {
    event.stopPropagation();
    if (index !== undefined) {
      this.selectedFiles.splice(index, 1);
    } else {
      this.selectedFiles = [];
    }
    this.uploadProgress = 0;
  }

  uploadDocument(): void {
    if (this.selectedFiles.length === 0) return;

    if (this.mode === 'claim') {
      // Claim mode: extract claim data from single document
      this.uploadClaimDocument();
    } else {
      // Supporting mode: just upload documents without extraction
      this.uploadSupportingDocuments();
    }
  }

  private uploadClaimDocument(): void {
    const file = this.selectedFiles[0];
    this.isUploading = true;
    this.uploadProgress = 10;

    this.chatService.addUserMessage(
      `Uploading claim form: ${file.name}`,
      'document',
      { fileName: file.name, documentType: DocumentType.ClaimForm }
    );

    const progressInterval = setInterval(() => {
      if (this.uploadProgress < 90 && this.isUploading) {
        this.uploadProgress += 10;
      }
    }, 500);

    this.apiService.submitDocument(
      file,
      this.userId || undefined,
      DocumentType.ClaimForm
    ).subscribe({
      next: (response) => {
        clearInterval(progressInterval);
        this.uploadProgress = 100;
        this.isUploading = false;
        this.documentSubmitted.emit(response);
        this.reset();
      },
      error: (error) => {
        clearInterval(progressInterval);
        this.isUploading = false;
        this.uploadProgress = 0;
        const errorMsg = error.error?.details || error.error?.error || error.message || 'Unknown error occurred';
        this.chatService.addBotMessage(
          `❌ Upload failed: ${errorMsg}`
        );
      }
    });
  }

  private uploadSupportingDocuments(): void {
    this.isUploading = true;
    this.uploadProgress = 10;
    this.uploadedDocs = [];

    this.chatService.addUserMessage(
      `Uploading ${this.selectedFiles.length} supporting document(s)...`,
      'document'
    );

    const progressInterval = setInterval(() => {
      if (this.uploadProgress < 90 && this.isUploading) {
        this.uploadProgress += 10;
      }
    }, 500);

    let completed = 0;
    const total = this.selectedFiles.length;

    this.selectedFiles.forEach(file => {
      this.apiService.uploadDocument(file, this.userId || undefined).subscribe({
        next: (result) => {
          this.uploadedDocs.push(result);
          completed++;
          
          if (completed === total) {
            clearInterval(progressInterval);
            this.uploadProgress = 100;
            this.isUploading = false;
            this.supportingDocsUploaded.emit(this.uploadedDocs);
            this.chatService.addBotMessage(
              `✅ Successfully uploaded ${total} supporting document(s)!`
            );
            this.reset();
          }
        },
        error: (error) => {
          completed++;
          const errorMsg = error.error?.details || error.error?.error || error.message || 'Unknown error';
          this.chatService.addBotMessage(
            `❌ Failed to upload ${file.name}: ${errorMsg}`
          );
          
          if (completed === total) {
            clearInterval(progressInterval);
            this.isUploading = false;
            this.uploadProgress = 0;
            if (this.uploadedDocs.length > 0) {
              this.supportingDocsUploaded.emit(this.uploadedDocs);
            }
          }
        }
      });
    });
  }

  cancel(): void {
    this.reset();
  }

  private reset(): void {
    this.selectedFiles = [];
    this.uploadedDocs = [];
    this.documentType = DocumentType.ClaimForm;
    this.userId = '';
    this.uploadProgress = 0;
    this.isUploading = false;
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  }
}
