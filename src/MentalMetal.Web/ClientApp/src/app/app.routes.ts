import { Routes } from '@angular/router';
import { authGuard } from './shared/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login/login.page').then(m => m.LoginPage) },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', loadComponent: () => import('./pages/placeholder.page').then(m => m.PlaceholderPage), canActivate: [authGuard], data: { title: 'Dashboard' } },
  { path: 'capture', loadComponent: () => import('./pages/placeholder.page').then(m => m.PlaceholderPage), canActivate: [authGuard], data: { title: 'Capture' } },
  { path: 'people', loadComponent: () => import('./pages/placeholder.page').then(m => m.PlaceholderPage), canActivate: [authGuard], data: { title: 'People' } },
  { path: 'initiatives', loadComponent: () => import('./pages/placeholder.page').then(m => m.PlaceholderPage), canActivate: [authGuard], data: { title: 'Initiatives' } },
  { path: 'queue', loadComponent: () => import('./pages/placeholder.page').then(m => m.PlaceholderPage), canActivate: [authGuard], data: { title: 'My Queue' } },
  { path: 'settings', loadComponent: () => import('./pages/settings/settings.page').then(m => m.SettingsPage), canActivate: [authGuard], data: { title: 'Settings' } },
  { path: '**', redirectTo: 'dashboard' },
];
