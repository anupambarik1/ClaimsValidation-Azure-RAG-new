import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideRouter, Routes } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { RoleSelectionComponent } from './components/role-selection/role-selection.component';
import { ChatComponent } from './components/chat/chat.component';
import { ClaimsListComponent } from './components/claims-list/claims-list.component';
import { ClaimDetailComponent } from './components/claim-detail/claim-detail.component';

const routes: Routes = [
  { path: '', redirectTo: '/role-selection', pathMatch: 'full' },
  { path: 'role-selection', component: RoleSelectionComponent },
  { path: 'chat', component: ChatComponent },
  { path: 'claims', component: ClaimsListComponent },
  { path: 'claims/:id', component: ClaimDetailComponent }
];

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideAnimations(),
    provideHttpClient(withInterceptorsFromDi())
  ]
};
