import { ChangeDetectionStrategy, Component, ElementRef, effect, inject, model, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { PanelModule } from 'primeng/panel';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { CapturesService } from '../../../shared/services/captures.service';
import { Capture, CaptureType } from '../../../shared/models/capture.model';

const ACCEPTED_EXTENSIONS = new Set(['.txt', '.html', '.htm', '.docx']);

@Component({
  selector: 'app-quick-capture-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    TextareaModule,
    SelectModule,
    PanelModule,
    ToastModule,
  ],
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
      <!-- keydown captured at dialog level so Cmd/Ctrl+Enter submits even when -->
      <!-- focus is inside a primeng control that swallows the textarea's Enter -->
      <div class="flex flex-col gap-4 pt-4" (keydown)="onDialogKeydown($event)">
        <div class="flex flex-col gap-2">
          <label for="captureContent" class="text-sm font-medium text-muted-color">Content *</label>
          <textarea
            #contentField
            pTextarea
            id="captureContent"
            [(ngModel)]="rawContent"
            (keydown)="onTextareaKeydown($event)"
            [rows]="6"
            class="w-full"
            placeholder="Paste text, meeting notes, or a quick thought…"
          ></textarea>
        </div>

        <p-panel header="Advanced" [toggleable]="true" [collapsed]="true">
          <div class="flex flex-col gap-4">
            <!-- File import drop-zone -->
            <div class="flex flex-col gap-2">
              <label class="text-sm font-medium text-muted-color">Import file</label>
              <div
                class="flex flex-col items-center justify-center gap-2 p-4 rounded-lg text-center cursor-pointer"
                style="border: 2px dashed var(--p-content-border-color)"
                (dragover)="onDragOver($event)"
                (drop)="onFileDrop($event)"
                (click)="fileInput.click()"
              >
                @if (selectedFile()) {
                  <div class="flex items-center gap-2">
                    <i class="pi pi-file"></i>
                    <span class="text-sm font-medium">{{ selectedFile()!.name }}</span>
                    <p-button
                      icon="pi pi-times"
                      [rounded]="true"
                      [text]="true"
                      size="small"
                      severity="secondary"
                      (onClick)="clearFile($event)"
                    />
                  </div>
                } @else {
                  <i class="pi pi-upload text-muted-color"></i>
                  <span class="text-sm text-muted-color">Drop .txt, .html, or .docx file here, or click to browse</span>
                }
              </div>
              <input
                #fileInput
                type="file"
                accept=".txt,.html,.htm,.docx"
                class="hidden"
                (change)="onFileSelected($event)"
              />
              @if (fileError()) {
                <span class="text-sm" style="color: var(--p-red-500)">{{ fileError() }}</span>
              }
            </div>

            <div class="flex flex-col gap-2">
              <label for="captureType" class="text-sm font-medium text-muted-color">Type</label>
              <p-select
                id="captureType"
                [options]="typeOptions"
                [(ngModel)]="selectedType"
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
        </p-panel>
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
  private readonly router = inject(Router);

  readonly visible = model(false);
  readonly created = output<Capture>();

  private readonly contentField = viewChild<ElementRef<HTMLTextAreaElement>>('contentField');

  protected readonly submitting = signal(false);
  protected readonly selectedFile = signal<File | null>(null);
  protected readonly fileError = signal<string | null>(null);
  protected rawContent = '';
  /** Defaults to QuickNote so the happy path requires no categorization (see capture-text spec). */
  protected selectedType: CaptureType = 'QuickNote';
  protected title = '';
  protected source = '';

  protected readonly typeOptions = [
    { label: 'Quick Note', value: 'QuickNote' as CaptureType },
    { label: 'Transcript', value: 'Transcript' as CaptureType },
    { label: 'Meeting Notes', value: 'MeetingNotes' as CaptureType },
  ];

  constructor() {
    // Autofocus the content textarea when the dialog opens so the user can
    // start typing immediately without an extra click (brief: "typing-plus-
    // Enter fast on the happy path").
    effect(() => {
      if (this.visible()) {
        queueMicrotask(() => this.contentField()?.nativeElement.focus());
      }
    });
  }

  protected isValid(): boolean {
    return this.rawContent.trim().length > 0 || this.selectedFile() !== null;
  }

  protected onTextareaKeydown(event: KeyboardEvent): void {
    // Plain Enter submits; Shift+Enter inserts a newline as normal.
    if (event.key === 'Enter' && !event.shiftKey && !event.metaKey && !event.ctrlKey) {
      if (this.isValid() && !this.selectedFile()) {
        event.preventDefault();
        this.onSubmit();
      }
    }
  }

  protected onDialogKeydown(event: KeyboardEvent): void {
    // Cmd/Ctrl+Enter submits from any field inside the dialog.
    if (event.key === 'Enter' && (event.metaKey || event.ctrlKey)) {
      if (this.isValid()) {
        event.preventDefault();
        this.onSubmit();
      }
    }
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
  }

  protected onFileDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    const file = event.dataTransfer?.files[0];
    if (file) this.validateAndSetFile(file);
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.validateAndSetFile(file);
    input.value = ''; // reset so the same file can be selected again
  }

  protected clearFile(event: Event): void {
    event.stopPropagation();
    this.selectedFile.set(null);
    this.fileError.set(null);
  }

  private validateAndSetFile(file: File): void {
    const ext = '.' + file.name.split('.').pop()?.toLowerCase();
    if (!ACCEPTED_EXTENSIONS.has(ext)) {
      this.fileError.set(`Unsupported file type: ${ext}. Accepted: .txt, .html, .docx`);
      return;
    }
    this.fileError.set(null);
    this.selectedFile.set(file);
  }

  protected onSubmit(): void {
    if (!this.isValid()) return;

    const file = this.selectedFile();
    if (file) {
      // File upload path — goes to /api/captures/import
      if (this.rawContent.trim().length > 0) {
        // Both typed content and a file — confirm before discarding text
        if (!confirm('You have both typed content and an attached file. The file will be imported and the typed content will be discarded. Continue?')) {
          return;
        }
      }
      this.submitting.set(true);
      this.capturesService.importFile(file, this.selectedType).subscribe({
        next: (result) => {
          this.submitting.set(false);
          this.messageService.add({
            severity: 'success',
            summary: 'File imported',
            detail: 'Click to view capture',
            life: 5000,
          });
          this.resetDraft();
          this.visible.set(false);
        },
        error: (err) => {
          this.submitting.set(false);
          const detail = err?.error?.error || err?.error?.detail || 'Failed to import file';
          this.messageService.add({ severity: 'error', summary: detail });
        },
      });
    } else {
      // Text capture path — existing POST /api/captures
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
          this.resetDraft();
          this.visible.set(false);
        },
        error: () => {
          this.submitting.set(false);
          this.messageService.add({ severity: 'error', summary: 'Failed to create capture' });
        },
      });
    }
  }

  private resetDraft(): void {
    this.rawContent = '';
    this.selectedType = 'QuickNote';
    this.title = '';
    this.source = '';
    this.selectedFile.set(null);
    this.fileError.set(null);
  }
}
