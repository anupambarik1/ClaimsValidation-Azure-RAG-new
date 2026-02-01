import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink],
  template: `
    <div class="app-container">
      <nav class="app-nav">
        <h1>Claims RAG Bot</h1>
        <div class="nav-links">
          <a routerLink="/chat" routerLinkActive="active">Submit Claim</a>
          <a routerLink="/claims" routerLinkActive="active">Claims Dashboard</a>
        </div>
      </nav>
      <main class="app-content">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .app-container {
      min-height: 100vh;
      background-color: #f5f5f5;
    }

    .app-nav {
      background-color: #2c3e50;
      color: white;
      padding: 1rem 2rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .app-nav h1 {
      margin: 0;
      font-size: 1.5rem;
      font-weight: 600;
    }

    .nav-links {
      display: flex;
      gap: 1.5rem;
    }

    .nav-links a {
      color: white;
      text-decoration: none;
      padding: 0.5rem 1rem;
      border-radius: 4px;
      transition: background-color 0.2s;
    }

    .nav-links a:hover {
      background-color: rgba(255, 255, 255, 0.1);
    }

    .nav-links a.active {
      background-color: #4CAF50;
    }

    .app-content {
      min-height: calc(100vh - 80px);
    }
  `]
})
export class AppComponent {
  title = 'Claims Management System';
}
