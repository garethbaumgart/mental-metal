import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { catchError, EMPTY, finalize, tap } from 'rxjs';
import { Briefing } from '../../shared/models/briefing.model';
import { BriefingService } from '../../shared/services/briefing.service';
import { MarkdownPipe } from '../../shared/pipes/markdown.pipe';

@Component({
  selector: 'app-weekly-briefing-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, DatePipe, RouterLink, MarkdownPipe],
  template: `
    <div class="flex flex-col gap-6 max-w-3xl mx-auto">
      <header class="flex flex-col gap-2">
        <h1 class="text-2xl font-bold">Weekly briefing</h1>
        <p class="text-sm text-muted-color">
          Milestones, overdue items, and people needing your attention this week.
        </p>
      </header>

      <section class="flex flex-col gap-4 p-5 rounded-md bg-surface-50">
        @if (loading() && !briefing()) {
          <div class="flex items-center gap-2 py-6 justify-center">
            <i class="pi pi-spinner pi-spin"></i>
            <span class="text-sm text-muted-color">Generating this week's briefing…</span>
          </div>
        } @else if (providerNotConfigured()) {
          <div class="flex flex-col items-start gap-2 py-4">
            <p class="text-sm text-muted-color">
              Configure your AI provider to generate weekly briefings.
            </p>
            <a
              routerLink="/settings"
              class="text-sm font-medium text-primary hover:underline"
            >Open settings</a>
          </div>
        } @else if (errorMessage(); as msg) {
          <div class="flex flex-col items-start gap-2 py-4">
            <p class="text-sm text-muted-color">{{ msg }}</p>
            <p-button label="Retry" icon="pi pi-refresh" size="small" (onClick)="regenerate()" />
          </div>
        } @else if (briefing(); as b) {
          <div class="flex items-center justify-between">
            <p class="text-xs text-muted-color">
              Generated {{ b.generatedAtUtc | date: 'medium' }} · {{ b.model }}
            </p>
            <p-button
              label="Regenerate"
              icon="pi pi-refresh"
              severity="secondary"
              size="small"
              [text]="true"
              (onClick)="regenerate()"
              [loading]="loading()"
            />
          </div>
          <article
            class="briefing-body max-w-none"
            [innerHTML]="b.markdownBody | markdown"
          ></article>
        }
      </section>
    </div>
  `,
  styles: [
    `
      .briefing-body :where(h1, h2, h3) {
        font-weight: 600;
        margin-top: 0.75rem;
      }
      .briefing-body h2 {
        font-size: 1rem;
      }
      .briefing-body p {
        margin: 0.25rem 0;
      }
      .briefing-body ul,
      .briefing-body ol {
        padding-left: 1.25rem;
        margin: 0.25rem 0;
      }
      .briefing-body li {
        list-style: disc;
      }
      .briefing-body ol li {
        list-style: decimal;
      }
    `,
  ],
})
export class WeeklyBriefingPage implements OnInit {
  private readonly briefingService = inject(BriefingService);

  protected readonly briefing = signal<Briefing | null>(null);
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly providerNotConfigured = signal(false);

  ngOnInit(): void {
    this.load(false);
  }

  regenerate(): void {
    this.load(true);
  }

  private load(force: boolean): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.providerNotConfigured.set(false);
    this.briefingService
      .loadWeekly(force)
      .pipe(
        tap((b) => this.briefing.set(b)),
        catchError((err: unknown) => {
          this.handleError(err);
          return EMPTY;
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe();
  }

  private handleError(err: unknown): void {
    if (err instanceof HttpErrorResponse) {
      const code = (err.error as { code?: string } | null)?.code;
      if (err.status === 409 && code === 'ai_provider_not_configured') {
        this.providerNotConfigured.set(true);
        return;
      }
    }
    this.errorMessage.set('Failed to generate briefing.');
  }
}
