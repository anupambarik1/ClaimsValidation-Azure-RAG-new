import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatRadioModule } from '@angular/material/radio';
import { ClaimsApiService } from '../../services/claims-api.service';
import { ClaimAuditRecord } from '../../models/claim.model';

@Component({
  selector: 'app-claim-search',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatChipsModule,
    MatRadioModule
  ],
  templateUrl: './claim-search.component.html',
  styleUrl: './claim-search.component.scss'
})
export class ClaimSearchComponent {
  searchForm: FormGroup;
  searchType: 'claimId' | 'policyNumber' = 'claimId';
  isLoading = false;
  searchResult: ClaimAuditRecord | null = null;
  searchResults: ClaimAuditRecord[] = [];
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private claimsApi: ClaimsApiService
  ) {
    this.searchForm = this.fb.group({
      searchValue: ['', Validators.required]
    });
  }

  onSearchTypeChange(type: 'claimId' | 'policyNumber') {
    this.searchType = type;
    this.clearResults();
    this.searchForm.reset();
  }

  onSearch() {
    if (this.searchForm.invalid) {
      return;
    }

    const searchValue = this.searchForm.value.searchValue.trim();
    this.isLoading = true;
    this.errorMessage = '';
    this.clearResults();

    if (this.searchType === 'claimId') {
      this.claimsApi.searchByClaimId(searchValue).subscribe({
        next: (result) => {
          this.searchResult = result;
          this.isLoading = false;
        },
        error: (error) => {
          this.errorMessage = error.error?.message || 'Claim not found';
          this.isLoading = false;
        }
      });
    } else {
      this.claimsApi.searchByPolicyNumber(searchValue).subscribe({
        next: (results) => {
          this.searchResults = results;
          if (results.length === 0) {
            this.errorMessage = 'No claims found for this policy number';
          }
          this.isLoading = false;
        },
        error: (error) => {
          this.errorMessage = 'Error searching claims';
          this.isLoading = false;
        }
      });
    }
  }

  clearResults() {
    this.searchResult = null;
    this.searchResults = [];
    this.errorMessage = '';
  }

  getStatusColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'covered':
        return 'primary';
      case 'not covered':
        return 'warn';
      case 'manual review':
        return 'accent';
      default:
        return '';
    }
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString();
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }
}
