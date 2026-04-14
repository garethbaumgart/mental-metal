import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { GoalsService } from '../../../shared/services/goals.service';
import { PeopleService } from '../../../shared/services/people.service';
import { Goal, GoalStatus, GoalType } from '../../../shared/models/goal.model';
import { Person } from '../../../shared/models/person.model';

@Component({
  selector: 'app-goals-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    DatePipe,
    ButtonModule,
    DialogModule,
    InputTextModule,
    TextareaModule,
    SelectModule,
    DatePickerModule,
    InputNumberModule,
    TableModule,
    TagModule,
    ToastModule,
  ],
  providers: [MessageService],
  template: `
    <p-toast />
    <div class="flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">Goals</h1>
        <p-button label="New Goal" icon="pi pi-plus" (onClick)="openCreate()" />
      </div>

      <div class="flex gap-4 flex-wrap">
        <p-select
          [options]="statusFilterOptions"
          [(ngModel)]="filterStatus"
          (ngModelChange)="load()"
          placeholder="All statuses"
          [showClear]="true"
          class="w-48"
        />
      </div>

      @if (loading()) {
        <div class="flex justify-center p-8"><i class="pi pi-spinner pi-spin text-2xl"></i></div>
      } @else if (items().length === 0) {
        <div class="flex flex-col items-center gap-4 p-12">
          <i class="pi pi-flag text-4xl text-muted-color"></i>
          <p class="text-muted-color">No goals recorded yet.</p>
        </div>
      } @else {
        <p-table [value]="items()" [rowHover]="true" styleClass="p-datatable-sm">
          <ng-template #header>
            <tr>
              <th>Title</th>
              <th>Person</th>
              <th>Type</th>
              <th>Status</th>
              <th>Target</th>
              <th>Actions</th>
            </tr>
          </ng-template>
          <ng-template #body let-row>
            <tr>
              <td>{{ row.title }}</td>
              <td>{{ personName(row.personId) }}</td>
              <td>{{ row.goalType }}</td>
              <td><p-tag [value]="row.status" [severity]="statusSeverity(row.status)" /></td>
              <td>{{ row.targetDate ? (row.targetDate | date: 'mediumDate') : '—' }}</td>
              <td class="flex gap-2">
                @if (row.status === 'Active') {
                  <p-button label="Achieve" size="small" severity="success" (onClick)="achieve(row)" />
                  <p-button label="Miss" size="small" severity="danger" [outlined]="true" (onClick)="miss(row)" />
                  <p-button label="Defer" size="small" [outlined]="true" (onClick)="defer(row)" />
                } @else {
                  <p-button label="Reactivate" size="small" [outlined]="true" (onClick)="reactivate(row)" />
                }
                <p-button label="Check-in" size="small" [text]="true" (onClick)="openCheckIn(row)" />
              </td>
            </tr>
          </ng-template>
        </p-table>
      }

      <p-dialog
        [(visible)]="showCreate"
        header="New Goal"
        [modal]="true"
        [style]="{ width: '480px' }"
      >
        <div class="flex flex-col gap-4">
          <div class="flex flex-col gap-2">
            <label class="text-sm font-medium text-muted-color">Person</label>
            <p-select [options]="peopleOptions()" [(ngModel)]="draftPersonId" placeholder="Select person" class="w-full" />
          </div>
          <div class="flex flex-col gap-2">
            <label class="text-sm font-medium text-muted-color">Title</label>
            <input pInputText [(ngModel)]="draftTitle" class="w-full" />
          </div>
          <div class="flex flex-col gap-2">
            <label class="text-sm font-medium text-muted-color">Type</label>
            <p-select [options]="typeOptions" [(ngModel)]="draftType" placeholder="Select type" class="w-full" />
          </div>
          <div class="flex flex-col gap-2">
            <label class="text-sm font-medium text-muted-color">Description</label>
            <textarea pTextarea [(ngModel)]="draftDescription" [rows]="3" class="w-full"></textarea>
          </div>
          <div class="flex flex-col gap-2">
            <label class="text-sm font-medium text-muted-color">Target date</label>
            <p-datepicker [(ngModel)]="draftTarget" dateFormat="yy-mm-dd" [showClear]="true" class="w-full" />
          </div>
        </div>
        <ng-template #footer>
          <p-button label="Cancel" [text]="true" (onClick)="showCreate.set(false)" />
          <p-button label="Create" (onClick)="submit()" [disabled]="!draftPersonId || !draftTitle.trim() || !draftType" />
        </ng-template>
      </p-dialog>

      <p-dialog
        [(visible)]="showCheckIn"
        header="Record Check-In"
        [modal]="true"
        [style]="{ width: '420px' }"
      >
        <div class="flex flex-col gap-4">
          <div class="flex flex-col gap-2">
            <label class="text-sm font-medium text-muted-color">Note</label>
            <textarea pTextarea [(ngModel)]="checkInNote" [rows]="3" class="w-full"></textarea>
          </div>
          <div class="flex flex-col gap-2">
            <label class="text-sm font-medium text-muted-color">Progress (0-100)</label>
            <p-inputnumber [(ngModel)]="checkInProgress" [min]="0" [max]="100" class="w-full" />
          </div>
        </div>
        <ng-template #footer>
          <p-button label="Cancel" [text]="true" (onClick)="showCheckIn.set(false)" />
          <p-button label="Record" (onClick)="submitCheckIn()" [disabled]="!checkInNote.trim()" />
        </ng-template>
      </p-dialog>
    </div>
  `,
})
export class GoalsListComponent implements OnInit {
  private readonly service = inject(GoalsService);
  private readonly peopleService = inject(PeopleService);
  private readonly messageService = inject(MessageService);

  readonly items = signal<Goal[]>([]);
  readonly people = signal<Person[]>([]);
  readonly loading = signal(true);
  readonly showCreate = signal(false);
  readonly showCheckIn = signal(false);

  protected filterStatus: GoalStatus | null = null;
  protected readonly statusFilterOptions: { label: string; value: GoalStatus }[] = [
    { label: 'Active', value: 'Active' },
    { label: 'Achieved', value: 'Achieved' },
    { label: 'Missed', value: 'Missed' },
    { label: 'Deferred', value: 'Deferred' },
  ];
  protected readonly typeOptions: { label: string; value: GoalType }[] = [
    { label: 'Development', value: 'Development' },
    { label: 'Performance', value: 'Performance' },
  ];

  protected draftPersonId: string | null = null;
  protected draftTitle = '';
  protected draftType: GoalType | null = null;
  protected draftDescription = '';
  protected draftTarget: Date | null = null;

  protected checkInGoal: Goal | null = null;
  protected checkInNote = '';
  protected checkInProgress: number | null = null;

  protected peopleOptions() {
    return this.people().map((p) => ({ label: p.name, value: p.id }));
  }

  protected personName(id: string): string {
    return this.people().find((p) => p.id === id)?.name ?? '(unknown)';
  }

  protected statusSeverity(status: GoalStatus): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (status) {
      case 'Active': return 'info';
      case 'Achieved': return 'success';
      case 'Missed': return 'danger';
      case 'Deferred': return 'secondary';
    }
  }

  ngOnInit(): void {
    this.peopleService.list().subscribe({ next: (p) => this.people.set(p) });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.list(undefined, undefined, this.filterStatus ?? undefined).subscribe({
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

  openCreate(): void {
    this.draftPersonId = null;
    this.draftTitle = '';
    this.draftType = null;
    this.draftDescription = '';
    this.draftTarget = null;
    this.showCreate.set(true);
  }

  submit(): void {
    if (!this.draftPersonId || !this.draftType || !this.draftTitle.trim()) return;
    this.service
      .create({
        personId: this.draftPersonId,
        title: this.draftTitle.trim(),
        goalType: this.draftType,
        description: this.draftDescription || null,
        targetDate: this.draftTarget ? this.draftTarget.toISOString().substring(0, 10) : null,
      })
      .subscribe({
        next: (g) => {
          this.items.update((list) => [g, ...list]);
          this.showCreate.set(false);
          this.messageService.add({ severity: 'success', summary: 'Goal created' });
        },
        error: () =>
          this.messageService.add({ severity: 'error', summary: 'Failed to create goal' }),
      });
  }

  achieve(goal: Goal): void {
    this.service.achieve(goal.id).subscribe({
      next: (g) => this.replace(g),
      error: (e) =>
        this.messageService.add({ severity: 'error', summary: 'Cannot achieve', detail: e?.error?.error ?? '' }),
    });
  }

  miss(goal: Goal): void {
    this.service.miss(goal.id).subscribe({
      next: (g) => this.replace(g),
      error: (e) =>
        this.messageService.add({ severity: 'error', summary: 'Cannot mark missed', detail: e?.error?.error ?? '' }),
    });
  }

  defer(goal: Goal): void {
    this.service.defer(goal.id, { reason: null }).subscribe({
      next: (g) => this.replace(g),
      error: (e) =>
        this.messageService.add({ severity: 'error', summary: 'Cannot defer', detail: e?.error?.error ?? '' }),
    });
  }

  reactivate(goal: Goal): void {
    this.service.reactivate(goal.id).subscribe({
      next: (g) => this.replace(g),
      error: (e) =>
        this.messageService.add({ severity: 'error', summary: 'Cannot reactivate', detail: e?.error?.error ?? '' }),
    });
  }

  openCheckIn(goal: Goal): void {
    this.checkInGoal = goal;
    this.checkInNote = '';
    this.checkInProgress = null;
    this.showCheckIn.set(true);
  }

  submitCheckIn(): void {
    if (!this.checkInGoal || !this.checkInNote.trim()) return;
    this.service
      .recordCheckIn(this.checkInGoal.id, { note: this.checkInNote.trim(), progress: this.checkInProgress })
      .subscribe({
        next: (g) => {
          this.replace(g);
          this.showCheckIn.set(false);
          this.messageService.add({ severity: 'success', summary: 'Check-in recorded' });
        },
        error: () =>
          this.messageService.add({ severity: 'error', summary: 'Failed to record check-in' }),
      });
  }

  private replace(g: Goal): void {
    this.items.update((list) => list.map((x) => (x.id === g.id ? g : x)));
  }
}
