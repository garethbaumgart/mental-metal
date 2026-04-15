import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { catchError, EMPTY, finalize, tap } from 'rxjs';
import { CommitmentsService } from '../../shared/services/commitments.service';
import { Commitment } from '../../shared/models/commitment.model';
import { isOnOrBeforeToday } from './widget-shell';

/**
 * Shows up to 5 open commitments that are either due today or already
 * overdue. Fails in isolation: an error here does not break the rest of
 * the dashboard.
 */
@Component({
  selector: 'app-todays-commitments-widget',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, RouterLink, ButtonModule],
  template: `
    <section
      class="flex flex-col gap-3 p-5 rounded-md bg-surface-50"
      aria-label="Today's commitments"
    >
      <header class="flex items-center justify-between">
        <h2 class="text-lg font-semibold">Today's commitments</h2>
        <a
          routerLink="/commitments"
          class="text-xs font-medium text-primary hover:underline"
        >View all</a>
      </header>

      @if (loading()) {
        <div class="flex items-center gap-2 py-4 justify-center">
          <i class="pi pi-spinner pi-spin"></i>
          <span class="text-sm text-muted-color">Loading…</span>
        </div>
      } @else if (error(); as msg) {
        <div class="flex flex-col items-start gap-2 py-3">
          <p class="text-sm text-muted-color">{{ msg }}</p>
          <p-button label="Retry" icon="pi pi-refresh" size="small" [text]="true" (onClick)="load()" />
        </div>
      } @else if (items().length === 0) {
        <p class="text-sm text-muted-color py-2">Nothing due today — clear inbox.</p>
      } @else {
        <ul class="flex flex-col gap-2">
          @for (c of items(); track c.id) {
            <li class="flex items-center gap-3 p-2 rounded bg-surface-0">
              @if (c.isOverdue) {
                <i class="pi pi-exclamation-circle text-red-500 text-sm" aria-label="Overdue"></i>
              } @else {
                <i class="pi pi-circle text-sm" aria-hidden="true"></i>
              }
              <span class="flex-1 text-sm">{{ c.description }}</span>
              @if (c.dueDate) {
                <span class="text-xs text-muted-color">{{ c.dueDate | date: 'mediumDate' }}</span>
              }
              <p-button
                icon="pi pi-check"
                severity="secondary"
                [text]="true"
                [rounded]="true"
                size="small"
                ariaLabel="Mark complete"
                (onClick)="complete(c)"
              />
            </li>
          }
        </ul>
      }
    </section>
  `,
})
export class TodaysCommitmentsWidgetComponent implements OnInit {
  private readonly commitments = inject(CommitmentsService);

  protected readonly items = signal<Commitment[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.commitments
      .list(undefined, 'Open')
      .pipe(
        tap((list) => {
          const relevant = list
            .filter((c) => c.isOverdue || isOnOrBeforeToday(c.dueDate))
            .sort((a, b) => {
              if (a.isOverdue !== b.isOverdue) return a.isOverdue ? -1 : 1;
              return (a.dueDate ?? '\uffff').localeCompare(b.dueDate ?? '\uffff');
            })
            .slice(0, 5);
          this.items.set(relevant);
        }),
        catchError(() => {
          this.error.set("Couldn't load commitments.");
          return EMPTY;
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe();
  }

  protected complete(c: Commitment): void {
    this.commitments.complete(c.id).subscribe({
      next: () => this.load(),
      error: () => this.error.set('Failed to mark complete.'),
    });
  }
}
