import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { OneOnOnesService } from '../../../shared/services/one-on-ones.service';
import { PeopleService } from '../../../shared/services/people.service';
import { OneOnOne } from '../../../shared/models/one-on-one.model';
import { Person } from '../../../shared/models/person.model';

@Component({
  selector: 'app-one-on-ones-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    TextareaModule,
    SelectModule,
    DatePickerModule,
    TableModule,
    TagModule,
    ToastModule,
  ],
  providers: [MessageService],
  template: `
    <p-toast />
    <div class="flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">One-on-Ones</h1>
        <p-button label="New 1:1" icon="pi pi-plus" (onClick)="openCreate()" />
      </div>

      @if (loading()) {
        <div class="flex justify-center p-8"><i class="pi pi-spinner pi-spin text-2xl"></i></div>
      } @else if (items().length === 0) {
        <div class="flex flex-col items-center gap-4 p-12">
          <i class="pi pi-comments text-4xl text-muted-color"></i>
          <p class="text-muted-color">No one-on-ones yet. Record your first 1:1.</p>
        </div>
      } @else {
        <p-table [value]="items()" [rowHover]="true" styleClass="p-datatable-sm">
          <ng-template #header>
            <tr>
              <th>Date</th>
              <th>Person</th>
              <th>Mood</th>
              <th>Topics</th>
              <th>Actions / Follow-ups</th>
            </tr>
          </ng-template>
          <ng-template #body let-row>
            <tr>
              <td>{{ formatOccurredAt(row.occurredAt) }}</td>
              <td>{{ personName(row.personId) }}</td>
              <td>
                @if (row.moodRating !== null) { {{ row.moodRating }} / 5 } @else { — }
              </td>
              <td>
                @for (t of row.topics; track t) {
                  <p-tag [value]="t" class="mr-1" />
                }
              </td>
              <td>{{ row.actionItems.length }} / {{ row.followUps.length }}</td>
            </tr>
          </ng-template>
        </p-table>
      }

      <p-dialog
        [(visible)]="showCreate"
        header="New One-on-One"
        [modal]="true"
        [style]="{ width: '480px' }"
      >
        <div class="flex flex-col gap-4">
          <div class="flex flex-col gap-2">
            <label for="person" class="text-sm font-medium text-muted-color">Person</label>
            <p-select
              id="person"
              [options]="peopleOptions()"
              [(ngModel)]="draftPersonId"
              placeholder="Select person"
              class="w-full"
            />
          </div>
          <div class="flex flex-col gap-2">
            <label for="date" class="text-sm font-medium text-muted-color">Date</label>
            <p-datepicker id="date" [(ngModel)]="draftDate" dateFormat="yy-mm-dd" class="w-full" />
          </div>
          <div class="flex flex-col gap-2">
            <label for="notes" class="text-sm font-medium text-muted-color">Notes</label>
            <textarea pTextarea id="notes" [(ngModel)]="draftNotes" [rows]="4" class="w-full"></textarea>
          </div>
          <div class="flex flex-col gap-2">
            <label for="mood" class="text-sm font-medium text-muted-color">Mood (1-5)</label>
            <p-select
              id="mood"
              [options]="moodOptions"
              [(ngModel)]="draftMood"
              [showClear]="true"
              placeholder="Optional"
              class="w-full"
            />
          </div>
        </div>
        <ng-template #footer>
          <p-button label="Cancel" severity="secondary" (onClick)="showCreate.set(false)" />
          <p-button label="Create" (onClick)="submit()" [disabled]="!draftPersonId || !draftDate" />
        </ng-template>
      </p-dialog>
    </div>
  `,
})
export class OneOnOnesListComponent implements OnInit {
  private readonly service = inject(OneOnOnesService);
  private readonly peopleService = inject(PeopleService);
  private readonly messageService = inject(MessageService);

  readonly items = signal<OneOnOne[]>([]);
  readonly people = signal<Person[]>([]);
  readonly loading = signal(true);
  readonly showCreate = signal(false);

  protected readonly moodOptions = [1, 2, 3, 4, 5].map((n) => ({ label: `${n}`, value: n }));
  protected draftPersonId: string | null = null;
  protected draftDate: Date | null = new Date();
  protected draftNotes = '';
  protected draftMood: number | null = null;

  protected peopleOptions() {
    return this.people().map((p) => ({ label: p.name, value: p.id }));
  }

  protected personName(id: string): string {
    return this.people().find((p) => p.id === id)?.name ?? '(unknown)';
  }

  /**
   * Defensively renders a 1:1 date. The API validates OccurredAt >= 2000-01-01,
   * but legacy rows that bypassed validation can still exist; render them as
   * an em dash rather than "Jan 1, 1" to avoid user confusion.
   *
   * `occurredAt` is a DateOnly string (`YYYY-MM-DD`). We parse components
   * explicitly and format in UTC so the calendar day shown matches what the
   * user recorded, rather than shifting across midnight in local time.
   */
  protected formatOccurredAt(raw: string | null | undefined): string {
    if (!raw) return '—';
    const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(raw);
    if (!match) return '—';
    const year = Number(match[1]);
    if (year < 2000) return '—';
    const utc = new Date(Date.UTC(year, Number(match[2]) - 1, Number(match[3])));
    if (Number.isNaN(utc.getTime())) return '—';
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeZone: 'UTC' }).format(utc);
  }

  ngOnInit(): void {
    this.peopleService.list().subscribe({
      next: (p) => this.people.set(p),
    });
    this.load();
  }

  openCreate(): void {
    this.draftPersonId = null;
    this.draftDate = new Date();
    this.draftNotes = '';
    this.draftMood = null;
    this.showCreate.set(true);
  }

  submit(): void {
    if (!this.draftPersonId || !this.draftDate) return;
    const dateStr = this.draftDate.toISOString().substring(0, 10);
    this.service
      .create({
        personId: this.draftPersonId,
        occurredAt: dateStr,
        notes: this.draftNotes || null,
        moodRating: this.draftMood,
      })
      .subscribe({
        next: (o) => {
          this.items.update((list) => [o, ...list]);
          this.showCreate.set(false);
          this.messageService.add({ severity: 'success', summary: '1:1 recorded' });
        },
        error: () =>
          this.messageService.add({ severity: 'error', summary: 'Failed to create 1:1' }),
      });
  }

  private load(): void {
    this.loading.set(true);
    this.service.list().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to load' });
      },
    });
  }
}
