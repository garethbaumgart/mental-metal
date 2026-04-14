import { Routes } from '@angular/router';
import { authGuard } from '../../shared/guards/auth.guard';

export const MY_QUEUE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./my-queue.page').then((m) => m.MyQueuePageComponent),
    canActivate: [authGuard],
    data: { title: 'My Queue' },
  },
];
