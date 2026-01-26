import { Component, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ChatService } from '../../services/chat.service';
import { ClaimsApiService } from '../../services/claims-api.service';
import { Observable } from 'rxjs';
import { ChatMessage, ClaimRequest, SubmitDocumentResponse } from '../../models/claim.model';
import { DocumentUploadComponent } from '../document-upload/document-upload.component';
import { ClaimFormComponent } from '../claim-form/claim-form.component';
import { ClaimResultComponent } from '../claim-result/claim-result.component';

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
    ClaimResultComponent
  ],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements AfterViewChecked {
  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;

  messages$: Observable<ChatMessage[]>;
  userMessage = '';
  isLoading = false;
  private shouldScroll = false;

  constructor(
    private chatService: ChatService,
    private apiService: ClaimsApiService
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
        this.chatService.addBotMessage(
          `Claim Validation Result:\n\n` +
          `Decision: ${result.isApproved ? '✅ APPROVED' : '❌ DENIED'}\n` +
          `Confidence: ${(result.confidenceScore * 100).toFixed(1)}%\n` +
          `Requires Review: ${result.requiresHumanReview ? 'Yes' : 'No'}\n\n` +
          `Reasoning:\n${result.reasoning}`,
          'result',
          result
        );
        this.shouldScroll = true;
      },
      error: (error) => {
        this.isLoading = false;
        this.chatService.addBotMessage(
          `❌ Error validating claim: ${error.error?.details || error.message}`,
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

  private scrollToBottom(): void {
    try {
      this.messagesContainer.nativeElement.scrollTop = this.messagesContainer.nativeElement.scrollHeight;
    } catch (err) { }
  }
}
