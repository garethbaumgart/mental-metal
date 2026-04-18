import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { SkeletonModule } from 'primeng/skeleton';
import { BriefingService } from '../../shared/services/briefing.service';
import { WeeklyBrief } from '../../shared/models/briefing.model';
import { MarkdownPipe } from '../../shared/pipes/markdown.pipe';

@Component({
  selector: 'app-weekly-brief',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    ButtonModule,
    TagModule,
    SkeletonModule,
    MarkdownPipe,
  ],
  template: `
    <div class="max-w-3xl mx-auto flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold">Weekly Brief</h1>
          <p class="text-sm text-muted-color">Cross-conversation intelligence for the week</p>
        </div>
        <p-button
          icon="pi pi-refresh"
          label="Refresh"
          severity="secondary"
          [outlined]="true"
          size="small"
          (onClick)="refresh()"
          [disabled]="loading()"
        />
      </div>

      @if (loading()) {
        <div class="flex flex-col gap-4">
          <p-skeleton height="1.5rem" width="60%" />
          <p-skeleton height="1rem" />
          <p-skeleton height="1rem" width="90%" />
          <p-skeleton height="1rem" width="80%" />
          <p-skeleton height="1rem" width="85%" />
          <p-skeleton height="1rem" width="70%" />
          <p-skeleton height="2rem" class="mt-4" />
          <p-skeleton height="1rem" width="60%" />
          <p-skeleton height="1rem" width="50%" />
          <p-skeleton height="2rem" class="mt-4" />
          <p-skeleton height="1rem" width="70%" />
        </div>
      } @else if (brief()) {
        <!-- Date Range -->
        <p class="text-sm text-muted-color">
          {{ brief()!.dateRange.start | date: 'mediumDate' }} &mdash; {{ brief()!.dateRange.end | date: 'mediumDate' }}
        </p>

        <!-- Narrative -->
        <section class="p-4 rounded bg-surface-50">
          <div [innerHTML]="brief()!.narrative | markdown"></div>
        </section>

        <!-- Cross-Conversation Insights -->
        @if (brief()!.crossConversationInsights.length > 0) {
          <section class="flex flex-col gap-3">
            <h2 class="text-lg font-semibold">Cross-Conversation Patterns</h2>
            <ul class="flex flex-col gap-2">
              @for (insight of brief()!.crossConversationInsights; track insight) {
                <li class="p-3 rounded bg-surface-50 text-sm">{{ insight }}</li>
              }
            </ul>
          </section>
        }

        <!-- Key Decisions -->
        @if (brief()!.decisions.length > 0) {
          <section class="flex flex-col gap-3">
            <h2 class="text-lg font-semibold">Key Decisions</h2>
            <ul class="flex flex-col gap-2">
              @for (d of brief()!.decisions; track d) {
                <li class="p-3 rounded bg-surface-50 text-sm">{{ d }}</li>
              }
            </ul>
          </section>
        }

        <!-- Commitment Tracker -->
        <section class="flex flex-col gap-3">
          <h2 class="text-lg font-semibold">Commitment Tracker</h2>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
            <div class="p-4 rounded bg-surface-50 text-center">
              <div class="text-2xl font-bold">{{ brief()!.commitmentStatus.newCount }}</div>
              <div class="text-xs text-muted-color">New</div>
            </div>
            <div class="p-4 rounded bg-surface-50 text-center">
              <div class="text-2xl font-bold">{{ brief()!.commitmentStatus.completedCount }}</div>
              <div class="text-xs text-muted-color">Completed</div>
            </div>
            <div class="p-4 rounded bg-surface-50 text-center">
              <div class="text-2xl font-bold">{{ brief()!.commitmentStatus.overdueCount }}</div>
              <div class="text-xs text-muted-color">Overdue</div>
            </div>
            <div class="p-4 rounded bg-surface-50 text-center">
              <div class="text-2xl font-bold">{{ brief()!.commitmentStatus.totalOpen }}</div>
              <div class="text-xs text-muted-color">Total Open</div>
            </div>
          </div>
        </section>

        <!-- Risks -->
        @if (brief()!.risks.length > 0) {
          <section class="flex flex-col gap-3">
            <h2 class="text-lg font-semibold">Risks &amp; Open Threads</h2>
            <ul class="flex flex-col gap-2">
              @for (r of brief()!.risks; track r) {
                <li class="p-3 rounded bg-surface-50 text-sm">{{ r }}</li>
              }
            </ul>
          </section>
        }

        <!-- Initiative Activity -->
        @if (brief()!.initiativeActivity.length > 0) {
          <section class="flex flex-col gap-3">
            <h2 class="text-lg font-semibold">Initiative Activity</h2>
            @for (i of brief()!.initiativeActivity; track i.initiativeId) {
              <div class="p-3 rounded bg-surface-50 flex flex-col gap-1">
                <div class="flex items-center justify-between">
                  <a [routerLink]="['/initiatives', i.initiativeId]" class="text-sm font-medium text-primary">
                    {{ i.title }}
                  </a>
                  <p-tag [value]="i.captureCount + ' capture' + (i.captureCount > 1 ? 's' : '')" severity="info" />
                </div>
                @if (i.autoSummary) {
                  <p class="text-sm text-muted-color">{{ i.autoSummary }}</p>
                }
              </div>
            }
          </section>
        }

        <p class="text-xs text-muted-color">
          Generated {{ brief()!.generatedAt | date: 'medium' }}
        </p>
      } @else if (error()) {
        <div class="p-8 text-center rounded bg-surface-50">
          <p class="text-muted-color">{{ error() }}</p>
          <p-button label="Try Again" size="small" [outlined]="true" (onClick)="load()" class="mt-4" />
        </div>
      }
    </div>
  `,
})
export class WeeklyBriefComponent implements OnInit {
  private readonly briefingService = inject(BriefingService);

  readonly brief = signal<WeeklyBrief | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.briefingService.getWeeklyBrief().subscribe({
      next: (b) => {
        this.brief.set(b);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to generate weekly brief. Is your AI provider configured?');
      },
    });
  }

  protected refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.briefingService.refreshWeeklyBrief().subscribe({
      next: (b) => {
        this.brief.set(b);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to refresh weekly brief.');
      },
    });
  }
}
