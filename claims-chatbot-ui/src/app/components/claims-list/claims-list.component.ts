import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ClaimsApiService } from '../../services/claims-api.service';
import { ClaimAuditRecord } from '../../models/claim.model';

@Component({
  selector: 'app-claims-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './claims-list.component.html',
  styleUrls: ['./claims-list.component.scss']
})
export class ClaimsListComponent implements OnInit {
  claims: ClaimAuditRecord[] = [];
  filteredClaims: ClaimAuditRecord[] = [];
  selectedStatus: string = 'All';
  loading: boolean = false;
  errorMessage: string = '';

  statusOptions = [
    { value: 'All', label: 'All Claims' },
    { value: 'Covered', label: 'Covered' },
    { value: 'Not Covered', label: 'Not Covered' },
    { value: 'Manual Review', label: 'Manual Review' }
  ];

  constructor(
    private claimsApiService: ClaimsApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadClaims();
  }

  loadClaims(): void {
    this.loading = true;
    this.errorMessage = '';

    this.claimsApiService.getAllClaims().subscribe({
      next: (claims) => {
        this.claims = claims;
        this.filterClaims();
        this.loading = false;
      },
      error: (error) => {
        this.errorMessage = 'Failed to load claims. Please try again.';
        console.error('Error loading claims:', error);
        this.loading = false;
      }
    });
  }

  onStatusChange(): void {
    this.filterClaims();
  }

  filterClaims(): void {
    if (this.selectedStatus === 'All') {
      this.filteredClaims = this.claims;
    } else {
      this.filteredClaims = this.claims.filter(
        claim => claim.decisionStatus === this.selectedStatus
      );
    }
  }

  viewClaimDetails(claimId: string): void {
    this.router.navigate(['/claims', claimId]);
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

  formatDate(date: string): string {
    return new Date(date).toLocaleString();
  }
}
