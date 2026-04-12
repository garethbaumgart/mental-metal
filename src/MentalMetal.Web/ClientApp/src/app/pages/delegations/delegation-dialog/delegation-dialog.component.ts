import { ChangeDetectionStrategy, Component, effect, inject, model, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { DelegationsService } from '../../../shared/services/delegations.service';
import { Delegation, DelegationPriority } from '../../../shared/models/delegation.model';
import { PeopleService } from '../../../shared/services/people.service';
import { InitiativesService } from '../../../shared/services/initiatives.service';
import { Person } from '../../../shared/models/person.model';
import { Initiative } from '../../../shared/models/initiative.model';

@Component({
  selector: 'app-delegation-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, DatePickerModule, DialogModule, InputTextModule, SelectModule, TextareaModule, ToastModule],
  providers: [MessageService],
  template: `
    <p-toast />
    <p-dialog
      [header]="editDelegation() ? 'Edit Delegation' : 'New Delegation'"
      [visible]="visible()"
      (visibleChange)="visible.set($event)"
      [modal]="true"
      [style]="{ width: '36rem' }"
    >
      <div class="flex flex-col gap-4 pt-4">
        <div class="flex flex-col gap-2">
          <label for="delegationDesc" class="text-sm font-medium text-muted-color">Description *</label>
          <textarea pTextarea id="delegationDesc" [(ngModel)]="description" [rows]="3" class="w-full" placeholder="What needs to be done?"></textarea>
        </div>

        @if (!editDelegation()) {
          <div class="flex flex-col gap-2">
            <label for="delegationPerson" class="text-sm font-medium text-muted-color">Delegate to *</label>
            <p-select
              id="delegationPerson"
              [options]="personOptions()"
              [(ngModel)]="selectedPersonId"
              placeholder="Select person"
              [filter]="true"
              filterBy="label"
              class="w-full"
            />
          </div>

          <div class="flex flex-col gap-2">
            <label for="delegationPriority" class="text-sm font-medium text-muted-color">Priority</label>
            <p-select
              id="delegationPriority"
              [options]="priorityOptions"
              [(ngModel)]="selectedPriority"
              placeholder="Medium"
              class="w-full"
            />
          </div>
        }

        <div class="flex flex-col gap-2">
          <label for="delegationDueDate" class="text-sm font-medium text-muted-color">Due Date (optional)</label>
          <p-datepicker
            id="delegationDueDate"
            [(ngModel)]="dueDate"
            [showIcon]="true"
            dateFormat="yy-mm-dd"
            placeholder="Select due date"
            class="w-full"
          />
        </div>

        <div class="flex flex-col gap-2">
          <label for="delegationInitiative" class="text-sm font-medium text-muted-color">Initiative (optional)</label>
          <p-select
            id="delegationInitiative"
            [options]="initiativeOptions()"
            [(ngModel)]="selectedInitiativeId"
            placeholder="Select initiative"
            [filter]="true"
            filterBy="label"
            [showClear]="true"
            class="w-full"
          />
        </div>

        <div class="flex flex-col gap-2">
          <label for="delegationNotes" class="text-sm font-medium text-muted-color">Notes (optional)</label>
          <textarea pTextarea id="delegationNotes" [(ngModel)]="notes" [rows]="2" class="w-full" placeholder="Any additional context..."></textarea>
        </div>
      </div>

      <ng-template #footer>
        <div class="flex justify-end gap-2">
          <p-button label="Cancel" severity="secondary" (onClick)="visible.set(false)" />
          <p-button
            [label]="editDelegation() ? 'Save' : 'Create'"
            icon="pi pi-check"
            (onClick)="onSubmit()"
            [loading]="submitting()"
            [disabled]="!isValid()"
          />
        </div>
      </ng-template>
    </p-dialog>
  `,
})
export class DelegationDialogComponent implements OnInit {
  private readonly delegationsService = inject(DelegationsService);
  private readonly peopleService = inject(PeopleService);
  private readonly initiativesService = inject(InitiativesService);
  private readonly messageService = inject(MessageService);

  readonly visible = model(false);
  readonly editDelegation = model<Delegation | null>(null);
  readonly created = output<Delegation>();
  readonly updated = output<Delegation>();

  protected readonly submitting = signal(false);
  protected readonly personOptions = signal<{ label: string; value: string }[]>([]);
  protected readonly initiativeOptions = signal<{ label: string; value: string }[]>([]);

  protected description = '';
  protected selectedPersonId: string | null = null;
  protected selectedPriority: DelegationPriority | null = null;
  protected selectedInitiativeId: string | null = null;
  protected dueDate: Date | null = null;
  protected notes = '';

  protected readonly priorityOptions = [
    { label: 'Low', value: 'Low' as DelegationPriority },
    { label: 'Medium', value: 'Medium' as DelegationPriority },
    { label: 'High', value: 'High' as DelegationPriority },
    { label: 'Urgent', value: 'Urgent' as DelegationPriority },
  ];

  constructor() {
    effect(() => {
      const edit = this.editDelegation();
      if (edit && this.visible()) {
        this.description = edit.description;
        this.notes = edit.notes ?? '';
        this.dueDate = edit.dueDate ? new Date(edit.dueDate + 'T00:00:00') : null;
        this.selectedInitiativeId = edit.initiativeId;
      }
    });
  }

  ngOnInit(): void {
    this.loadPeople();
    this.loadInitiatives();
  }

  protected isValid(): boolean {
    if (this.editDelegation()) {
      return this.description.trim().length > 0;
    }
    return this.description.trim().length > 0 && this.selectedPersonId !== null;
  }

  protected onSubmit(): void {
    if (!this.isValid()) return;

    this.submitting.set(true);

    const edit = this.editDelegation();
    if (edit) {
      this.delegationsService.update(edit.id, {
        description: this.description.trim(),
        notes: this.notes.trim() || null,
      }).subscribe({
        next: (latest) => {
          const newDueDateStr = this.formatDateStr(this.dueDate);
          const dueDateChanged = newDueDateStr !== edit.dueDate;

          if (dueDateChanged) {
            this.delegationsService.updateDueDate(edit.id, { dueDate: newDueDateStr }).subscribe({
              next: (updated) => this.finishEdit(updated),
              error: () => {
                this.messageService.add({ severity: 'error', summary: 'Failed to update due date' });
                this.finishEdit(latest);
              },
            });
          } else {
            this.finishEdit(latest);
          }
        },
        error: () => {
          this.submitting.set(false);
          this.messageService.add({ severity: 'error', summary: 'Failed to update delegation' });
        },
      });
    } else {
      const dueDateStr = this.formatDateStr(this.dueDate) ?? undefined;

      this.delegationsService.create({
        description: this.description.trim(),
        delegatePersonId: this.selectedPersonId!,
        ...(this.selectedPriority && { priority: this.selectedPriority }),
        ...(dueDateStr && { dueDate: dueDateStr }),
        ...(this.selectedInitiativeId && { initiativeId: this.selectedInitiativeId }),
        ...(this.notes.trim() && { notes: this.notes.trim() }),
      }).subscribe({
        next: (delegation) => {
          this.submitting.set(false);
          this.created.emit(delegation);
          this.resetForm();
          this.visible.set(false);
        },
        error: () => {
          this.submitting.set(false);
          this.messageService.add({ severity: 'error', summary: 'Failed to create delegation' });
        },
      });
    }
  }

  private finishEdit(delegation: Delegation): void {
    this.submitting.set(false);
    this.updated.emit(delegation);
    this.visible.set(false);
  }

  private formatDateStr(date: Date | null): string | null {
    if (!date) return null;
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }

  private resetForm(): void {
    this.description = '';
    this.selectedPersonId = null;
    this.selectedPriority = null;
    this.selectedInitiativeId = null;
    this.dueDate = null;
    this.notes = '';
  }

  private loadPeople(): void {
    this.peopleService.list().subscribe({
      next: (people: Person[]) => {
        this.personOptions.set(
          people
            .filter(p => !p.isArchived)
            .map(p => ({ label: p.name, value: p.id })),
        );
      },
    });
  }

  private loadInitiatives(): void {
    this.initiativesService.list().subscribe({
      next: (initiatives: Initiative[]) => {
        this.initiativeOptions.set(
          initiatives.map(i => ({ label: i.title, value: i.id })),
        );
      },
    });
  }
}
