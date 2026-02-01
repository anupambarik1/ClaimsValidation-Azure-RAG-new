import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatButtonModule } from '@angular/material/button';
import { ClaimRequest } from '../../models/claim.model';

@Component({
  selector: 'app-claim-result',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatChipsModule,
    MatDividerModule,
    MatButtonModule
  ],
  templateUrl: './claim-result.component.html',
  styleUrls: ['./claim-result.component.scss']
})
export class ClaimResultComponent {
  @Input() result: any;
  @Output() confirmAndSubmit = new EventEmitter<ClaimRequest>();
  @Output() editClaim = new EventEmitter<ClaimRequest>();

  getStatusIcon(): string {
    // Handle document validation status
    if (this.result.validationStatus) {
      switch (this.result.validationStatus) {
        case 'ReadyForSubmission':
          return 'check_circle';
        case 'ReadyForReview':
          return 'warning';
        case 'RequiresCorrection':
          return 'error';
        default:
          return 'info';
      }
    }
    
    // Handle claim decision status
    if (this.result.status) {
      switch (this.result.status) {
        case 'Covered':
          return 'check_circle';
        case 'Manual Review':
          return 'warning';
        case 'Not Covered':
          return 'error';
        default:
          return 'info';
      }
    }
    
    return 'info';
  }

  getFieldConfidence(field: string): number {
    return this.result.extractionResult?.fieldConfidences?.[field] || 0;
  }
  
  get isClaimDecision(): boolean {
    return !!this.result.status;
  }
  
  get isDocumentResult(): boolean {
    return !!this.result.validationStatus;
  }
  
  onConfirmAndSubmit(): void {
    if (this.result.extractionResult?.extractedClaim) {
      this.confirmAndSubmit.emit(this.result.extractionResult.extractedClaim);
    }
  }
  
  onEditClaim(): void {
    if (this.result.extractionResult?.extractedClaim) {
      this.editClaim.emit(this.result.extractionResult.extractedClaim);
    }
  }
  
  get canSubmit(): boolean {
    return this.result.validationStatus === 'ReadyForSubmission' || 
           this.result.validationStatus === 'ReadyForReview';
  }
}
