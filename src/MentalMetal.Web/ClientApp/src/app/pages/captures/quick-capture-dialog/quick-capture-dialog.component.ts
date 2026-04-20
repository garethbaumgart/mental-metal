import { ChangeDetectionStrategy, Component, ElementRef, effect, inject, model, output, signal, viewChild, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { PanelModule } from 'primeng/panel';
import { ToastModule } from 'primeng/toast';
import { MessageModule } from 'primeng/message';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { CapturesService } from '../../../shared/services/captures.service';
import { CaptureProcessingTrackerService } from '../../../shared/services/capture-processing-tracker.service';
import { AudioRecorderService } from '../../../shared/services/audio-recorder.service';
import { DeepgramTranscriptionService } from '../../../shared/services/deepgram-transcription.service';
import { Capture } from '../../../shared/models/capture.model';

const ACCEPTED_EXTENSIONS = new Set(['.txt', '.html', '.htm', '.docx']);

type CaptureMode = 'type' | 'voice';
type VoiceState = 'idle' | 'recording' | 'done';

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
    PanelModule,
    ToastModule,
    MessageModule,
    TooltipModule,
  ],
  providers: [MessageService],
  template: `
    <p-toast />
    <p-dialog
      header="Quick Capture"
      [visible]="visible()"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [style]="{ width: '32rem' }"
    >
      <div class="flex flex-col gap-4 pt-4" (keydown)="onDialogKeydown($event)">
        <!-- Mode toggle -->
        <div class="flex items-center gap-2">
          <p-button
            icon="pi pi-pencil"
            label="Type"
            [severity]="captureMode() === 'type' ? 'primary' : 'secondary'"
            [outlined]="captureMode() !== 'type'"
            size="small"
            (onClick)="setCaptureMode('type')"
          />
          <p-button
            icon="pi pi-microphone"
            label="Voice"
            [severity]="captureMode() === 'voice' ? 'primary' : 'secondary'"
            [outlined]="captureMode() !== 'voice'"
            size="small"
            (onClick)="setCaptureMode('voice')"
            [disabled]="!deepgramAvailable()"
            [pTooltip]="deepgramAvailable() ? '' : 'Configure Deepgram in Settings to enable voice capture'"
          />
        </div>

        @if (captureMode() === 'type') {
          <!-- Type mode (existing behavior) -->
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
              placeholder="Paste text, meeting notes, or a quick thought..."
            ></textarea>
          </div>

          <p-panel header="Advanced" [toggleable]="true" [collapsed]="true">
            <div class="flex flex-col gap-4">
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
                <label for="captureTitle" class="text-sm font-medium text-muted-color">Title (optional)</label>
                <input pInputText id="captureTitle" [(ngModel)]="title" class="w-full" />
              </div>

              <div class="flex flex-col gap-2">
                <label for="captureSource" class="text-sm font-medium text-muted-color">Source (optional)</label>
                <input pInputText id="captureSource" [(ngModel)]="source" class="w-full" placeholder="e.g. weekly 1:1, standup" />
              </div>
            </div>
          </p-panel>
        }

        @if (captureMode() === 'voice') {
          <!-- Voice mode -->
          <div class="flex flex-col items-center gap-4 py-4">
            @if (voiceState() === 'idle') {
              <p-button
                icon="pi pi-microphone"
                [rounded]="true"
                severity="danger"
                class="text-3xl"
                [style]="{ width: '4rem', height: '4rem' }"
                (onClick)="startVoiceRecording()"
                pTooltip="Click to start recording"
              />
              <span class="text-sm text-muted-color">Tap to speak</span>
              @if (voiceError()) {
                <p-message severity="error" [text]="voiceError()!" />
              }
            }

            @if (voiceState() === 'recording') {
              <div class="flex items-center gap-3">
                <span class="inline-block h-3 w-3 rounded-full animate-pulse" style="background: var(--p-red-500)"></span>
                <span class="font-mono text-lg font-semibold">{{ recorder.formattedTime() }}</span>
              </div>

              <!-- Audio levels -->
              <div class="flex items-end gap-0.5 h-6 w-full max-w-xs">
                @for (level of recorder.audioLevels(); track $index) {
                  <div
                    class="flex-1 rounded-sm transition-all duration-75"
                    [style.height.%]="Math.max(8, level * 100)"
                    style="background: var(--p-primary-color); min-width: 2px"
                  ></div>
                }
              </div>

              <!-- Interim transcript -->
              @if (transcription.interimText() || transcription.transcript()) {
                <div class="w-full max-h-32 overflow-y-auto rounded p-3 text-sm" style="background: var(--p-surface-50)">
                  @if (transcription.transcript()) {
                    <p>{{ transcription.transcript() }}</p>
                  }
                  @if (transcription.interimText()) {
                    <p class="text-muted-color italic">{{ transcription.interimText() }}</p>
                  }
                </div>
              }

              <p-button
                icon="pi pi-stop-circle"
                label="Stop"
                severity="secondary"
                (onClick)="stopVoiceRecording()"
              />

              @if (transcription.error()) {
                <p-message severity="warn" [text]="transcription.error()!" />
              }
            }

            @if (voiceState() === 'done') {
              <div class="flex flex-col gap-2 w-full">
                <label class="text-sm font-medium text-muted-color">Transcript (editable)</label>
                <textarea
                  pTextarea
                  [(ngModel)]="rawContent"
                  [rows]="6"
                  class="w-full"
                  placeholder="Voice transcript will appear here..."
                ></textarea>
              </div>
              @if (!rawContent.trim()) {
                <p-message severity="warn" text="No transcript was captured. Try again or switch to Type mode." />
              }
            }
          </div>
        }
      </div>

      <ng-template #footer>
        <div class="flex justify-end gap-2">
          <p-button label="Cancel" severity="secondary" (onClick)="onCancel()" />
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
export class QuickCaptureDialogComponent implements OnDestroy {
  private readonly capturesService = inject(CapturesService);
  private readonly processingTracker = inject(CaptureProcessingTrackerService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  protected readonly recorder = inject(AudioRecorderService);
  protected readonly transcription = inject(DeepgramTranscriptionService);

  readonly visible = model(false);
  readonly created = output<Capture>();

  private readonly contentField = viewChild<ElementRef<HTMLTextAreaElement>>('contentField');

  protected readonly submitting = signal(false);
  protected readonly selectedFile = signal<File | null>(null);
  protected readonly fileError = signal<string | null>(null);
  protected readonly captureMode = signal<CaptureMode>('type');
  protected readonly voiceState = signal<VoiceState>('idle');
  protected readonly voiceError = signal<string | null>(null);
  protected readonly deepgramAvailable = signal(false);

  protected rawContent = '';
  protected title = '';
  protected source = '';

  protected readonly Math = Math;

  constructor() {
    effect(() => {
      if (this.visible()) {
        queueMicrotask(() => this.contentField()?.nativeElement.focus());
        // Check Deepgram availability when dialog opens
        this.checkDeepgramAvailability();
      }
    });
  }

  private async checkDeepgramAvailability(): Promise<void> {
    const available = await this.transcription.checkAvailability();
    this.deepgramAvailable.set(available);
    // Reset transcription error so it doesn't leak into the UI
    if (!available) {
      this.transcription.error.set(null);
    }
  }

  protected setCaptureMode(mode: CaptureMode): void {
    if (this.voiceState() === 'recording') {
      // Don't switch while recording
      return;
    }
    this.captureMode.set(mode);
    if (mode === 'type') {
      queueMicrotask(() => this.contentField()?.nativeElement.focus());
    }
  }

  protected async startVoiceRecording(): Promise<void> {
    this.voiceError.set(null);

    // Wire up PCM forwarding
    this.recorder.onPcmChunk.set((data: ArrayBuffer) => {
      this.transcription.sendRawPcm(data);
    });

    try {
      await this.recorder.start();
    } catch {
      this.recorder.onPcmChunk.set(null);
      this.voiceError.set(this.recorder.error() ?? 'Failed to start recording');
      return;
    }

    if (this.recorder.state() !== 'recording') {
      this.recorder.onPcmChunk.set(null);
      this.voiceError.set(this.recorder.error() ?? 'Microphone access denied. Switch to Type mode.');
      return;
    }

    // Start transcription — single channel, no diarization for voice notes
    this.transcription.start(1, 'You', 'linear16', this.recorder.sampleRate());
    this.voiceState.set('recording');
  }

  protected async stopVoiceRecording(): Promise<void> {
    this.transcription.stop();
    await this.recorder.stop();
    this.recorder.onPcmChunk.set(null);

    // Populate the text area with the transcript
    this.rawContent = this.transcription.transcript();
    this.voiceState.set('done');
  }

  protected onVisibleChange(newVisible: boolean): void {
    if (!newVisible) {
      this.onCancel();
    }
    this.visible.set(newVisible);
  }

  protected onCancel(): void {
    if (this.voiceState() === 'recording') {
      this.transcription.reset();
      this.recorder.discard();
      this.recorder.onPcmChunk.set(null);
    }
    this.voiceState.set('idle');
    this.visible.set(false);
  }

  protected isValid(): boolean {
    return this.rawContent.trim().length > 0 || this.selectedFile() !== null;
  }

  protected onTextareaKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey && !event.metaKey && !event.ctrlKey) {
      if (this.isValid() && !this.selectedFile()) {
        event.preventDefault();
        this.onSubmit();
      }
    }
  }

  protected onDialogKeydown(event: KeyboardEvent): void {
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
    input.value = '';
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

    const isVoice = this.captureMode() === 'voice' && this.voiceState() === 'done';

    const file = this.selectedFile();
    if (file && !isVoice) {
      if (this.rawContent.trim().length > 0) {
        if (!confirm('You have both typed content and an attached file. The file will be imported and the typed content will be discarded. Continue?')) {
          return;
        }
      }
      this.submitting.set(true);
      this.capturesService.importFile(
        file,
        'QuickNote',
        this.title.trim() || undefined,
        this.source.trim() || undefined,
      ).subscribe({
        next: (result) => {
          this.submitting.set(false);
          this.processingTracker.track(result.id);
          this.messageService.add({
            severity: 'info',
            summary: 'Capture saved',
            detail: 'Processing in background...',
            life: 3000,
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
      this.submitting.set(true);
      this.capturesService.create({
        rawContent: this.rawContent.trim(),
        type: 'QuickNote',
        source: isVoice ? 'Voice' : 'Typed',
        ...(this.title.trim() && { title: this.title.trim() }),
      }).subscribe({
        next: (capture) => {
          this.submitting.set(false);
          this.processingTracker.track(capture.id);
          this.messageService.add({
            severity: 'info',
            summary: 'Capture saved',
            detail: 'Processing in background...',
            life: 3000,
          });
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
    this.title = '';
    this.source = '';
    this.selectedFile.set(null);
    this.fileError.set(null);
    this.captureMode.set('type');
    this.voiceState.set('idle');
    this.voiceError.set(null);
    this.transcription.reset();
  }

  ngOnDestroy(): void {
    if (this.voiceState() === 'recording') {
      this.transcription.reset();
      this.recorder.discard();
      this.recorder.onPcmChunk.set(null);
    }
  }
}
