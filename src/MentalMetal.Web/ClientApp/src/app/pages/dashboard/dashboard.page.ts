import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TodaysCommitmentsWidgetComponent } from './todays-commitments-widget.component';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    TodaysCommitmentsWidgetComponent,
  ],
  template: `
    <div class="flex flex-col gap-4 max-w-5xl mx-auto">
      <header class="flex flex-col gap-1">
        <h1 class="text-2xl font-bold">Dashboard</h1>
        <p class="text-sm text-muted-color">Your day at a glance.</p>
      </header>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <app-todays-commitments-widget />
      </div>
    </div>
  `,
})
export class DashboardPage {}
