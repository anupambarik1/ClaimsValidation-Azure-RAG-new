import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';

@Component({
  selector: 'app-claim-result',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatChipsModule,
    MatDividerModule
  ],
  templateUrl: './claim-result.component.html',
  styleUrls: ['./claim-result.component.scss']
})
export class ClaimResultComponent {
  @Input() result: any;

  getStatusIcon(): string {
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

  getFieldConfidence(field: string): number {
    return this.result.extractionResult?.fieldConfidences?.[field] || 0;
  }
}
