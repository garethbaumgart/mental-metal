import { ChangeDetectionStrategy, Component, effect, inject, model, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { NudgesService } from './nudges.service';
import { CadenceType, CreateNudgeRequest, Nudge, NudgeDayOfWeek } from './nudges.models';
import { PeopleService } from '../../shared/services/people.service';
import { InitiativesService } from '../../shared/services/initiatives.service';

@Component({
  selector: 'app-nudge-edit-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ButtonModule,
    DatePickerModule,
    DialogModule,
    InputNumberModule,
    InputTextModule,
    SelectModule,
    TextareaModule,
    ToastModule,
  ],
  providers: [MessageService],
  template: `
    <p-toast />
    <p-dialog
      [header]="editNudge() ? 'Edit Nudge' : 'New Nudge'"
      [visible]="visible()"
      (visibleChange)="visible.set($event)"
      [modal]="true"
      [style]="{ width: '36rem' }"
    >
      <div class="flex flex-col gap-4 pt-4">
        <div class="flex flex-col gap-2">
          <label for="nudgeTitle" class="text-sm font-medium text-muted-color">Title *</label>
          <input pInputText id="nudgeTitle" [(ngModel)]="title" maxlength="200" class="w-full" placeholder="e.g. Review risk log" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="nudgeCadence" class="text-sm font-medium text-muted-color">Cadence *</label>
          <p-select
            id="nudgeCadence"
            [options]="cadenceOptions"
            [(ngModel)]="cadenceType"
            class="w-full"
          />
        </div>

        @if (cadenceType === 'Weekly' || cadenceType === 'Biweekly') {
          <div class="flex flex-col gap-2">
            <label for="nudgeDow" class="text-sm font-medium text-muted-color">Day of week *</label>
            <p-select id="nudgeDow" [options]="dayOfWeekOptions" [(ngModel)]="dayOfWeek" class="w-full" />
          </div>
        }

        @if (cadenceType === 'Monthly') {
          <div class="flex flex-col gap-2">
            <label for="nudgeDom" class="text-sm font-medium text-muted-color">Day of month *</label>
            <p-inputNumber id="nudgeDom" [(ngModel)]="dayOfMonth" [min]="1" [max]="31" class="w-full" />
            <small class="text-muted-color">Days above the month length (e.g. 31 in February) will clamp to month-end.</small>
          </div>
        }

        @if (cadenceType === 'Custom') {
          <div class="flex flex-col gap-2">
            <label for="nudgeInterval" class="text-sm font-medium text-muted-color">Interval (days) *</label>
            <p-inputNumber id="nudgeInterval" [(ngModel)]="customIntervalDays" [min]="1" [max]="365" class="w-full" />
          </div>
        }

        @if (!editNudge()) {
          <div class="flex flex-col gap-2">
            <label for="nudgeStart" class="text-sm font-medium text-muted-color">Start date (optional)</label>
            <p-datepicker id="nudgeStart" [(ngModel)]="startDate" [showIcon]="true" dateFormat="yy-mm-dd" class="w-full" />
          </div>
        }

        <div class="flex flex-col gap-2">
          <label for="nudgePerson" class="text-sm font-medium text-muted-color">Person (optional)</label>
          <p-select id="nudgePerson" [options]="personOptions()" [(ngModel)]="selectedPersonId" [showClear]="true" [filter]="true" filterBy="label" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="nudgeInit" class="text-sm font-medium text-muted-color">Initiative (optional)</label>
          <p-select id="nudgeInit" [options]="initiativeOptions()" [(ngModel)]="selectedInitiativeId" [showClear]="true" [filter]="true" filterBy="label" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="nudgeNotes" class="text-sm font-medium text-muted-color">Notes (optional)</label>
          <textarea pTextarea id="nudgeNotes" [(ngModel)]="notes" [rows]="2" maxlength="2000" class="w-full"></textarea>
        </div>
      </div>

      <ng-template #footer>
        <div class="flex justify-end gap-2">
          <p-button label="Cancel" severity="secondary" (onClick)="visible.set(false)" />
          <p-button
            [label]="editNudge() ? 'Save' : 'Create'"
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
export class NudgeEditDialogComponent implements OnInit {
  private readonly service = inject(NudgesService);
  private readonly peopleService = inject(PeopleService);
  private readonly initiativesService = inject(InitiativesService);
  private readonly messageService = inject(MessageService);

  readonly visible = model(false);
  readonly editNudge = model<Nudge | null>(null);
  readonly saved = output<Nudge>();

  protected readonly submitting = signal(false);
  protected readonly personOptions = signal<{ label: string; value: string }[]>([]);
  protected readonly initiativeOptions = signal<{ label: string; value: string }[]>([]);

  protected title = '';
  protected cadenceType: CadenceType = 'Daily';
  protected dayOfWeek: NudgeDayOfWeek = 'Monday';
  protected dayOfMonth: number | null = 1;
  protected customIntervalDays: number | null = 7;
  protected startDate: Date | null = null;
  protected selectedPersonId: string | null = null;
  protected selectedInitiativeId: string | null = null;
  protected notes = '';

  protected readonly cadenceOptions: { label: string; value: CadenceType }[] = [
    { label: 'Daily', value: 'Daily' },
    { label: 'Weekly', value: 'Weekly' },
    { label: 'Biweekly', value: 'Biweekly' },
    { label: 'Monthly', value: 'Monthly' },
    { label: 'Custom (N days)', value: 'Custom' },
  ];

  protected readonly dayOfWeekOptions: { label: string; value: NudgeDayOfWeek }[] = [
    { label: 'Monday', value: 'Monday' },
    { label: 'Tuesday', value: 'Tuesday' },
    { label: 'Wednesday', value: 'Wednesday' },
    { label: 'Thursday', value: 'Thursday' },
    { label: 'Friday', value: 'Friday' },
    { label: 'Saturday', value: 'Saturday' },
    { label: 'Sunday', value: 'Sunday' },
  ];

  constructor() {
    effect(() => {
      const edit = this.editNudge();
      if (edit && this.visible()) {
        this.title = edit.title;
        this.cadenceType = edit.cadence.type;
        this.dayOfWeek = edit.cadence.dayOfWeek ?? 'Monday';
        this.dayOfMonth = edit.cadence.dayOfMonth ?? 1;
        this.customIntervalDays = edit.cadence.customIntervalDays ?? 7;
        this.notes = edit.notes ?? '';
        this.selectedPersonId = edit.personId;
        this.selectedInitiativeId = edit.initiativeId;
      }
    });
  }

  ngOnInit(): void {
    this.peopleService.list().subscribe({
      next: (people) => this.personOptions.set(
        people.filter(p => !p.isArchived).map(p => ({ label: p.name, value: p.id })),
      ),
    });
    this.initiativesService.list().subscribe({
      next: (items) => this.initiativeOptions.set(items.map(i => ({ label: i.title, value: i.id }))),
    });
  }

  protected isValid(): boolean {
    if (this.title.trim().length === 0 || this.title.length > 200) return false;
    switch (this.cadenceType) {
      case 'Weekly':
      case 'Biweekly':
        return !!this.dayOfWeek;
      case 'Monthly':
        return this.dayOfMonth !== null && this.dayOfMonth >= 1 && this.dayOfMonth <= 31;
      case 'Custom':
        return this.customIntervalDays !== null && this.customIntervalDays >= 1 && this.customIntervalDays <= 365;
      default:
        return true;
    }
  }

  protected onSubmit(): void {
    if (!this.isValid()) return;
    this.submitting.set(true);

    const edit = this.editNudge();
    if (edit) {
      // Update details + cadence (two calls, cadence last).
      this.service.update(edit.id, {
        title: this.title.trim(),
        notes: this.notes.trim() || null,
        personId: this.selectedPersonId,
        initiativeId: this.selectedInitiativeId,
      }).subscribe({
        next: () => {
          this.service.updateCadence(edit.id, this.buildCadenceRequest()).subscribe({
            next: (updated) => this.finish(updated),
            error: () => this.fail('Failed to update cadence'),
          });
        },
        error: () => this.fail('Failed to update nudge'),
      });
    } else {
      const request: CreateNudgeRequest = {
        title: this.title.trim(),
        cadenceType: this.cadenceType,
        ...this.buildCadenceRequest(),
        ...(this.startDate && { startDate: this.formatDate(this.startDate) }),
        personId: this.selectedPersonId,
        initiativeId: this.selectedInitiativeId,
        notes: this.notes.trim() || null,
      };
      this.service.create(request).subscribe({
        next: (created) => this.finish(created),
        error: () => this.fail('Failed to create nudge'),
      });
    }
  }

  private buildCadenceRequest() {
    return {
      cadenceType: this.cadenceType,
      dayOfWeek: this.cadenceType === 'Weekly' || this.cadenceType === 'Biweekly' ? this.dayOfWeek : null,
      dayOfMonth: this.cadenceType === 'Monthly' ? this.dayOfMonth : null,
      customIntervalDays: this.cadenceType === 'Custom' ? this.customIntervalDays : null,
    };
  }

  private finish(nudge: Nudge): void {
    this.submitting.set(false);
    this.saved.emit(nudge);
    this.resetForm();
    this.visible.set(false);
  }

  private fail(message: string): void {
    this.submitting.set(false);
    this.messageService.add({ severity: 'error', summary: message });
  }

  private resetForm(): void {
    this.title = '';
    this.cadenceType = 'Daily';
    this.dayOfWeek = 'Monday';
    this.dayOfMonth = 1;
    this.customIntervalDays = 7;
    this.startDate = null;
    this.selectedPersonId = null;
    this.selectedInitiativeId = null;
    this.notes = '';
    this.editNudge.set(null);
  }

  private formatDate(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }
}
