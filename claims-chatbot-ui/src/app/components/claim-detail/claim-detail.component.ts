import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ClaimsApiService } from '../../services/claims-api.service';
import { ClaimAuditRecord, ClaimDecisionUpdate } from '../../models/claim.model';

@Component({
  selector: 'app-claim-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './claim-detail.component.html',
  styleUrls: ['./claim-detail.component.scss']
})
export class ClaimDetailComponent implements OnInit {
  claim: ClaimAuditRecord | null = null;
  claimId: string = '';
  loading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  // Specialist decision form
  showDecisionForm: boolean = false;
  decisionUpdate: ClaimDecisionUpdate = {
    newStatus: '',
    specialistNotes: '',
    specialistId: ''
  };

  statusOptions = [
    { value: 'Covered', label: 'Covered' },
    { value: 'Not Covered', label: 'Not Covered' },
    { value: 'Manual Review', label: 'Manual Review' }
  ];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private claimsApiService: ClaimsApiService
  ) {}

  ngOnInit(): void {
    this.claimId = this.route.snapshot.paramMap.get('id') || '';
    if (this.claimId) {
      this.loadClaimDetails();
    } else {
      this.errorMessage = 'Invalid claim ID';
    }
  }

  loadClaimDetails(): void {
    this.loading = true;
    this.errorMessage = '';

    this.claimsApiService.getClaimById(this.claimId).subscribe({
      next: (claim) => {
        this.claim = claim;
        this.decisionUpdate.newStatus = claim.decisionStatus;
        this.loading = false;
      },
      error: (error) => {
        this.errorMessage = 'Failed to load claim details. Please try again.';
        console.error('Error loading claim:', error);
        this.loading = false;
      }
    });
  }

  toggleDecisionForm(): void {
    this.showDecisionForm = !this.showDecisionForm;
    if (this.showDecisionForm) {
      this.successMessage = '';
    }
  }

  submitDecision(): void {
    if (!this.decisionUpdate.specialistId.trim()) {
      this.errorMessage = 'Specialist ID is required';
      return;
    }

    if (!this.decisionUpdate.specialistNotes.trim()) {
      this.errorMessage = 'Specialist notes are required';
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.claimsApiService.updateClaimDecision(this.claimId, this.decisionUpdate).subscribe({
      next: () => {
        this.successMessage = 'Claim decision updated successfully!';
        this.showDecisionForm = false;
        this.loading = false;
        
        // Reload claim details to show updated information
        setTimeout(() => {
          this.loadClaimDetails();
        }, 1500);
      },
      error: (error) => {
        this.errorMessage = 'Failed to update claim decision. Please try again.';
        console.error('Error updating claim:', error);
        this.loading = false;
      }
    });
  }

  cancelDecision(): void {
    this.showDecisionForm = false;
    this.decisionUpdate = {
      newStatus: this.claim?.decisionStatus || '',
      specialistNotes: '',
      specialistId: ''
    };
    this.errorMessage = '';
  }

  goBack(): void {
    this.router.navigate(['/claims']);
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Covered':
        return 'status-covered';
      case 'Not Covered':
        return 'status-not-covered';
      case 'Manual Review':
        return 'status-manual-review';
      default:
        return '';
    }
  }

  formatDate(date: string | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleString();
  }
}
