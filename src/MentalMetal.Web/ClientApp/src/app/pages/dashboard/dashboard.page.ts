import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MorningBriefingWidgetComponent } from './morning-briefing-widget.component';
import { TodaysCommitmentsWidgetComponent } from './todays-commitments-widget.component';
import { TodaysOneOnOnesWidgetComponent } from './todays-one-on-ones-widget.component';
import { TopOfQueueWidgetComponent } from './top-of-queue-widget.component';
import { OverdueSummaryWidgetComponent } from './overdue-summary-widget.component';

/**
 * Dashboard shell: responsive grid that composes independent widgets so
 * the page is useful even when any one widget (including the AI briefing)
 * fails. See openspec/specs/daily-weekly-briefing/spec.md and
 * openspec/changes/dashboard-shell/ for the isolation contract.
 */
@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MorningBriefingWidgetComponent,
    TodaysCommitmentsWidgetComponent,
    TodaysOneOnOnesWidgetComponent,
    TopOfQueueWidgetComponent,
    OverdueSummaryWidgetComponent,
  ],
  template: `
    <div class="flex flex-col gap-4 max-w-5xl mx-auto">
      <header class="flex flex-col gap-1">
        <h1 class="text-2xl font-bold">Today</h1>
        <p class="text-sm text-muted-color">Your day at a glance.</p>
      </header>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Briefing anchors the top, full width -->
        <div class="lg:col-span-2">
          <app-morning-briefing-widget />
        </div>

        <app-todays-commitments-widget />
        <app-todays-one-on-ones-widget />
        <app-top-of-queue-widget />

        <!-- Overdue summary spans full width as a one-line glance. The
             lg:col-span-2 must live on the grid child (this wrapper),
             not inside the component host. -->
        <div class="lg:col-span-2">
          <app-overdue-summary-widget />
        </div>
      </div>
    </div>
  `,
})
export class DashboardPage {}
