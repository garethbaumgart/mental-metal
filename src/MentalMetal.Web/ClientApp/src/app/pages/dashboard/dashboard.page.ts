import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MorningBriefingWidgetComponent } from './morning-briefing-widget.component';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MorningBriefingWidgetComponent],
  template: `
    <div class="flex flex-col gap-6 max-w-3xl mx-auto">
      <header class="flex flex-col gap-2">
        <h1 class="text-2xl font-bold">Dashboard</h1>
        <p class="text-sm text-muted-color">Your day at a glance.</p>
      </header>

      <app-morning-briefing-widget />
    </div>
  `,
})
export class DashboardPage {}
