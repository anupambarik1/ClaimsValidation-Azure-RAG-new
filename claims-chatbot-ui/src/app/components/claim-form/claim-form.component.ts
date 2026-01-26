import { Component, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ClaimRequest } from '../../models/claim.model';

@Component({
  selector: 'app-claim-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './claim-form.component.html',
  styleUrls: ['./claim-form.component.scss']
})
export class ClaimFormComponent {
  @Output() claimSubmitted = new EventEmitter<ClaimRequest>();

  claimForm: FormGroup;
  isSubmitting = false;

  constructor(private fb: FormBuilder) {
    this.claimForm = this.fb.group({
      policyNumber: ['', [Validators.required]],
      policyType: ['Motor', [Validators.required]],
      claimAmount: ['', [Validators.required, Validators.min(1)]],
      claimDescription: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(1000)]]
    });
  }

  submitClaim(): void {
    if (this.claimForm.valid) {
      this.isSubmitting = true;
      const claim: ClaimRequest = {
        policyNumber: this.claimForm.value.policyNumber,
        policyType: this.claimForm.value.policyType,
        claimAmount: parseFloat(this.claimForm.value.claimAmount),
        claimDescription: this.claimForm.value.claimDescription
      };

      this.claimSubmitted.emit(claim);

      // Reset submitting state after a delay
      setTimeout(() => {
        this.isSubmitting = false;
      }, 1000);
    }
  }

  resetForm(): void {
    this.claimForm.reset({
      policyType: 'Motor'
    });
  }
}
