import { Routes } from '@angular/router';
import { authGuard } from './shared/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login/login.page').then(m => m.LoginPage) },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard.page').then(m => m.DashboardPage), canActivate: [authGuard], data: { title: 'Dashboard' } },
  { path: 'capture', loadComponent: () => import('./pages/captures/captures-list/captures-list.component').then(m => m.CapturesListComponent), canActivate: [authGuard], data: { title: 'Capture' } },
  { path: 'capture/:id', loadComponent: () => import('./pages/captures/capture-detail/capture-detail.component').then(m => m.CaptureDetailComponent), canActivate: [authGuard], data: { title: 'Capture Detail' } },
  { path: 'people', loadComponent: () => import('./pages/people/people-list/people-list.component').then(m => m.PeopleListComponent), canActivate: [authGuard], data: { title: 'People' } },
  { path: 'people/:id', loadComponent: () => import('./pages/people/person-detail/person-detail.component').then(m => m.PersonDetailComponent), canActivate: [authGuard], data: { title: 'Person Detail' } },
  { path: 'initiatives', loadComponent: () => import('./pages/initiatives/initiatives-list/initiatives-list.component').then(m => m.InitiativesListComponent), canActivate: [authGuard], data: { title: 'Initiatives' } },
  { path: 'initiatives/:id', loadComponent: () => import('./pages/initiatives/initiative-detail/initiative-detail.component').then(m => m.InitiativeDetailComponent), canActivate: [authGuard], data: { title: 'Initiative Detail' } },
  { path: 'commitments', loadComponent: () => import('./pages/commitments/commitments-list/commitments-list.component').then(m => m.CommitmentsListComponent), canActivate: [authGuard], data: { title: 'Commitments' } },
  { path: 'commitments/:id', loadComponent: () => import('./pages/commitments/commitment-detail/commitment-detail.component').then(m => m.CommitmentDetailComponent), canActivate: [authGuard], data: { title: 'Commitment Detail' } },
  { path: 'settings', loadComponent: () => import('./pages/settings/settings.page').then(m => m.SettingsPage), canActivate: [authGuard], data: { title: 'Settings' } },
  { path: '**', redirectTo: 'dashboard' },
];
