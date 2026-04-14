import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { catchError, EMPTY, finalize, tap } from 'rxjs';
import { Briefing } from '../../shared/models/briefing.model';
import { BriefingService } from '../../shared/services/briefing.service';
import { MarkdownPipe } from '../../shared/pipes/markdown.pipe';

@Component({
  selector: 'app-one-on-one-prep-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, DialogModule, DatePipe, RouterLink, MarkdownPipe],
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [style]="{ width: '720px', maxWidth: '95vw' }"
      [draggable]="false"
      header="1:1 prep sheet"
    >
      <ng-template pTemplate="content">
        @if (loading() && !briefing()) {
          <div class="flex items-center gap-2 py-6 justify-center">
            <i class="pi pi-spinner pi-spin"></i>
            <span class="text-sm text-muted-color">Generating prep sheet…</span>
          </div>
        } @else if (providerNotConfigured()) {
          <div class="flex flex-col items-start gap-2 py-4">
            <p class="text-sm text-muted-color">
              Configure your AI provider to generate 1:1 prep sheets.
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
          <div class="flex flex-col gap-2">
            <p class="text-xs text-muted-color">
              Generated {{ b.generatedAtUtc | date: 'medium' }} · {{ b.model }}
            </p>
            <article
              class="briefing-body"
              [innerHTML]="b.markdownBody | markdown"
            ></article>
          </div>
        }
      </ng-template>
      <ng-template pTemplate="footer">
        <p-button
          label="Regenerate"
          icon="pi pi-refresh"
          severity="secondary"
          [text]="true"
          (onClick)="regenerate()"
          [disabled]="loading()"
        />
        <p-button label="Close" (onClick)="close()" />
      </ng-template>
    </p-dialog>
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
export class OneOnOnePrepDialogComponent {
  private readonly briefingService = inject(BriefingService);

  readonly visible = input.required<boolean>();
  readonly personId = input.required<string>();
  readonly visibleChange = output<boolean>();

  protected readonly briefing = signal<Briefing | null>(null);
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly providerNotConfigured = signal(false);

  // Tracks whether we've already kicked off an auto-load for the current open
  // session of the dialog so the effect doesn't re-fire on signal churn.
  private autoLoadTriggered = false;

  constructor() {
    // Auto-generate when the dialog becomes visible for the first time per
    // session. Reset the latch when the dialog closes so reopening will load
    // again on demand. Avoid reading mutable signals (loading/briefing) inside
    // the effect so the run isn't re-scheduled when load() flips them.
    effect(() => {
      const isVisible = this.visible();
      if (isVisible && !this.autoLoadTriggered) {
        this.autoLoadTriggered = true;
        this.load(false);
      } else if (!isVisible) {
        this.autoLoadTriggered = false;
      }
    });
  }

  regenerate(): void {
    this.load(true);
  }

  close(): void {
    this.visibleChange.emit(false);
  }

  onVisibleChange(value: boolean): void {
    if (!value) this.visibleChange.emit(false);
  }

  private load(force: boolean): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.providerNotConfigured.set(false);
    this.briefingService
      .loadOneOnOnePrep(this.personId(), force)
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
    this.errorMessage.set('Failed to generate prep sheet.');
  }
}
