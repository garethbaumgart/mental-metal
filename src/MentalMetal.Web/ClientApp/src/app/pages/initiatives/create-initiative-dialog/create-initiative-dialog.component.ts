import { ChangeDetectionStrategy, Component, inject, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InitiativesService } from '../../../shared/services/initiatives.service';
import { Initiative } from '../../../shared/models/initiative.model';

@Component({
  selector: 'app-create-initiative-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, DialogModule, InputTextModule],
  template: `
    <p-dialog
      header="New Initiative"
      [visible]="visible()"
      (visibleChange)="visible.set($event)"
      [modal]="true"
      [style]="{ width: '28rem' }"
    >
      <div class="flex flex-col gap-4 pt-4">
        <div class="flex flex-col gap-2">
          <label for="initiativeTitle" class="text-sm font-medium text-muted-color">Title *</label>
          <input pInputText id="initiativeTitle" [(ngModel)]="title" class="w-full" />
        </div>
      </div>

      <ng-template #footer>
        <div class="flex justify-end gap-2">
          <p-button label="Cancel" severity="secondary" (onClick)="visible.set(false)" />
          <p-button
            label="Create"
            (onClick)="onSubmit()"
            [loading]="submitting()"
            [disabled]="!isValid()"
          />
        </div>
      </ng-template>
    </p-dialog>
  `,
})
export class CreateInitiativeDialogComponent {
  private readonly initiativesService = inject(InitiativesService);

  readonly visible = model(false);
  readonly created = output<Initiative>();

  protected readonly submitting = signal(false);
  protected title = '';

  protected isValid(): boolean {
    return this.title.trim().length > 0;
  }

  protected onSubmit(): void {
    if (!this.isValid()) return;

    this.submitting.set(true);
    this.initiativesService.create({ title: this.title.trim() }).subscribe({
      next: (initiative) => {
        this.submitting.set(false);
        this.created.emit(initiative);
        this.title = '';
        this.visible.set(false);
      },
      error: () => this.submitting.set(false),
    });
  }
}
