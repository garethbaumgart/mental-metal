import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { SkeletonModule } from 'primeng/skeleton';
import { BriefingService } from '../../shared/services/briefing.service';
import { DailyBrief } from '../../shared/models/briefing.model';
import { MarkdownPipe } from '../../shared/pipes/markdown.pipe';

@Component({
  selector: 'app-daily-brief',
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
          <h1 class="text-2xl font-bold">Daily Brief</h1>
          <p class="text-sm text-muted-color">Your morning intelligence summary</p>
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
        </div>
      } @else if (brief()) {
        <!-- Narrative -->
        <section class="p-4 rounded bg-surface-50">
          <div [innerHTML]="brief()!.narrative | markdown"></div>
        </section>

        <!-- Fresh Commitments -->
        @if (brief()!.freshCommitments.length > 0) {
          <section class="flex flex-col gap-3">
            <h2 class="text-lg font-semibold">New Commitments (from yesterday)</h2>
            @for (c of brief()!.freshCommitments; track c.id) {
              <div class="flex items-center gap-2 p-3 rounded bg-surface-50">
                <p-tag [value]="c.direction === 'MineToThem' ? 'Mine' : 'Theirs'" severity="secondary" />
                <span class="flex-1 text-sm">{{ c.description }}</span>
                @if (c.personName) {
                  <a [routerLink]="['/people', c.personId]" class="text-xs text-primary">{{ c.personName }}</a>
                }
              </div>
            }
          </section>
        }

        <!-- Due Today -->
        @if (brief()!.dueToday.length > 0) {
          <section class="flex flex-col gap-3">
            <h2 class="text-lg font-semibold">Due Today</h2>
            @for (c of brief()!.dueToday; track c.id) {
              <div class="flex items-center gap-2 p-3 rounded bg-surface-50">
                <p-tag [value]="c.direction === 'MineToThem' ? 'Mine' : 'Theirs'" severity="secondary" />
                <span class="flex-1 text-sm">{{ c.description }}</span>
                @if (c.personName) {
                  <a [routerLink]="['/people', c.personId]" class="text-xs text-primary">{{ c.personName }}</a>
                }
              </div>
            }
          </section>
        }

        <!-- Overdue -->
        @if (brief()!.overdue.length > 0) {
          <section class="flex flex-col gap-3">
            <h2 class="text-lg font-semibold">Overdue</h2>
            @for (c of brief()!.overdue; track c.id) {
              <div class="flex items-center gap-2 p-3 rounded bg-surface-50">
                <p-tag value="Overdue" severity="danger" />
                <p-tag [value]="c.direction === 'MineToThem' ? 'Mine' : 'Theirs'" severity="secondary" />
                <span class="flex-1 text-sm">{{ c.description }}</span>
                @if (c.dueDate) {
                  <span class="text-xs text-muted-color">Due {{ c.dueDate | date: 'mediumDate' }}</span>
                }
                @if (c.personName) {
                  <a [routerLink]="['/people', c.personId]" class="text-xs text-primary">{{ c.personName }}</a>
                }
              </div>
            }
          </section>
        }

        <!-- People Activity -->
        @if (brief()!.peopleActivity.length > 0) {
          <section class="flex flex-col gap-3">
            <h2 class="text-lg font-semibold">People Activity</h2>
            <div class="flex flex-wrap gap-2">
              @for (p of brief()!.peopleActivity; track p.personId) {
                <a [routerLink]="['/people', p.personId]"
                   class="flex items-center gap-2 p-2 rounded bg-surface-50 text-sm">
                  <span class="font-medium">{{ p.personName }}</span>
                  <p-tag [value]="p.mentionCount + ' mention' + (p.mentionCount > 1 ? 's' : '')" severity="info" />
                </a>
              }
            </div>
          </section>
        }

        <!-- Empty State -->
        @if (brief()!.captureCount === 0) {
          <div class="p-8 text-center rounded bg-surface-50">
            <i class="pi pi-inbox text-3xl text-muted-color"></i>
            <p class="mt-2 text-muted-color">No captures from yesterday. Add some meeting notes or transcripts to see your daily brief.</p>
          </div>
        }

        <p class="text-xs text-muted-color">
          {{ brief()!.captureCount }} capture(s) analyzed. Generated {{ brief()!.generatedAt | date: 'medium' }}
        </p>
      } @else if (error()) {
        <div class="p-8 text-center rounded bg-surface-50">
          <p class="text-muted-color">{{ error() }}</p>
          @if (aiNotConfigured()) {
            <a routerLink="/settings" class="inline-block mt-4 text-sm text-primary font-medium">Go to Settings</a>
          } @else {
            <p-button label="Try Again" size="small" [outlined]="true" (onClick)="load()" class="mt-4" />
          }
        </div>
      }
    </div>
  `,
})
export class DailyBriefComponent implements OnInit {
  private readonly briefingService = inject(BriefingService);

  readonly brief = signal<DailyBrief | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly aiNotConfigured = signal(false);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.aiNotConfigured.set(false);
    this.briefingService.getDailyBrief().subscribe({
      next: (b) => {
        this.brief.set(b);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        if (err.status === 422 && err.error?.code === 'ai.notConfigured') {
          this.aiNotConfigured.set(true);
          this.error.set(err.error.error);
        } else {
          this.error.set('Failed to generate daily brief.');
        }
      },
    });
  }

  protected refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.aiNotConfigured.set(false);
    this.briefingService.refreshDailyBrief().subscribe({
      next: (b) => {
        this.brief.set(b);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        if (err.status === 422 && err.error?.code === 'ai.notConfigured') {
          this.aiNotConfigured.set(true);
          this.error.set(err.error.error);
        } else {
          this.error.set('Failed to refresh daily brief.');
        }
      },
    });
  }
}
