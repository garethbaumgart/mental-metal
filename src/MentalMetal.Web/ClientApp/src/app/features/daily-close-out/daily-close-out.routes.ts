import { Routes } from '@angular/router';
import { authGuard } from '../../shared/guards/auth.guard';

export const DAILY_CLOSE_OUT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./daily-close-out-page.component').then((m) => m.DailyCloseOutPageComponent),
    canActivate: [authGuard],
    data: { title: 'Close-out' },
  },
];
