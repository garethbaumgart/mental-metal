import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { catchError, finalize, forkJoin, of, tap } from 'rxjs';
import { OneOnOnesService } from '../../shared/services/one-on-ones.service';
import { PeopleService } from '../../shared/services/people.service';
import { OneOnOne } from '../../shared/models/one-on-one.model';
import { Person } from '../../shared/models/person.model';
import { isToday } from './widget-shell';

interface OneOnOneRow {
  id: string;
  personId: string;
  personName: string;
  occurredAt: string;
}

/**
 * Lists the 1:1s scheduled (OccurredAt) for today. Links each row to the
 * person detail page for 30-second prep. Fails in isolation.
 */
@Component({
  selector: 'app-todays-one-on-ones-widget',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ButtonModule],
  template: `
    <section
      class="flex flex-col gap-3 p-5 rounded-md bg-surface-50"
      aria-label="Today's 1:1s"
    >
      <header class="flex items-center justify-between">
        <h2 class="text-lg font-semibold">Today's 1:1s</h2>
        <a
          routerLink="/one-on-ones"
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
      } @else if (rows().length === 0) {
        <p class="text-sm text-muted-color py-2">No 1:1s scheduled today.</p>
      } @else {
        <ul class="flex flex-col gap-2">
          @for (row of rows(); track row.id) {
            <li class="p-2 rounded bg-surface-0">
              <a
                [routerLink]="['/people', row.personId]"
                class="flex items-center gap-3 text-sm hover:underline"
              >
                <i class="pi pi-comments text-muted-color" aria-hidden="true"></i>
                <span class="flex-1 font-medium">{{ row.personName }}</span>
              </a>
            </li>
          }
        </ul>
      }
    </section>
  `,
})
export class TodaysOneOnOnesWidgetComponent implements OnInit {
  private readonly oneOnOnes = inject(OneOnOnesService);
  private readonly people = inject(PeopleService);

  protected readonly rows = signal<OneOnOneRow[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    forkJoin({ oneOnOnes: this.oneOnOnes.list(), people: this.people.list() })
      .pipe(
        tap(({ oneOnOnes, people }) => {
          const byId = new Map<string, Person>(people.map((p) => [p.id, p]));
          const rows: OneOnOneRow[] = oneOnOnes
            .filter((o) => isToday(o.occurredAt))
            .sort((a, b) => a.occurredAt.localeCompare(b.occurredAt))
            .map((o: OneOnOne) => ({
              id: o.id,
              personId: o.personId,
              personName: byId.get(o.personId)?.name ?? '(unknown)',
              occurredAt: o.occurredAt,
            }));
          this.rows.set(rows);
        }),
        catchError(() => {
          this.error.set("Couldn't load 1:1s.");
          return of(null);
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe();
  }
}
