import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  inject,
  output,
  signal,
} from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { CapturesService } from '../../../shared/services/captures.service';
import { Capture } from '../../../shared/models/capture.model';

type RecorderState = 'idle' | 'recording' | 'uploading' | 'error';

/**
 * Standalone recorder that uses MediaRecorder to capture audio and POST it
 * to /api/captures/audio. Signal-driven, zoneless-safe: every state
 * mutation goes through .set()/.update(). See capture-audio spec
 * "Audio capture frontend recorder".
 */
@Component({
  selector: 'app-capture-recorder',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, MessageModule],
  template: `
    <div class="flex flex-col gap-3 rounded border border-surface-200 bg-surface-0 p-4">
      <div class="flex items-center gap-3">
        @if (state() === 'idle') {
          <p-button
            label="Record"
            icon="pi pi-microphone"
            severity="danger"
            (onClick)="startRecording()"
          />
        }
        @if (state() === 'recording') {
          <p-button
            label="Stop"
            icon="pi pi-stop-circle"
            severity="secondary"
            (onClick)="stopRecording()"
          />
          <span class="text-sm text-muted-color">{{ formattedDuration() }}</span>
        }
        @if (state() === 'uploading') {
          <p-button label="Uploading…" icon="pi pi-spin pi-spinner" [disabled]="true" />
        }
      </div>
      @if (errorMessage(); as err) {
        <p-message severity="error" [text]="err" />
      }
    </div>
  `,
})
export class CaptureRecorderComponent implements OnDestroy {
  private readonly capturesService = inject(CapturesService);

  readonly uploaded = output<Capture>();

  protected readonly state = signal<RecorderState>('idle');
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly durationSeconds = signal(0);

  protected readonly formattedDuration = computed(() => {
    const total = this.durationSeconds();
    const mm = Math.floor(total / 60)
      .toString()
      .padStart(2, '0');
    const ss = Math.floor(total % 60)
      .toString()
      .padStart(2, '0');
    return `${mm}:${ss}`;
  });

  private recorder: MediaRecorder | null = null;
  private stream: MediaStream | null = null;
  private chunks: Blob[] = [];
  private startedAtMs = 0;
  private tickHandle: ReturnType<typeof setInterval> | null = null;

  async startRecording(): Promise<void> {
    this.errorMessage.set(null);
    try {
      this.stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    } catch (err) {
      this.state.set('error');
      this.errorMessage.set(
        err instanceof Error ? err.message : 'Microphone permission denied.',
      );
      return;
    }

    this.chunks = [];
    this.recorder = new MediaRecorder(this.stream);
    this.recorder.ondataavailable = (e) => {
      if (e.data.size > 0) this.chunks.push(e.data);
    };
    this.recorder.onstop = () => this.onRecorderStopped();
    this.recorder.start();

    this.startedAtMs = Date.now();
    this.durationSeconds.set(0);
    this.tickHandle = setInterval(() => {
      this.durationSeconds.set(Math.floor((Date.now() - this.startedAtMs) / 1000));
    }, 500);

    this.state.set('recording');
  }

  stopRecording(): void {
    if (this.recorder && this.recorder.state !== 'inactive') {
      this.recorder.stop();
    }
    this.stopTick();
    this.stream?.getTracks().forEach((t) => t.stop());
  }

  private onRecorderStopped(): void {
    const blob = new Blob(this.chunks, { type: this.recorder?.mimeType || 'audio/webm' });
    const duration = Math.max(0, (Date.now() - this.startedAtMs) / 1000);
    this.state.set('uploading');

    this.capturesService.uploadAudio(blob, duration).subscribe({
      next: (capture) => {
        this.uploaded.emit(capture);
        this.state.set('idle');
        this.durationSeconds.set(0);
      },
      error: (err) => {
        this.state.set('error');
        this.errorMessage.set(
          err?.error?.message || err?.error?.errorCode || 'Upload failed.',
        );
      },
    });
  }

  private stopTick(): void {
    if (this.tickHandle !== null) {
      clearInterval(this.tickHandle);
      this.tickHandle = null;
    }
  }

  ngOnDestroy(): void {
    this.stopTick();
    this.stream?.getTracks().forEach((t) => t.stop());
  }
}
