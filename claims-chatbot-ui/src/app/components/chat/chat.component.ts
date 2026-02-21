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

    const userInput = this.userMessage.trim();
    this.chatService.addUserMessage(userInput);
    this.shouldScroll = true;

    // Process the message and provide intelligent responses
    const response = this.processUserMessage(userInput);
    this.chatService.addBotMessage(response);
    this.shouldScroll = true;

    this.userMessage = '';
  }

  private processUserMessage(message: string): string {
    const lowerMessage = message.toLowerCase();

    // Help and general queries
    if (lowerMessage.includes('help') || lowerMessage.includes('what can you do') || lowerMessage.includes('how do i')) {
      return `I can help you with several claim-related tasks:

📄 **Document Processing:**
• Upload claim forms for automatic data extraction
• Upload supporting documents (medical records, receipts, etc.)
• View and download processed documents

📝 **Manual Entry:**
• Fill out claim forms manually if you prefer
• Edit extracted data from uploaded documents

🔍 **Claim Management:**
• Search for existing claims by ID or policy number
• View claim status and validation results
• Review specialist decisions

❓ **Questions:**
• Ask about claim requirements and procedures
• Get help with specific claim types
• Learn about document requirements

Try using the tabs above, or ask me a specific question!`;
    }

    // Document-related queries
    if (lowerMessage.includes('document') || lowerMessage.includes('upload') || lowerMessage.includes('file')) {
      return `For document processing, you have several options:

📤 **Upload Claim Form:** Use the "Upload Claim Form" tab to upload a claim document. I'll automatically extract the data and validate it.

📋 **Supporting Documents:** After submitting a claim, use "Upload Supporting Docs" to add medical records, receipts, or other evidence.

📁 **Document Types Supported:**
• PDF files
• Images (JPG, PNG)
• Scanned documents

The system will analyze your documents and extract relevant claim information automatically.`;
    }

    // Claim status queries
    if (lowerMessage.includes('status') || lowerMessage.includes('find') || lowerMessage.includes('search')) {
      return `To check claim status or search for claims:

🔍 **Search Claims Tab:** Use this to find claims by:
• Claim ID (exact match)
• Policy Number (shows all claims for that policy)

📊 **View All Claims:** The "Search Claims" tab also shows recent claims with their current status.

📋 **Status Types:**
• **Pending:** Awaiting specialist review
• **Approved:** Claim has been approved
• **Rejected:** Claim was denied
• **Under Review:** Currently being processed

Try the "Search Claims" tab to find what you're looking for!`;
    }

    // Manual entry queries
    if (lowerMessage.includes('manual') || lowerMessage.includes('fill') || lowerMessage.includes('form')) {
      return `For manual claim entry:

📝 **Manual Claim Entry Tab:** Use this when you prefer to type in claim details yourself instead of uploading a document.

✏️ **What You'll Need:**
• Policy number
• Patient/member information
• Service dates and details
• Diagnosis codes (if known)
• Amount claimed

The manual form includes validation to ensure all required fields are completed. You can also upload supporting documents after submitting the claim.`;
    }

    // Questions about claims
    if (lowerMessage.includes('what is') || lowerMessage.includes('explain') || lowerMessage.includes('tell me about')) {
      if (lowerMessage.includes('claim')) {
        return `A health insurance claim is a request for payment submitted to an insurance company when a covered person receives medical care or services.

📋 **Key Components:**
• **Patient Information:** Name, date of birth, relationship to policyholder
• **Provider Details:** Doctor/hospital name, location, specialty
• **Service Information:** Dates of service, procedures performed, diagnosis
• **Cost Information:** Charges, insurance responsibility, patient payments

📄 **Required Documents:**
• Itemized bill from provider
• Explanation of Benefits (EOB) from primary insurance
• Medical records supporting the claim
• Assignment of benefits form

I can help you submit claims using either document upload or manual entry!`;
      }

      if (lowerMessage.includes('policy') || lowerMessage.includes('coverage')) {
        return `Insurance policies define what services and treatments are covered by your health plan.

🔍 **Coverage Types:**
• **Inpatient:** Hospital stays
• **Outpatient:** Doctor visits, tests, procedures
• **Prescription Drugs:** Medications
• **Preventive Care:** Annual checkups, screenings
• **Emergency Services:** ER visits, ambulance

📞 **To Check Coverage:** You'll need your policy number. I can help you search for claims under a specific policy using the "Search Claims" tab.`;
      }
    }

    // Greeting responses
    if (lowerMessage.includes('hello') || lowerMessage.includes('hi') || lowerMessage.includes('hey')) {
      return `Hi there! 👋 Welcome to the Claims Assistant!

I'm designed to help you with all aspects of claim processing. Here's what I can do for you:

🔄 **Process Claims:** Upload documents or fill forms manually
🔍 **Find Claims:** Search by ID or policy number  
📋 **Get Status:** Check approval status and decisions
❓ **Answer Questions:** Ask about procedures, requirements, or coverage

Try asking me specific questions like:
• "How do I upload a document?"
• "What is a claim?"
• "How do I check claim status?"
• "What documents do I need?"

Or use the tabs above to get started right away!`;
    }

    // Default response for unrecognized queries
    return `I understand you're asking about "${message}". While I don't have a specific answer for that, I can definitely help you with:

📄 **Document Processing:** Upload claim forms, medical records, or receipts
📝 **Manual Entry:** Fill out claim details yourself
🔍 **Claim Search:** Find claims by ID or policy number
📊 **Status Updates:** Check approval status and specialist decisions
💬 **General Questions:** Ask about claim procedures, coverage, or requirements

Try one of these:
• "How do I upload documents?"
• "What documents are needed for a claim?"
• "How do I check claim status?"
• "What is claim validation?"

Or use the tabs above for specific actions!`;
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
