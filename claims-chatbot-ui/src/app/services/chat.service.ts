import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ChatMessage } from '../models/claim.model';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private messagesSubject = new BehaviorSubject<ChatMessage[]>([]);
  public messages$ = this.messagesSubject.asObservable();

  constructor() {
    // Add welcome message
    this.addBotMessage('Welcome to the Claims Assistant! 👋\n\nI\'m here to help you with claim processing and validation. Use the tabs above to get started, or type your questions below.');
  }

  addUserMessage(content: string, type: ChatMessage['type'] = 'text', data?: any): void {
    const message: ChatMessage = {
      id: this.generateId(),
      content,
      sender: 'user',
      timestamp: new Date(),
      type,
      data
    };
    this.messagesSubject.next([...this.messagesSubject.value, message]);
  }

  addBotMessage(content: string, type: ChatMessage['type'] = 'text', data?: any): void {
    const message: ChatMessage = {
      id: this.generateId(),
      content,
      sender: 'bot',
      timestamp: new Date(),
      type,
      data
    };
    this.messagesSubject.next([...this.messagesSubject.value, message]);
  }

  clearChat(): void {
    this.messagesSubject.next([]);
    this.addBotMessage('Chat cleared. How can I help you?');
  }

  private generateId(): string {
    return `msg-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  getMessages(): ChatMessage[] {
    return this.messagesSubject.value;
  }
}
