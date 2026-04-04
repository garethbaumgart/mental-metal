import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', loadComponent: () => import('./pages/placeholder.page').then(m => m.PlaceholderPage), data: { title: 'Dashboard' } },
  { path: 'capture', loadComponent: () => import('./pages/placeholder.page').then(m => m.PlaceholderPage), data: { title: 'Capture' } },
  { path: 'people', loadComponent: () => import('./pages/placeholder.page').then(m => m.PlaceholderPage), data: { title: 'People' } },
  { path: 'initiatives', loadComponent: () => import('./pages/placeholder.page').then(m => m.PlaceholderPage), data: { title: 'Initiatives' } },
  { path: 'queue', loadComponent: () => import('./pages/placeholder.page').then(m => m.PlaceholderPage), data: { title: 'My Queue' } },
  { path: '**', redirectTo: 'dashboard' },
];
