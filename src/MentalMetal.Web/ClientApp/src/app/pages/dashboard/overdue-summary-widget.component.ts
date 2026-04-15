import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of, tap } from 'rxjs';
import { CommitmentsService } from '../../shared/services/commitments.service';
import { DelegationsService } from '../../shared/services/delegations.service';

interface SummaryCount {
  loaded: boolean;
  failed: boolean;
  value: number;
}

const emptyCount = (): SummaryCount => ({ loaded: false, failed: false, value: 0 });

/**
 * Single-row glance of overdue work across sources. Each source tracks
 * its own status so a failure in one is shown as "—" while the others
 * keep rendering live values — the isolation contract the dashboard
 * shell promises.
 *
 * Unread nudges is stubbed at 0 until a GET /api/nudges/unread-count (or
 * equivalent) endpoint is available (tracked in the change's design.md).
 */
@Component({
  selector: 'app-overdue-summary-widget',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <section
      class="flex flex-col gap-3 p-5 rounded-md bg-surface-50 lg:col-span-2"
      aria-label="Overdue summary"
    >
      <h2 class="text-lg font-semibold">What's slipping</h2>
      <p class="text-sm flex flex-wrap items-center gap-x-2">
        <a
          routerLink="/commitments"
          class="text-primary hover:underline font-medium"
        >{{ display(commitments()) }} commitments overdue</a>
        <span class="text-muted-color">·</span>
        <a
          routerLink="/delegations"
          class="text-primary hover:underline font-medium"
        >{{ display(delegations()) }} delegations stale</a>
        <span class="text-muted-color">·</span>
        <a
          routerLink="/nudges"
          class="text-primary hover:underline font-medium"
        >{{ display(nudges()) }} unread nudges</a>
      </p>
    </section>
  `,
})
export class OverdueSummaryWidgetComponent implements OnInit {
  private readonly commitmentsService = inject(CommitmentsService);
  private readonly delegationsService = inject(DelegationsService);

  protected readonly commitments = signal<SummaryCount>(emptyCount());
  protected readonly delegations = signal<SummaryCount>(emptyCount());
  // Nudges currently has no unread-count endpoint; render "0" until one
  // is added. Marked loaded so the UI doesn't show a placeholder.
  protected readonly nudges = signal<SummaryCount>({ loaded: true, failed: false, value: 0 });

  ngOnInit(): void {
    this.loadCommitments();
    this.loadDelegations();
  }

  protected display(c: SummaryCount): string {
    if (c.failed) return '—';
    if (!c.loaded) return '…';
    return String(c.value);
  }

  private loadCommitments(): void {
    this.commitmentsService
      .list(undefined, 'Open', undefined, undefined, true)
      .pipe(
        tap((list) => this.commitments.set({ loaded: true, failed: false, value: list.length })),
        catchError(() => {
          this.commitments.set({ loaded: true, failed: true, value: 0 });
          return of(null);
        }),
        finalize(() => undefined),
      )
      .subscribe();
  }

  private loadDelegations(): void {
    this.delegationsService
      .list()
      .pipe(
        tap((list) => {
          // "Stale" = active delegation (not completed) that is either
          // past its due date or hasn't been followed up in >7 days.
          const sevenDaysAgoMs = Date.now() - 7 * 24 * 60 * 60 * 1000;
          const today = new Date().toISOString().slice(0, 10);
          const stale = list.filter((d) => {
            if (d.status === 'Completed') return false;
            if (d.dueDate && d.dueDate.slice(0, 10) < today) return true;
            if (d.lastFollowedUpAt) {
              return new Date(d.lastFollowedUpAt).getTime() < sevenDaysAgoMs;
            }
            // No follow-up yet — consider stale if created >7 days ago.
            return new Date(d.createdAt).getTime() < sevenDaysAgoMs;
          }).length;
          this.delegations.set({ loaded: true, failed: false, value: stale });
        }),
        catchError(() => {
          this.delegations.set({ loaded: true, failed: true, value: 0 });
          return of(null);
        }),
      )
      .subscribe();
  }
}
