import { Routes } from '@angular/router';
import { authGuard } from '../../shared/guards/auth.guard';

export const NUDGES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./nudges-list.page').then(m => m.NudgesListPageComponent),
    canActivate: [authGuard],
    data: { title: 'Nudges' },
  },
];
