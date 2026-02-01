import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ClaimRequest } from '../models/claim.model';

@Injectable({
  providedIn: 'root'
})
export class ClaimDataService {
  private claimToEditSubject = new BehaviorSubject<ClaimRequest | null>(null);
  public claimToEdit$: Observable<ClaimRequest | null> = this.claimToEditSubject.asObservable();

  setClaimToEdit(claim: ClaimRequest): void {
    this.claimToEditSubject.next(claim);
  }

  clearClaimToEdit(): void {
    this.claimToEditSubject.next(null);
  }

  getCurrentClaim(): ClaimRequest | null {
    return this.claimToEditSubject.value;
  }
}
