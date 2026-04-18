import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnDestroy,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { TextareaModule } from 'primeng/textarea';
import { AudioRecorderService } from '../../../shared/services/audio-recorder.service';
import { DeepgramTranscriptionService } from '../../../shared/services/deepgram-transcription.service';
import { CapturesService } from '../../../shared/services/captures.service';
import { Capture } from '../../../shared/models/capture.model';

type PanelState = 'idle' | 'requesting-permissions' | 'recording' | 'stopping' | 'review' | 'saving';

/**
 * Inline recording panel for the Captures page. Captures mic (and optionally
 * tab audio), streams PCM to Deepgram for real-time transcription, and saves
 * the final transcript as a Capture on completion.
 */
@Component({
  selector: 'app-recording-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, MessageModule, TextareaModule],
  template: `
    @switch (panelState()) {
      @case ('idle') {
        <div class="flex items-center gap-3 rounded-lg border p-4" style="border-color: var(--p-content-border-color); background: var(--p-surface-0)">
          <p-button
            label="Record Meeting"
            icon="pi pi-microphone"
            severity="danger"
            (onClick)="startWithSystemAudio()"
          />
          <p-button
            label="Mic Only"
            icon="pi pi-microphone"
            severity="secondary"
            [outlined]="true"
            (onClick)="startMicOnly()"
          />
          @if (deepgramError()) {
            <span class="text-sm" style="color: var(--p-red-500)">{{ deepgramError() }}</span>
          }
        </div>
      }

      @case ('requesting-permissions') {
        <div class="flex items-center gap-3 rounded-lg border p-4" style="border-color: var(--p-content-border-color); background: var(--p-surface-0)">
          <i class="pi pi-spinner pi-spin text-xl text-muted-color"></i>
          <span class="text-muted-color">Requesting permissions...</span>
        </div>
      }

      @case ('recording') {
        <div class="flex flex-col gap-4 rounded-lg border p-4" style="border-color: var(--p-primary-color); background: var(--p-surface-0)">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-3">
              <span class="inline-block h-3 w-3 rounded-full animate-pulse" style="background: var(--p-red-500)"></span>
              <span class="font-mono text-lg font-semibold">{{ recorder.formattedTime() }}</span>
              @if (recorder.hasSystemAudio()) {
                <span class="text-sm text-muted-color">Mic + Tab Audio</span>
              } @else {
                <span class="text-sm text-muted-color">Mic Only</span>
              }
            </div>
            <div class="flex items-center gap-2">
              <p-button
                label="Stop"
                icon="pi pi-stop-circle"
                severity="secondary"
                (onClick)="stopRecording()"
              />
              <p-button
                label="Cancel"
                icon="pi pi-times"
                severity="danger"
                [outlined]="true"
                (onClick)="discardRecording()"
              />
            </div>
          </div>

          <!-- Audio level visualiser -->
          <div class="flex items-end gap-0.5 h-8">
            @for (level of recorder.audioLevels(); track $index) {
              <div
                class="flex-1 rounded-sm transition-all duration-75"
                [style.height.%]="Math.max(8, level * 100)"
                style="background: var(--p-primary-color); min-width: 3px"
              ></div>
            }
          </div>

          <!-- Interim transcript -->
          @if (transcription.interimText() || transcription.transcript()) {
            <div class="max-h-48 overflow-y-auto rounded p-3 text-sm" style="background: var(--p-surface-50)">
              @if (transcription.transcript()) {
                <p>{{ transcription.transcript() }}</p>
              }
              @if (transcription.interimText()) {
                <p class="text-muted-color italic">{{ transcription.interimText() }}</p>
              }
            </div>
          }

          <!-- Errors -->
          @if (recorder.error()) {
            <p-message severity="error" [text]="recorder.error()!" />
          }
          @if (transcription.error()) {
            <p-message severity="warn" [text]="transcription.error()!" />
          }
        </div>
      }

      @case ('stopping') {
        <div class="flex items-center gap-3 rounded-lg border p-4" style="border-color: var(--p-content-border-color); background: var(--p-surface-0)">
          <i class="pi pi-spinner pi-spin text-xl text-muted-color"></i>
          <span class="text-muted-color">Finalising transcript...</span>
        </div>
      }

      @case ('review') {
        <div class="flex flex-col gap-4 rounded-lg border p-4" style="border-color: var(--p-content-border-color); background: var(--p-surface-0)">
          <h3 class="text-lg font-semibold">Recording Complete</h3>
          <div class="max-h-64 overflow-y-auto rounded p-3 text-sm" style="background: var(--p-surface-50)">
            @if (reviewTranscript()) {
              <p class="whitespace-pre-wrap">{{ reviewTranscript() }}</p>
            } @else {
              <p class="text-muted-color italic">No transcript was captured.</p>
            }
          </div>
          <div class="flex items-center gap-2">
            <p-button
              label="Save as Capture"
              icon="pi pi-check"
              (onClick)="saveTranscript()"
              [disabled]="!reviewTranscript()"
              [loading]="panelState() === 'saving'"
            />
            <p-button
              label="Discard"
              icon="pi pi-trash"
              severity="danger"
              [outlined]="true"
              (onClick)="discardReview()"
            />
          </div>
        </div>
      }

      @case ('saving') {
        <div class="flex items-center gap-3 rounded-lg border p-4" style="border-color: var(--p-content-border-color); background: var(--p-surface-0)">
          <i class="pi pi-spinner pi-spin text-xl text-muted-color"></i>
          <span class="text-muted-color">Saving capture...</span>
        </div>
      }
    }
  `,
})
export class RecordingPanelComponent implements OnDestroy {
  protected readonly recorder = inject(AudioRecorderService);
  protected readonly transcription = inject(DeepgramTranscriptionService);
  private readonly capturesService = inject(CapturesService);
  private readonly destroyRef = inject(DestroyRef);

  readonly saved = output<Capture>();

  readonly panelState = signal<PanelState>('idle');
  readonly reviewTranscript = signal('');
  readonly deepgramError = signal<string | null>(null);

  protected readonly Math = Math;

  async startWithSystemAudio(): Promise<void> {
    await this.startRecording(true);
  }

  async startMicOnly(): Promise<void> {
    await this.startRecording(false);
  }

  private async startRecording(withSystemAudio: boolean): Promise<void> {
    this.panelState.set('requesting-permissions');
    this.deepgramError.set(null);

    // Check Deepgram availability first
    const available = await this.transcription.checkAvailability();
    if (!available) {
      this.deepgramError.set(this.transcription.error() ?? 'Transcription service unavailable');
      this.panelState.set('idle');
      return;
    }

    // Wire up PCM chunk forwarding
    this.recorder.onPcmChunk.set((data: ArrayBuffer) => {
      this.transcription.sendRawPcm(data);
    });

    try {
      if (withSystemAudio) {
        await this.recorder.startWithSystemAudio();
      } else {
        await this.recorder.start();
      }
    } catch {
      this.recorder.onPcmChunk.set(null);
      this.panelState.set('idle');
      return;
    }

    if (this.recorder.state() !== 'recording') {
      // Permission denied or other error
      this.recorder.onPcmChunk.set(null);
      this.panelState.set('idle');
      return;
    }

    // Start transcription with correct channel count
    this.transcription.start(
      this.recorder.channelCount(),
      'You',
      'linear16',
      this.recorder.sampleRate(),
    );

    this.panelState.set('recording');
  }

  async stopRecording(): Promise<void> {
    this.panelState.set('stopping');
    this.transcription.stop();
    await this.recorder.stop();

    const transcript = this.transcription.labeledTranscript();
    this.reviewTranscript.set(transcript);
    this.panelState.set('review');
  }

  discardRecording(): void {
    this.transcription.reset();
    this.recorder.discard();
    this.recorder.onPcmChunk.set(null);
    this.panelState.set('idle');
  }

  saveTranscript(): void {
    const content = this.reviewTranscript();
    if (!content) return;

    this.panelState.set('saving');
    this.capturesService
      .create({
        rawContent: content,
        type: 'Transcript',
        source: 'AudioCapture',
        title: `Meeting recording ${new Date().toLocaleDateString()}`,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (capture) => {
          this.saved.emit(capture);
          this.reviewTranscript.set('');
          this.transcription.reset();
          this.recorder.onPcmChunk.set(null);
          this.panelState.set('idle');
        },
        error: () => {
          this.deepgramError.set('Failed to save capture. Please try again.');
          this.panelState.set('review');
        },
      });
  }

  discardReview(): void {
    this.reviewTranscript.set('');
    this.transcription.reset();
    this.recorder.onPcmChunk.set(null);
    this.panelState.set('idle');
  }

  ngOnDestroy(): void {
    if (this.panelState() === 'recording') {
      this.transcription.reset();
      this.recorder.discard();
      this.recorder.onPcmChunk.set(null);
    }
  }
}
