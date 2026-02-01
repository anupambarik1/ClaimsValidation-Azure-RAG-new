import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-role-selection',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './role-selection.component.html',
  styleUrls: ['./role-selection.component.scss']
})
export class RoleSelectionComponent {
  constructor(private router: Router) {}

  selectRole(role: 'claimant' | 'specialist'): void {
    if (role === 'claimant') {
      this.router.navigate(['/chat']);
    } else if (role === 'specialist') {
      this.router.navigate(['/claims']);
    }
  }
}
