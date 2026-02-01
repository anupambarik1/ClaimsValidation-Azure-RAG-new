import { Component, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subscription } from 'rxjs';
import { ClaimRequest } from '../../models/claim.model';
import { ClaimDataService } from '../../services/claim-data.service';

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
export class ClaimFormComponent implements OnInit, OnDestroy {
  @Output() claimSubmitted = new EventEmitter<ClaimRequest>();

  claimForm: FormGroup;
  isSubmitting = false;
  private claimDataSubscription?: Subscription;

  constructor(
    private fb: FormBuilder,
    private claimDataService: ClaimDataService
  ) {
    this.claimForm = this.fb.group({
      policyNumber: ['', [Validators.required]],
      policyType: ['Motor', [Validators.required]],
      claimAmount: ['', [Validators.required, Validators.min(1)]],
      claimDescription: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(1000)]]
    });
  }

  ngOnInit(): void {
    // Subscribe to claim data updates
    this.claimDataSubscription = this.claimDataService.claimToEdit$.subscribe(claim => {
      if (claim) {
        this.populateForm(claim);
        // Clear the claim data after populating
        setTimeout(() => this.claimDataService.clearClaimToEdit(), 100);
      }
    });
  }

  ngOnDestroy(): void {
    if (this.claimDataSubscription) {
      this.claimDataSubscription.unsubscribe();
    }
  }

  private populateForm(claim: ClaimRequest): void {
    this.claimForm.patchValue({
      policyNumber: claim.policyNumber,
      policyType: claim.policyType,
      claimAmount: claim.claimAmount,
      claimDescription: claim.claimDescription
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
