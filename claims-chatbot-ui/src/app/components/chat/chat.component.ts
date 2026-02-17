import { Component, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTabsModule, MatTabGroup } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ChatService } from '../../services/chat.service';
import { ClaimDataService } from '../../services/claim-data.service';
import { ClaimsApiService } from '../../services/claims-api.service';
import { Observable } from 'rxjs';
import { ChatMessage, ClaimRequest, SubmitDocumentResponse, DocumentUploadResult } from '../../models/claim.model';
import { DocumentUploadComponent } from '../document-upload/document-upload.component';
import { ClaimFormComponent } from '../claim-form/claim-form.component';
import { ClaimResultComponent } from '../claim-result/claim-result.component';
import { ClaimSearchComponent } from '../claim-search/claim-search.component';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatTabsModule,
    MatTooltipModule,
    DocumentUploadComponent,
    ClaimFormComponent,
    ClaimResultComponent,
    ClaimSearchComponent
  ],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements AfterViewChecked {
  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;
  @ViewChild('tabGroup') private tabGroup!: MatTabGroup;

  messages$: Observable<ChatMessage[]>;
  userMessage = '';
  isLoading = false;
  private shouldScroll = false;
  
  // Track pending claim and supporting documents
  pendingClaim: ClaimRequest | null = null;
  supportingDocuments: string[] = [];
  awaitingSupportingDocs = false;

  constructor(
    private chatService: ChatService,
    private apiService: ClaimsApiService,
    private claimDataService: ClaimDataService
  ) {
    this.messages$ = this.chatService.messages$;
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  handleEnter(event: Event): void {
    const keyboardEvent = event as KeyboardEvent;
    if (!keyboardEvent.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  sendMessage(): void {
    if (!this.userMessage.trim()) return;

    this.chatService.addUserMessage(this.userMessage);
    this.shouldScroll = true;

    // Simple bot response for general queries
    this.chatService.addBotMessage(
      'I can help you with claim validation and document processing. Please use the tabs below to:\n' +
      '• Upload a claim document for automatic extraction\n' +
      '• Fill out the claim form manually\n' +
      '• Or ask me specific questions about claims'
    );
    this.shouldScroll = true;

    this.userMessage = '';
  }

  handleDocumentSubmit(response: SubmitDocumentResponse): void {
    this.isLoading = false;
    
    // This only handles claim form extraction
    this.chatService.addBotMessage(
      `Document processed successfully!\n\n` +
      `Validation Status: ${response.validationStatus}\n` +
      `Overall Confidence: ${(response.extractionResult.overallConfidence * 100).toFixed(1)}%\n` +
      `Next Action: ${response.nextAction}`,
      'result',
      response
    );
    
    this.shouldScroll = true;
  }
  
  handleSupportingDocsUpload(docs: DocumentUploadResult[]): void {
    // Add document IDs to supporting documents list
    docs.forEach(doc => {
      if (doc.documentId) {
        this.supportingDocuments.push(doc.documentId);
      }
    });
    
    this.chatService.addBotMessage(
      `📄 ${docs.length} supporting document(s) uploaded successfully!\n\n` +
      `Total documents: ${this.supportingDocuments.length}\n\n` +
      `You can upload more documents or click "Finalize Claim" in the header to complete submission.`,
      'text'
    );
    
    this.shouldScroll = true;
  }
  
  finalizeClaim(): void {
    if (!this.pendingClaim) {
      this.chatService.addBotMessage(
        '❌ No pending claim to finalize. Please submit a claim first.',
        'text'
      );
      return;
    }
    
    this.isLoading = true;
    this.shouldScroll = true;
    
    this.chatService.addUserMessage(
      `Finalizing claim with ${this.supportingDocuments.length} supporting document(s)...`,
      'text'
    );
    
    this.apiService.finalizeClaim({
      claimData: this.pendingClaim,
      supportingDocumentIds: this.supportingDocuments.length > 0 ? this.supportingDocuments : undefined
    }).subscribe({
      next: (result) => {
        this.isLoading = false;
        
        const isApproved = result.status === 'Covered';
        const requiresReview = result.status === 'Manual Review';
        const statusIcon = isApproved ? '✅' : requiresReview ? '⚠️' : '❌';
        
        let message = `🎉 Claim Finalized!\n\n` +
          `Decision: ${statusIcon} ${result.status.toUpperCase()}\n` +
          `Confidence: ${(result.confidenceScore * 100).toFixed(1)}%\n`;
        
        if (result.explanation) {
          message += `\nExplanation:\n${result.explanation}`;
        }
        
        if (result.clauseReferences && result.clauseReferences.length > 0) {
          message += `\n\nRelevant Policy Clauses:\n` + result.clauseReferences.map(c => `• ${c}`).join('\n');
        }
        
        message += `\n\n✅ Your claim has been submitted and saved in our system.`;
        
        this.chatService.addBotMessage(message, 'result', result);
        
        // Reset the workflow state
        this.pendingClaim = null;
        this.supportingDocuments = [];
        this.awaitingSupportingDocs = false;
        
        this.shouldScroll = true;
      },
      error: (error) => {
        this.isLoading = false;
        const errorMsg = error.error?.details || error.error?.error || error.message || 'Unknown error occurred';
        this.chatService.addBotMessage(
          `❌ Error finalizing claim: ${errorMsg}`,
          'text'
        );
        this.shouldScroll = true;
      }
    });
  }
  
  cancelPendingClaim(): void {
    this.pendingClaim = null;
    this.supportingDocuments = [];
    this.awaitingSupportingDocs = false;
    
    this.chatService.addBotMessage(
      '❌ Pending claim cancelled. You can start a new claim submission.',
      'text'
    );
    this.shouldScroll = true;
  }

  handleClaimSubmit(claim: ClaimRequest): void {
    this.isLoading = true;
    this.shouldScroll = true;

    this.chatService.addUserMessage(
      `Validating claim:\nPolicy: ${claim.policyNumber}\nType: ${claim.policyType}\nAmount: $${claim.claimAmount.toLocaleString()}`,
      'claim',
      claim
    );

    this.apiService.validateClaim(claim).subscribe({
      next: (result) => {
        this.isLoading = false;
        
        // Map backend response to UI-friendly format
        const isApproved = result.status === 'Covered';
        const requiresReview = result.status === 'Manual Review';
        const statusIcon = isApproved ? '✅' : requiresReview ? '⚠️' : '❌';
        
        let message = `Claim Validation Result:\n\n` +
          `Decision: ${statusIcon} ${result.status.toUpperCase()}\n` +
          `Confidence: ${(result.confidenceScore * 100).toFixed(1)}%\n`;
        
        if (result.explanation) {
          message += `\nExplanation:\n${result.explanation}`;
        }
        
        if (result.clauseReferences && result.clauseReferences.length > 0) {
          message += `\n\nRelevant Policy Clauses:\n` + result.clauseReferences.map(c => `• ${c}`).join('\n');
        }
        
        if (result.requiredDocuments && result.requiredDocuments.length > 0) {
          message += `\n\nRequired Documents:\n` + result.requiredDocuments.map(d => `• ${d}`).join('\n');
          
          // Store claim and ask for supporting documents
          this.pendingClaim = claim;
          this.awaitingSupportingDocs = true;
          this.supportingDocuments = [];
          
          message += `\n\n📎 Please upload the required supporting documents using the "Upload Document" tab.\n` +
                     `Once all documents are uploaded, click "Finalize Claim" to complete the submission.`;
        } else {
          // No supporting documents required, claim is complete
          message += `\n\n✅ No additional documents required. Claim validation is complete.`;
        }
        
        this.chatService.addBotMessage(message, 'result', result);
        this.shouldScroll = true;
      },
      error: (error) => {
        this.isLoading = false;
        const errorMsg = error.error?.details || error.error?.error || error.message || 'Unknown error occurred';
        this.chatService.addBotMessage(
          `❌ Error validating claim: ${errorMsg}`,
          'text'
        );
        this.shouldScroll = true;
      }
    });
  }

  clearChat(): void {
    this.chatService.clearChat();
    this.shouldScroll = true;
  }
  
  handleConfirmAndSubmit(claim: ClaimRequest): void {
    this.chatService.addBotMessage(
      '✅ Submitting extracted claim for validation...',
      'text'
    );
    this.shouldScroll = true;
    
    // Submit the extracted claim for validation
    this.handleClaimSubmit(claim);
  }
  
  handleEditClaim(claim: ClaimRequest): void {
    // Set the claim data for the form to pick up
    this.claimDataService.setClaimToEdit(claim);
    
    // Switch to the Claim Form tab (index 2)
    if (this.tabGroup) {
      this.tabGroup.selectedIndex = 2;
    }
    
    this.chatService.addBotMessage(
      '📝 Switched to Claim Form. Please review and edit the extracted details, then submit.',
      'text'
    );
    this.shouldScroll = true;
  }

  private scrollToBottom(): void {
    try {
      this.messagesContainer.nativeElement.scrollTop = this.messagesContainer.nativeElement.scrollHeight;
    } catch (err) { }
  }
}
