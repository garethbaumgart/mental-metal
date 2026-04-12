import { ChangeDetectionStrategy, Component, inject, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { CapturesService } from '../../../shared/services/captures.service';
import { Capture, CaptureType } from '../../../shared/models/capture.model';

@Component({
  selector: 'app-quick-capture-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, DialogModule, InputTextModule, TextareaModule, SelectModule, ToastModule],
  providers: [MessageService],
  template: `
    <p-toast />
    <p-dialog
      header="Quick Capture"
      [visible]="visible()"
      (visibleChange)="visible.set($event)"
      [modal]="true"
      [style]="{ width: '32rem' }"
    >
      <div class="flex flex-col gap-4 pt-4">
        <div class="flex flex-col gap-2">
          <label for="captureContent" class="text-sm font-medium text-muted-color">Content *</label>
          <textarea pTextarea id="captureContent" [(ngModel)]="rawContent" [rows]="6" class="w-full" placeholder="Paste text, meeting notes, or a quick thought..."></textarea>
        </div>

        <div class="flex flex-col gap-2">
          <label for="captureType" class="text-sm font-medium text-muted-color">Type *</label>
          <p-select
            id="captureType"
            [options]="typeOptions"
            [(ngModel)]="selectedType"
            placeholder="Select type"
            class="w-full"
          />
        </div>

        <div class="flex flex-col gap-2">
          <label for="captureTitle" class="text-sm font-medium text-muted-color">Title (optional)</label>
          <input pInputText id="captureTitle" [(ngModel)]="title" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="captureSource" class="text-sm font-medium text-muted-color">Source (optional)</label>
          <input pInputText id="captureSource" [(ngModel)]="source" class="w-full" placeholder="e.g. weekly 1:1, standup" />
        </div>
      </div>

      <ng-template #footer>
        <div class="flex justify-end gap-2">
          <p-button label="Cancel" severity="secondary" (onClick)="visible.set(false)" />
          <p-button
            label="Capture"
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
export class QuickCaptureDialogComponent {
  private readonly capturesService = inject(CapturesService);
  private readonly messageService = inject(MessageService);

  readonly visible = model(false);
  readonly created = output<Capture>();

  protected readonly submitting = signal(false);
  protected rawContent = '';
  protected selectedType: CaptureType | null = null;
  protected title = '';
  protected source = '';

  protected readonly typeOptions = [
    { label: 'Quick Note', value: 'QuickNote' as CaptureType },
    { label: 'Transcript', value: 'Transcript' as CaptureType },
    { label: 'Meeting Notes', value: 'MeetingNotes' as CaptureType },
  ];

  protected isValid(): boolean {
    return this.rawContent.trim().length > 0 && this.selectedType !== null;
  }

  protected onSubmit(): void {
    if (!this.isValid() || !this.selectedType) return;

    this.submitting.set(true);
    this.capturesService.create({
      rawContent: this.rawContent.trim(),
      type: this.selectedType,
      ...(this.title.trim() && { title: this.title.trim() }),
      ...(this.source.trim() && { source: this.source.trim() }),
    }).subscribe({
      next: (capture) => {
        this.submitting.set(false);
        this.created.emit(capture);
        this.rawContent = '';
        this.selectedType = null;
        this.title = '';
        this.source = '';
        this.visible.set(false);
      },
      error: () => {
        this.submitting.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to create capture' });
      },
    });
  }
}
