import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService } from 'primeng/api';
import { NudgesService } from './nudges.service';
import { CadenceType, Nudge } from './nudges.models';
import { NudgeEditDialogComponent } from './nudge-edit-dialog.component';
import { PeopleService } from '../../shared/services/people.service';
import { InitiativesService } from '../../shared/services/initiatives.service';

type DueFilter = 'all' | 'today' | 'thisWeek';
type ActiveFilter = 'all' | 'active' | 'paused';

@Component({
  selector: 'app-nudges-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ButtonModule,
    ConfirmDialogModule,
    SelectModule,
    TableModule,
    TagModule,
    ToastModule,
    TooltipModule,
    NudgeEditDialogComponent,
  ],
  providers: [MessageService, ConfirmationService],
  template: `
    <p-toast />
    <p-confirmDialog />
    <div class="flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">Nudges</h1>
        <p-button label="New Nudge" icon="pi pi-plus" (onClick)="openCreateDialog()" />
      </div>

      <div class="flex items-center gap-4 flex-wrap">
        <p-select
          [options]="activeOptions"
          [ngModel]="activeFilter()"
          (ngModelChange)="activeFilter.set($event); reload()"
          class="w-40"
        />
        <p-select
          [options]="dueOptions"
          [ngModel]="dueFilter()"
          (ngModelChange)="dueFilter.set($event); reload()"
          class="w-40"
        />
        <p-select
          [options]="personOptions()"
          [ngModel]="selectedPersonId()"
          (ngModelChange)="selectedPersonId.set($event); reload()"
          [showClear]="true"
          placeholder="All people"
          [filter]="true"
          filterBy="label"
          class="w-56"
        />
        <p-select
          [options]="initiativeOptions()"
          [ngModel]="selectedInitiativeId()"
          (ngModelChange)="selectedInitiativeId.set($event); reload()"
          [showClear]="true"
          placeholder="All initiatives"
          [filter]="true"
          filterBy="label"
          class="w-56"
        />
      </div>

      @if (loading()) {
        <div class="flex justify-center p-8">
          <i class="pi pi-spinner pi-spin text-2xl"></i>
        </div>
      } @else if (nudges().length === 0) {
        <div class="flex flex-col items-center gap-4 p-12">
          <i class="pi pi-clock text-4xl text-muted-color"></i>
          <p class="text-muted-color">No nudges yet. Create one to start building a rhythm.</p>
        </div>
      } @else {
        <p-table [value]="nudges()" [rows]="20" [paginator]="nudges().length > 20" [rowHover]="true" styleClass="p-datatable-sm">
          <ng-template #header>
            <tr>
              <th>Title</th>
              <th>Cadence</th>
              <th>Next due</th>
              <th>Person</th>
              <th>Initiative</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </ng-template>
          <ng-template #body let-n>
            <tr>
              <td class="font-medium">{{ n.title }}</td>
              <td class="text-muted-color text-sm">{{ cadenceLabel(n.cadence.type) }}</td>
              <td class="text-muted-color text-sm">{{ n.nextDueDate ?? '-' }}</td>
              <td class="text-muted-color text-sm">{{ personName(n.personId) }}</td>
              <td class="text-muted-color text-sm">{{ initiativeName(n.initiativeId) }}</td>
              <td>
                @if (n.isActive) {
                  <p-tag value="Active" severity="success" />
                } @else {
                  <p-tag value="Paused" severity="secondary" />
                }
              </td>
              <td>
                <div class="flex gap-1">
                  @if (n.isActive) {
                    <p-button icon="pi pi-check" [text]="true" size="small" severity="success" pTooltip="Mark as nudged" (onClick)="onMarkNudged(n)" />
                    <p-button icon="pi pi-pause" [text]="true" size="small" severity="warn" pTooltip="Pause" (onClick)="onPause(n)" />
                  } @else {
                    <p-button icon="pi pi-play" [text]="true" size="small" severity="success" pTooltip="Resume" (onClick)="onResume(n)" />
                  }
                  <p-button icon="pi pi-pencil" [text]="true" size="small" pTooltip="Edit" (onClick)="onEdit(n)" />
                  <p-button icon="pi pi-trash" [text]="true" size="small" severity="danger" pTooltip="Delete" (onClick)="onDelete(n)" />
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      }

      <app-nudge-edit-dialog
        [(visible)]="dialogVisible"
        [(editNudge)]="editingNudge"
        (saved)="onSaved($event)"
      />
    </div>
  `,
})
export class NudgesListPageComponent implements OnInit {
  private readonly service = inject(NudgesService);
  private readonly peopleService = inject(PeopleService);
  private readonly initiativesService = inject(InitiativesService);
  private readonly messageService = inject(MessageService);
  private readonly confirmation = inject(ConfirmationService);

  readonly nudges = signal<Nudge[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly editingNudge = signal<Nudge | null>(null);

  readonly activeFilter = signal<ActiveFilter>('all');
  readonly dueFilter = signal<DueFilter>('all');
  readonly selectedPersonId = signal<string | null>(null);
  readonly selectedInitiativeId = signal<string | null>(null);

  protected readonly personOptions = signal<{ label: string; value: string }[]>([]);
  protected readonly initiativeOptions = signal<{ label: string; value: string }[]>([]);
  protected readonly peopleMap = signal<Map<string, string>>(new Map());
  protected readonly initiativesMap = signal<Map<string, string>>(new Map());

  protected readonly activeOptions: { label: string; value: ActiveFilter }[] = [
    { label: 'All', value: 'all' },
    { label: 'Active', value: 'active' },
    { label: 'Paused', value: 'paused' },
  ];

  protected readonly dueOptions: { label: string; value: DueFilter }[] = [
    { label: 'Any time', value: 'all' },
    { label: 'Due today', value: 'today' },
    { label: 'Due this week', value: 'thisWeek' },
  ];

  protected readonly isActiveParam = computed(() => {
    switch (this.activeFilter()) {
      case 'active': return true;
      case 'paused': return false;
      default: return null;
    }
  });

  ngOnInit(): void {
    this.loadPeople();
    this.loadInitiatives();
    this.reload();
  }

  protected cadenceLabel(type: CadenceType): string {
    switch (type) {
      case 'Daily': return 'Daily';
      case 'Weekly': return 'Weekly';
      case 'Biweekly': return 'Biweekly';
      case 'Monthly': return 'Monthly';
      case 'Custom': return 'Custom';
    }
  }

  protected personName(id: string | null): string {
    if (!id) return '-';
    return this.peopleMap().get(id) ?? '-';
  }

  protected initiativeName(id: string | null): string {
    if (!id) return '-';
    return this.initiativesMap().get(id) ?? '-';
  }

  protected openCreateDialog(): void {
    this.editingNudge.set(null);
    this.dialogVisible.set(true);
  }

  protected onEdit(n: Nudge): void {
    this.editingNudge.set(n);
    this.dialogVisible.set(true);
  }

  protected onSaved(_nudge: Nudge): void {
    this.reload();
  }

  protected onMarkNudged(n: Nudge): void {
    this.service.markNudged(n.id).subscribe({
      next: () => this.reload(),
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to mark nudged' }),
    });
  }

  protected onPause(n: Nudge): void {
    this.service.pause(n.id).subscribe({
      next: () => this.reload(),
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to pause nudge' }),
    });
  }

  protected onResume(n: Nudge): void {
    this.service.resume(n.id).subscribe({
      next: () => this.reload(),
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to resume nudge' }),
    });
  }

  protected onDelete(n: Nudge): void {
    this.confirmation.confirm({
      message: `Delete nudge "${n.title}"?`,
      accept: () => {
        this.service.delete(n.id).subscribe({
          next: () => this.reload(),
          error: () => this.messageService.add({ severity: 'error', summary: 'Failed to delete nudge' }),
        });
      },
    });
  }

  protected reload(): void {
    this.loading.set(true);

    const dueWithinDays = this.dueFilter() === 'thisWeek' ? 7 : undefined;
    const dueBefore = this.dueFilter() === 'today' ? this.todayIso() : undefined;
    const isActive = this.dueFilter() !== 'all' ? true : this.isActiveParam() ?? undefined;

    this.service.list({
      isActive,
      personId: this.selectedPersonId() ?? undefined,
      initiativeId: this.selectedInitiativeId() ?? undefined,
      dueBefore,
      dueWithinDays,
    }).subscribe({
      next: (items) => {
        this.nudges.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.nudges.set([]);
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to load nudges' });
      },
    });
  }

  private todayIso(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  private loadPeople(): void {
    this.peopleService.list().subscribe({
      next: (people) => {
        const active = people.filter(p => !p.isArchived);
        this.personOptions.set(active.map(p => ({ label: p.name, value: p.id })));
        const map = new Map<string, string>();
        people.forEach(p => map.set(p.id, p.name));
        this.peopleMap.set(map);
      },
    });
  }

  private loadInitiatives(): void {
    this.initiativesService.list().subscribe({
      next: (items) => {
        this.initiativeOptions.set(items.map(i => ({ label: i.title, value: i.id })));
        const map = new Map<string, string>();
        items.forEach(i => map.set(i.id, i.title));
        this.initiativesMap.set(map);
      },
    });
  }
}
