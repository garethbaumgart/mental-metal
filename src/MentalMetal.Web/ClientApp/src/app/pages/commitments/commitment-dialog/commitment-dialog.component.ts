import { ChangeDetectionStrategy, Component, inject, model, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { CommitmentsService } from '../../../shared/services/commitments.service';
import { Commitment, CommitmentDirection } from '../../../shared/models/commitment.model';
import { PeopleService } from '../../../shared/services/people.service';
import { InitiativesService } from '../../../shared/services/initiatives.service';
import { Person } from '../../../shared/models/person.model';
import { Initiative } from '../../../shared/models/initiative.model';

@Component({
  selector: 'app-commitment-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, DatePickerModule, DialogModule, InputTextModule, SelectModule, TextareaModule, ToastModule],
  providers: [MessageService],
  template: `
    <p-toast />
    <p-dialog
      [header]="editCommitment() ? 'Edit Commitment' : 'New Commitment'"
      [visible]="visible()"
      (visibleChange)="visible.set($event)"
      [modal]="true"
      [style]="{ width: '36rem' }"
    >
      <div class="flex flex-col gap-4 pt-4">
        <div class="flex flex-col gap-2">
          <label for="commitmentDesc" class="text-sm font-medium text-muted-color">Description *</label>
          <textarea pTextarea id="commitmentDesc" [(ngModel)]="description" [rows]="3" class="w-full" placeholder="What is the commitment?"></textarea>
        </div>

        @if (!editCommitment()) {
          <div class="flex flex-col gap-2">
            <label for="commitmentDirection" class="text-sm font-medium text-muted-color">Direction *</label>
            <p-select
              id="commitmentDirection"
              [options]="directionOptions"
              [(ngModel)]="selectedDirection"
              placeholder="Select direction"
              class="w-full"
            />
          </div>

          <div class="flex flex-col gap-2">
            <label for="commitmentPerson" class="text-sm font-medium text-muted-color">Person *</label>
            <p-select
              id="commitmentPerson"
              [options]="personOptions()"
              [(ngModel)]="selectedPersonId"
              placeholder="Select person"
              [filter]="true"
              filterBy="label"
              class="w-full"
            />
          </div>
        }

        <div class="flex flex-col gap-2">
          <label for="commitmentDueDate" class="text-sm font-medium text-muted-color">Due Date (optional)</label>
          <p-datepicker
            id="commitmentDueDate"
            [(ngModel)]="dueDate"
            [showIcon]="true"
            dateFormat="yy-mm-dd"
            placeholder="Select due date"
            class="w-full"
          />
        </div>

        @if (!editCommitment()) {
          <div class="flex flex-col gap-2">
            <label for="commitmentInitiative" class="text-sm font-medium text-muted-color">Initiative (optional)</label>
            <p-select
              id="commitmentInitiative"
              [options]="initiativeOptions()"
              [(ngModel)]="selectedInitiativeId"
              placeholder="Select initiative"
              [filter]="true"
              filterBy="label"
              [showClear]="true"
              class="w-full"
            />
          </div>
        }

        <div class="flex flex-col gap-2">
          <label for="commitmentNotes" class="text-sm font-medium text-muted-color">Notes (optional)</label>
          <textarea pTextarea id="commitmentNotes" [(ngModel)]="notes" [rows]="2" class="w-full" placeholder="Any additional context..."></textarea>
        </div>
      </div>

      <ng-template #footer>
        <div class="flex justify-end gap-2">
          <p-button label="Cancel" severity="secondary" (onClick)="visible.set(false)" />
          <p-button
            [label]="editCommitment() ? 'Save' : 'Create'"
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
export class CommitmentDialogComponent implements OnInit {
  private readonly commitmentsService = inject(CommitmentsService);
  private readonly peopleService = inject(PeopleService);
  private readonly initiativesService = inject(InitiativesService);
  private readonly messageService = inject(MessageService);

  readonly visible = model(false);
  readonly editCommitment = model<Commitment | null>(null);
  readonly created = output<Commitment>();
  readonly updated = output<Commitment>();

  protected readonly submitting = signal(false);
  protected readonly personOptions = signal<{ label: string; value: string }[]>([]);
  protected readonly initiativeOptions = signal<{ label: string; value: string }[]>([]);

  protected description = '';
  protected selectedDirection: CommitmentDirection | null = null;
  protected selectedPersonId: string | null = null;
  protected selectedInitiativeId: string | null = null;
  protected dueDate: Date | null = null;
  protected notes = '';

  protected readonly directionOptions = [
    { label: 'I owe them', value: 'MineToThem' as CommitmentDirection },
    { label: 'They owe me', value: 'TheirsToMe' as CommitmentDirection },
  ];

  ngOnInit(): void {
    this.loadPeople();
    this.loadInitiatives();
  }

  protected isValid(): boolean {
    if (this.editCommitment()) {
      return this.description.trim().length > 0;
    }
    return this.description.trim().length > 0
      && this.selectedDirection !== null
      && this.selectedPersonId !== null;
  }

  protected onSubmit(): void {
    if (!this.isValid()) return;

    this.submitting.set(true);

    const edit = this.editCommitment();
    if (edit) {
      this.commitmentsService.update(edit.id, {
        description: this.description.trim(),
        notes: this.notes.trim() || null,
      }).subscribe({
        next: (commitment) => {
          this.submitting.set(false);
          this.updated.emit(commitment);
          this.visible.set(false);
        },
        error: () => {
          this.submitting.set(false);
          this.messageService.add({ severity: 'error', summary: 'Failed to update commitment' });
        },
      });
    } else {
      const dueDateStr = this.dueDate
        ? `${this.dueDate.getFullYear()}-${String(this.dueDate.getMonth() + 1).padStart(2, '0')}-${String(this.dueDate.getDate()).padStart(2, '0')}`
        : undefined;

      this.commitmentsService.create({
        description: this.description.trim(),
        direction: this.selectedDirection!,
        personId: this.selectedPersonId!,
        ...(dueDateStr && { dueDate: dueDateStr }),
        ...(this.selectedInitiativeId && { initiativeId: this.selectedInitiativeId }),
        ...(this.notes.trim() && { notes: this.notes.trim() }),
      }).subscribe({
        next: (commitment) => {
          this.submitting.set(false);
          this.created.emit(commitment);
          this.resetForm();
          this.visible.set(false);
        },
        error: () => {
          this.submitting.set(false);
          this.messageService.add({ severity: 'error', summary: 'Failed to create commitment' });
        },
      });
    }
  }

  private resetForm(): void {
    this.description = '';
    this.selectedDirection = null;
    this.selectedPersonId = null;
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
