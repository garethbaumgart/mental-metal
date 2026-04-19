import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { SkeletonModule } from 'primeng/skeleton';
import { BriefingService } from '../../shared/services/briefing.service';
import { CommitmentsService } from '../../shared/services/commitments.service';
import { PeopleService } from '../../shared/services/people.service';
import { DailyBrief } from '../../shared/models/briefing.model';
import { Commitment } from '../../shared/models/commitment.model';
import { Person } from '../../shared/models/person.model';
import { MarkdownPipe } from '../../shared/pipes/markdown.pipe';
import { toLocalDateKey, todayLocalIso } from './widget-shell';

@Component({
  selector: 'app-dashboard-page',
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
    <div class="flex flex-col gap-6 max-w-5xl mx-auto">
      <header class="flex flex-col gap-1">
        <h1 class="text-2xl font-bold">Dashboard</h1>
        <p class="text-sm text-muted-color">Your day at a glance.</p>
      </header>

      <!-- Daily Brief Section -->
      <section class="flex flex-col gap-4 p-5 rounded-md bg-surface-50" aria-label="Daily Brief">
        <header class="flex items-center justify-between">
          <h2 class="text-lg font-semibold">Daily Brief</h2>
          <a routerLink="/briefing/daily" class="text-xs font-medium text-primary hover:underline">
            View Full Brief
          </a>
        </header>

        @if (briefLoading()) {
          <div class="flex flex-col gap-3">
            <p-skeleton height="1.5rem" width="60%" />
            <p-skeleton height="1rem" />
            <p-skeleton height="1rem" width="90%" />
            <p-skeleton height="1rem" width="80%" />
          </div>
        } @else if (brief()) {
          <div class="text-sm" [innerHTML]="brief()!.narrative | markdown"></div>

          @if (brief()!.freshCommitments.length > 0) {
            <div class="flex flex-col gap-2">
              <h3 class="text-sm font-semibold">New Commitments</h3>
              @for (c of brief()!.freshCommitments; track c.id) {
                <div class="flex items-center gap-2 p-2 rounded bg-surface-0 text-sm">
                  <p-tag [value]="c.direction === 'MineToThem' ? 'Mine' : 'Theirs'" severity="secondary" />
                  <span class="flex-1">{{ c.description }}</span>
                  @if (c.personName) {
                    <a [routerLink]="['/people', c.personId]" class="text-xs text-primary">{{ c.personName }}</a>
                  }
                </div>
              }
            </div>
          }

          @if (brief()!.dueToday.length > 0) {
            <div class="flex flex-col gap-2">
              <h3 class="text-sm font-semibold">Due Today</h3>
              @for (c of brief()!.dueToday; track c.id) {
                <div class="flex items-center gap-2 p-2 rounded bg-surface-0 text-sm">
                  <span class="flex-1">{{ c.description }}</span>
                  @if (c.personName) {
                    <a [routerLink]="['/people', c.personId]" class="text-xs text-primary">{{ c.personName }}</a>
                  }
                </div>
              }
            </div>
          }

          @if (brief()!.overdue.length > 0) {
            <div class="flex flex-col gap-2">
              <h3 class="text-sm font-semibold">Overdue</h3>
              @for (c of brief()!.overdue; track c.id) {
                <div class="flex items-center gap-2 p-2 rounded bg-surface-0 text-sm">
                  <p-tag value="Overdue" severity="danger" />
                  <span class="flex-1">{{ c.description }}</span>
                  @if (c.dueDate) {
                    <span class="text-xs text-muted-color">Due {{ c.dueDate | date: 'mediumDate' }}</span>
                  }
                </div>
              }
            </div>
          }

          <div class="flex items-center justify-between">
            <p class="text-xs text-muted-color">{{ brief()!.captureCount }} capture(s) analyzed</p>
            <a routerLink="/briefing/weekly" class="text-xs font-medium text-primary hover:underline">
              View Weekly Brief
            </a>
          </div>
        } @else if (briefError()) {
          <div class="flex flex-col items-start gap-2 py-3">
            <p class="text-sm text-muted-color">{{ briefError() }}</p>
            <p-button label="Retry" icon="pi pi-refresh" size="small" [text]="true" (onClick)="loadBrief()" />
          </div>
        }
      </section>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Open Commitments Section -->
        <section class="flex flex-col gap-3 p-5 rounded-md bg-surface-50" aria-label="Open Commitments">
          <header class="flex items-center justify-between">
            <h2 class="text-lg font-semibold">Open Commitments</h2>
            <a routerLink="/commitments" class="text-xs font-medium text-primary hover:underline">
              View All
            </a>
          </header>

          @if (commitmentsLoading()) {
            <div class="flex flex-col gap-2">
              <p-skeleton height="2.5rem" />
              <p-skeleton height="2.5rem" />
              <p-skeleton height="2.5rem" />
            </div>
          } @else if (commitmentsError()) {
            <div class="flex flex-col items-start gap-2 py-3">
              <p class="text-sm text-muted-color">{{ commitmentsError() }}</p>
              <p-button label="Retry" icon="pi pi-refresh" size="small" [text]="true" (onClick)="loadCommitments()" />
            </div>
          } @else if (commitments().length === 0) {
            <p class="text-sm text-muted-color py-2">No open commitments.</p>
          } @else {
            @if (overdueCommitments().length > 0) {
              <div class="flex flex-col gap-1">
                <span class="text-xs font-semibold uppercase text-muted-color">Overdue</span>
                @for (c of overdueCommitments(); track c.id) {
                  <div class="flex items-center gap-2 p-2 rounded bg-surface-0">
                    <p-tag value="Overdue" severity="danger" />
                    <span class="flex-1 text-sm truncate">{{ c.description }}</span>
                    <p-button icon="pi pi-check" severity="success" [text]="true" [rounded]="true" size="small"
                      ariaLabel="Complete" [disabled]="acting() === c.id" [loading]="acting() === c.id"
                      (onClick)="completeCommitment(c)" />
                    <p-button icon="pi pi-times" severity="secondary" [text]="true" [rounded]="true" size="small"
                      ariaLabel="Dismiss" [disabled]="acting() === c.id"
                      (onClick)="dismissCommitment(c)" />
                  </div>
                }
              </div>
            }

            @if (dueTodayCommitments().length > 0) {
              <div class="flex flex-col gap-1">
                <span class="text-xs font-semibold uppercase text-muted-color">Due Today</span>
                @for (c of dueTodayCommitments(); track c.id) {
                  <div class="flex items-center gap-2 p-2 rounded bg-surface-0">
                    <span class="flex-1 text-sm truncate">{{ c.description }}</span>
                    <p-button icon="pi pi-check" severity="success" [text]="true" [rounded]="true" size="small"
                      ariaLabel="Complete" [disabled]="acting() === c.id" [loading]="acting() === c.id"
                      (onClick)="completeCommitment(c)" />
                    <p-button icon="pi pi-times" severity="secondary" [text]="true" [rounded]="true" size="small"
                      ariaLabel="Dismiss" [disabled]="acting() === c.id"
                      (onClick)="dismissCommitment(c)" />
                  </div>
                }
              </div>
            }

            @if (dueThisWeekCommitments().length > 0) {
              <div class="flex flex-col gap-1">
                <span class="text-xs font-semibold uppercase text-muted-color">Due This Week</span>
                @for (c of dueThisWeekCommitments(); track c.id) {
                  <div class="flex items-center gap-2 p-2 rounded bg-surface-0">
                    <span class="flex-1 text-sm truncate">{{ c.description }}</span>
                    @if (c.dueDate) {
                      <span class="text-xs text-muted-color">{{ formatDueDate(c.dueDate) }}</span>
                    }
                    <p-button icon="pi pi-check" severity="success" [text]="true" [rounded]="true" size="small"
                      ariaLabel="Complete" [disabled]="acting() === c.id" [loading]="acting() === c.id"
                      (onClick)="completeCommitment(c)" />
                    <p-button icon="pi pi-times" severity="secondary" [text]="true" [rounded]="true" size="small"
                      ariaLabel="Dismiss" [disabled]="acting() === c.id"
                      (onClick)="dismissCommitment(c)" />
                  </div>
                }
              </div>
            }

            @if (laterCommitments().length > 0) {
              <div class="flex flex-col gap-1">
                <span class="text-xs font-semibold uppercase text-muted-color">Later</span>
                @for (c of laterCommitments(); track c.id) {
                  <div class="flex items-center gap-2 p-2 rounded bg-surface-0">
                    <span class="flex-1 text-sm truncate">{{ c.description }}</span>
                    @if (c.dueDate) {
                      <span class="text-xs text-muted-color">{{ formatDueDate(c.dueDate) }}</span>
                    }
                    <p-button icon="pi pi-check" severity="success" [text]="true" [rounded]="true" size="small"
                      ariaLabel="Complete" [disabled]="acting() === c.id" [loading]="acting() === c.id"
                      (onClick)="completeCommitment(c)" />
                    <p-button icon="pi pi-times" severity="secondary" [text]="true" [rounded]="true" size="small"
                      ariaLabel="Dismiss" [disabled]="acting() === c.id"
                      (onClick)="dismissCommitment(c)" />
                  </div>
                }
              </div>
            }
          }
        </section>

        <!-- People Quick Access Section -->
        <section class="flex flex-col gap-3 p-5 rounded-md bg-surface-50" aria-label="People Quick Access">
          <header class="flex items-center justify-between">
            <h2 class="text-lg font-semibold">People</h2>
            <a routerLink="/people" class="text-xs font-medium text-primary hover:underline">
              View All
            </a>
          </header>

          @if (peopleLoading()) {
            <div class="flex flex-col gap-2">
              <p-skeleton height="2.5rem" />
              <p-skeleton height="2.5rem" />
              <p-skeleton height="2.5rem" />
            </div>
          } @else if (peopleError()) {
            <div class="flex flex-col items-start gap-2 py-3">
              <p class="text-sm text-muted-color">{{ peopleError() }}</p>
              <p-button label="Retry" icon="pi pi-refresh" size="small" [text]="true" (onClick)="loadPeople()" />
            </div>
          } @else if (people().length === 0) {
            <p class="text-sm text-muted-color py-2">No people added yet.</p>
          } @else {
            <ul class="flex flex-col gap-2">
              @for (p of people(); track p.id) {
                <li>
                  <a [routerLink]="['/people', p.id]"
                     class="flex items-center gap-3 p-2 rounded bg-surface-0 text-sm hover:bg-surface-100">
                    <span class="font-medium flex-1">{{ p.name }}</span>
                    <p-tag [value]="formatPersonType(p.type)" severity="info" />
                  </a>
                </li>
              }
            </ul>
          }
        </section>
      </div>
    </div>
  `,
})
export class DashboardPage implements OnInit {
  private readonly briefingService = inject(BriefingService);
  private readonly commitmentsService = inject(CommitmentsService);
  private readonly peopleService = inject(PeopleService);

  // Daily Brief state
  protected readonly brief = signal<DailyBrief | null>(null);
  protected readonly briefLoading = signal(true);
  protected readonly briefError = signal<string | null>(null);

  // Commitments state
  protected readonly commitments = signal<Commitment[]>([]);
  protected readonly commitmentsLoading = signal(true);
  protected readonly commitmentsError = signal<string | null>(null);
  protected readonly acting = signal<string | null>(null);

  // People state
  protected readonly people = signal<Person[]>([]);
  protected readonly peopleLoading = signal(true);
  protected readonly peopleError = signal<string | null>(null);

  // Grouped commitments
  protected readonly overdueCommitments = computed(() =>
    this.commitments().filter((c) => c.isOverdue),
  );

  protected readonly dueTodayCommitments = computed(() => {
    const today = todayLocalIso();
    return this.commitments().filter((c) => !c.isOverdue && toLocalDateKey(c.dueDate) === today);
  });

  protected readonly dueThisWeekCommitments = computed(() => {
    const today = todayLocalIso();
    const weekEnd = this.getEndOfWeek();
    return this.commitments().filter((c) => {
      if (c.isOverdue) return false;
      const key = toLocalDateKey(c.dueDate);
      if (!key) return false;
      return key > today && key <= weekEnd;
    });
  });

  protected readonly laterCommitments = computed(() => {
    const weekEnd = this.getEndOfWeek();
    return this.commitments().filter((c) => {
      if (c.isOverdue) return false;
      const key = toLocalDateKey(c.dueDate);
      // No due date or after this week
      return !key || key > weekEnd;
    }).filter((c) => {
      // Exclude those already in dueTodayCommitments
      const today = todayLocalIso();
      const key = toLocalDateKey(c.dueDate);
      return key !== today;
    });
  });

  ngOnInit(): void {
    this.loadBrief();
    this.loadCommitments();
    this.loadPeople();
  }

  protected loadBrief(): void {
    this.briefLoading.set(true);
    this.briefError.set(null);
    this.briefingService.getDailyBrief().subscribe({
      next: (b) => {
        this.brief.set(b);
        this.briefLoading.set(false);
      },
      error: () => {
        this.briefLoading.set(false);
        this.briefError.set('Failed to load daily brief.');
      },
    });
  }

  protected loadCommitments(): void {
    this.commitmentsLoading.set(true);
    this.commitmentsError.set(null);
    this.commitmentsService.list(undefined, 'Open').subscribe({
      next: (list) => {
        this.commitments.set(
          list.sort((a, b) => {
            if (a.isOverdue !== b.isOverdue) return a.isOverdue ? -1 : 1;
            return (a.dueDate ?? '\uffff').localeCompare(b.dueDate ?? '\uffff');
          }),
        );
        this.commitmentsLoading.set(false);
      },
      error: () => {
        this.commitmentsLoading.set(false);
        this.commitmentsError.set('Failed to load commitments.');
      },
    });
  }

  protected loadPeople(): void {
    this.peopleLoading.set(true);
    this.peopleError.set(null);
    this.peopleService.list().subscribe({
      next: (list) => {
        // Show most recently updated first, limit to 10
        this.people.set(
          [...list]
            .sort((a, b) => b.updatedAt.localeCompare(a.updatedAt))
            .slice(0, 10),
        );
        this.peopleLoading.set(false);
      },
      error: () => {
        this.peopleLoading.set(false);
        this.peopleError.set('Failed to load people.');
      },
    });
  }

  protected completeCommitment(c: Commitment): void {
    if (this.acting()) return;
    this.acting.set(c.id);
    this.commitmentsService.complete(c.id).subscribe({
      next: () => {
        this.acting.set(null);
        this.loadCommitments();
      },
      error: () => this.acting.set(null),
    });
  }

  protected dismissCommitment(c: Commitment): void {
    if (this.acting()) return;
    this.acting.set(c.id);
    this.commitmentsService.dismiss(c.id).subscribe({
      next: () => {
        this.acting.set(null);
        this.loadCommitments();
      },
      error: () => this.acting.set(null),
    });
  }

  protected formatDueDate(raw: string): string {
    const key = toLocalDateKey(raw);
    if (!key) return '';
    const [y, m, d] = key.split('-').map(Number);
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeZone: 'UTC' })
      .format(new Date(Date.UTC(y, m - 1, d)));
  }

  protected formatPersonType(type: string): string {
    switch (type) {
      case 'DirectReport': return 'Direct Report';
      case 'Peer': return 'Peer';
      case 'Stakeholder': return 'Stakeholder';
      case 'External': return 'External';
      default: return type;
    }
  }

  private getEndOfWeek(): string {
    const now = new Date();
    const dayOfWeek = now.getDay();
    const daysUntilSunday = dayOfWeek === 0 ? 0 : 7 - dayOfWeek;
    const endOfWeek = new Date(now);
    endOfWeek.setDate(now.getDate() + daysUntilSunday);
    return toLocalDateKey(endOfWeek) as string;
  }
}
