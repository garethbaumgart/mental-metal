import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MultiSelectModule } from 'primeng/multiselect';
import { PeopleService } from '../../shared/services/people.service';
import { InitiativesService } from '../../shared/services/initiatives.service';
import { CloseOutQueueItem, ReassignCaptureRequest } from './daily-close-out.models';

@Component({
  selector: 'app-reassign-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, DialogModule, MultiSelectModule],
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="visibleChange.emit($event)"
      header="Reassign capture"
      [modal]="true"
      [style]="{ width: '520px' }"
    >
      <div class="flex flex-col gap-4">
        <div class="flex flex-col gap-2">
          <label for="people" class="text-sm font-medium text-muted-color">People</label>
          <p-multiSelect
            id="people"
            [options]="peopleOptions()"
            [(ngModel)]="personIds"
            optionLabel="label"
            optionValue="value"
            placeholder="Select people"
            class="w-full"
          />
        </div>
        <div class="flex flex-col gap-2">
          <label for="initiatives" class="text-sm font-medium text-muted-color">Initiatives</label>
          <p-multiSelect
            id="initiatives"
            [options]="initiativeOptions()"
            [(ngModel)]="initiativeIds"
            optionLabel="label"
            optionValue="value"
            placeholder="Select initiatives"
            class="w-full"
          />
        </div>
      </div>
      <ng-template #footer>
        <p-button label="Cancel" [text]="true" (onClick)="visibleChange.emit(false)" />
        <p-button label="Apply" (onClick)="apply()" />
      </ng-template>
    </p-dialog>
  `,
})
export class ReassignDialogComponent {
  private readonly peopleService = inject(PeopleService);
  private readonly initiativesService = inject(InitiativesService);

  readonly visible = input.required<boolean>();
  readonly capture = input<CloseOutQueueItem | null>(null);
  readonly visibleChange = output<boolean>();
  readonly applied = output<ReassignCaptureRequest>();

  protected readonly peopleOptions = signal<Array<{ label: string; value: string }>>([]);
  protected readonly initiativeOptions = signal<Array<{ label: string; value: string }>>([]);
  protected personIds: string[] = [];
  protected initiativeIds: string[] = [];

  constructor() {
    this.peopleService.list().subscribe({
      next: (people) =>
        this.peopleOptions.set(people.map((p) => ({ label: p.name, value: p.id }))),
    });
    this.initiativesService.list().subscribe({
      next: (initiatives) =>
        this.initiativeOptions.set(initiatives.map((i) => ({ label: i.title, value: i.id }))),
    });

    // Reset selections when the capture changes.
    effect(() => {
      const c = this.capture();
      this.personIds = c ? [...c.linkedPersonIds] : [];
      this.initiativeIds = c ? [...c.linkedInitiativeIds] : [];
    });
  }

  protected apply(): void {
    this.applied.emit({ personIds: this.personIds, initiativeIds: this.initiativeIds });
  }
}
