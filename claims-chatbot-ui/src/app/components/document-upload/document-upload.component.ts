import { Component, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ClaimsApiService } from '../../services/claims-api.service';
import { ChatService } from '../../services/chat.service';
import { DocumentType, SubmitDocumentResponse } from '../../models/claim.model';

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
    MatProgressSpinnerModule
  ],
  templateUrl: './document-upload.component.html',
  styleUrls: ['./document-upload.component.scss']
})
export class DocumentUploadComponent {
  @Output() documentSubmitted = new EventEmitter<SubmitDocumentResponse>();

  selectedFile: File | null = null;
  documentType: DocumentType = DocumentType.ClaimForm;
  userId = '';
  isDragOver = false;
  isUploading = false;
  uploadProgress = 0;

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
      this.handleFile(files[0]);
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
      this.handleFile(input.files[0]);
    }
  }

  private handleFile(file: File): void {
    // Validate file type
    const validTypes = ['application/pdf', 'image/jpeg', 'image/png'];
    if (!validTypes.includes(file.type)) {
      this.chatService.addBotMessage('❌ Invalid file type. Please upload PDF, JPG, or PNG files only.');
      return;
    }

    // Validate file size (10MB max)
    const maxSize = 10 * 1024 * 1024;
    if (file.size > maxSize) {
      this.chatService.addBotMessage('❌ File size exceeds 10MB limit.');
      return;
    }

    this.selectedFile = file;
  }

  removeFile(event: Event): void {
    event.stopPropagation();
    this.selectedFile = null;
    this.uploadProgress = 0;
  }

  uploadDocument(): void {
    if (!this.selectedFile) return;

    this.isUploading = true;
    this.uploadProgress = 10;

    this.chatService.addUserMessage(
      `Uploading document: ${this.selectedFile.name}`,
      'document',
      { fileName: this.selectedFile.name, documentType: this.documentType }
    );

    this.apiService.submitDocument(
      this.selectedFile,
      this.userId || undefined,
      this.documentType
    ).subscribe({
      next: (response) => {
        this.uploadProgress = 100;
        this.isUploading = false;
        this.documentSubmitted.emit(response);
        this.reset();
      },
      error: (error) => {
        this.isUploading = false;
        this.uploadProgress = 0;
        this.chatService.addBotMessage(
          `❌ Upload failed: ${error.error?.details || error.message}`
        );
      }
    });

    // Simulate progress for user feedback
    const progressInterval = setInterval(() => {
      if (this.uploadProgress < 90) {
        this.uploadProgress += 10;
      } else {
        clearInterval(progressInterval);
      }
    }, 500);
  }

  cancel(): void {
    this.reset();
  }

  private reset(): void {
    this.selectedFile = null;
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
