import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { MessageModule } from 'primeng/message';
import { MessageService } from 'primeng/api';

/**
 * Settings section for Deepgram API key configuration. Shows connection
 * status and allows testing the key. Deepgram is configured at the
 * application level (appsettings.json / environment variable), so this
 * component only checks status — it does not write the key.
 */
@Component({
  selector: 'app-deepgram-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ButtonModule,
    InputTextModule,
    ProgressSpinnerModule,
    MessageModule,
  ],
  template: `
    <section class="flex flex-col gap-4">
      <h2 class="text-xl font-semibold">Deepgram Transcription</h2>

      <p class="text-sm text-muted-color">
        Deepgram provides real-time speech-to-text for meeting recording and voice notes.
        The API key is configured at the server level.
      </p>

      <div class="flex items-center gap-3">
        @if (checking()) {
          <p-progressSpinner
            [style]="{ width: '24px', height: '24px' }"
            strokeWidth="4"
          />
          <span class="text-sm text-muted-color">Checking connection...</span>
        } @else if (status() === 'connected') {
          <i class="pi pi-check-circle text-xl" style="color: var(--p-primary-color)"></i>
          <span class="text-sm font-medium" style="color: var(--p-primary-color)">Deepgram connected</span>
        } @else if (status() === 'not-configured') {
          <i class="pi pi-info-circle text-xl text-muted-color"></i>
          <span class="text-sm text-muted-color">Not configured</span>
        } @else if (status() === 'error') {
          <i class="pi pi-times-circle text-xl" style="color: var(--p-red-500)"></i>
          <span class="text-sm" style="color: var(--p-red-500)">{{ errorMessage() }}</span>
        }

        <p-button
          label="Test Connection"
          icon="pi pi-sync"
          severity="secondary"
          [outlined]="true"
          size="small"
          (onClick)="checkStatus()"
          [loading]="checking()"
        />
      </div>

      @if (status() === 'not-configured') {
        <p-message
          severity="info"
          text="Set the Deepgram__ApiKey environment variable or configure Deepgram:ApiKey in appsettings to enable transcription."
        />
      }
    </section>
  `,
})
export class DeepgramSettingsComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messageService = inject(MessageService);

  readonly checking = signal(false);
  readonly status = signal<'unknown' | 'connected' | 'not-configured' | 'error'>('unknown');
  readonly errorMessage = signal('');

  ngOnInit(): void {
    this.checkStatus();
  }

  checkStatus(): void {
    this.checking.set(true);
    this.http.get<{ available: boolean; reason?: string }>('/api/transcription/status').subscribe({
      next: (response) => {
        this.checking.set(false);
        if (response.available) {
          this.status.set('connected');
        } else {
          if (response.reason?.includes('not configured')) {
            this.status.set('not-configured');
          } else {
            this.status.set('error');
            this.errorMessage.set(response.reason ?? 'Unknown error');
          }
        }
      },
      error: () => {
        this.checking.set(false);
        this.status.set('error');
        this.errorMessage.set('Could not reach transcription service');
      },
    });
  }
}
