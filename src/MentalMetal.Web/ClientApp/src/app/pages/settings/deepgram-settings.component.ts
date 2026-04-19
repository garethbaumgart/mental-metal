import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnInit,
  signal,
  DestroyRef,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Subject, EMPTY, of, debounceTime, switchMap, catchError } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { MessageModule } from 'primeng/message';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';

interface TranscriptionProviderStatus {
  isConfigured: boolean;
  provider: string | null;
  model: string | null;
}

interface ValidateResponse {
  success: boolean;
  error: string | null;
}

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
    ConfirmDialogModule,
  ],
  providers: [ConfirmationService],
  template: `
    <section class="flex flex-col gap-4">
      <h2 class="text-xl font-semibold">Deepgram Transcription</h2>

      <p class="text-sm text-muted-color">
        Deepgram provides speech-to-text for meeting recording and voice notes.
        Add your own API key to enable transcription.
      </p>

      <a
        href="https://console.deepgram.com/"
        target="_blank"
        rel="noopener noreferrer"
        class="text-sm text-primary hover:underline"
      >
        Don't have one? &rarr; Open Deepgram Console
      </a>

      <!-- API Key Input -->
      <div class="flex flex-col gap-2">
        <label for="dgApiKey" class="text-sm font-medium text-muted-color">API Key</label>
        <div class="flex items-center gap-2">
          <input
            pInputText
            id="dgApiKey"
            type="password"
            [(ngModel)]="apiKey"
            (input)="onApiKeyInput()"
            [placeholder]="isConfigured() ? '••••••••••••••••' : 'Paste your Deepgram API key'"
            class="w-full"
          />
          @if (validating()) {
            <p-progressSpinner
              [style]="{ width: '24px', height: '24px' }"
              strokeWidth="4"
            />
          }
          @if (validationResult() === 'success') {
            <i class="pi pi-check-circle text-xl" style="color: var(--p-primary-color)"></i>
          }
          @if (validationResult() === 'error') {
            <i class="pi pi-times-circle text-xl" style="color: var(--p-red-500)"></i>
          }
        </div>
        @if (validationMessage()) {
          <small [style.color]="validationResult() === 'success' ? 'var(--p-primary-color)' : 'var(--p-red-500)'">
            {{ validationMessage() }}
          </small>
        }
      </div>

      <!-- Model Input -->
      <div class="flex flex-col gap-2">
        <label for="dgModel" class="text-sm font-medium text-muted-color">Model</label>
        <input
          pInputText
          id="dgModel"
          [(ngModel)]="model"
          placeholder="nova-3"
          class="w-full"
        />
        <small class="text-muted-color">
          Default: nova-3. See Deepgram docs for available models.
        </small>
      </div>

      <!-- Action Buttons -->
      <div class="flex gap-2">
        <p-button
          label="Save"
          (onClick)="save()"
          [loading]="saving()"
          [disabled]="!canSave()"
        />
        <p-button
          label="Test Connection"
          icon="pi pi-sync"
          severity="secondary"
          [outlined]="true"
          (onClick)="testConnection()"
          [loading]="validating()"
        />
        @if (isConfigured()) {
          <p-button
            label="Remove"
            severity="danger"
            [outlined]="true"
            (onClick)="confirmRemove()"
          />
        }
      </div>

      @if (!isConfigured() && !apiKey) {
        <p-message
          severity="info"
          text="Add your Deepgram API key above to enable audio transcription."
        />
      }
    </section>
    <p-confirmDialog />
  `,
})
export class DeepgramSettingsComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly validateSubject = new Subject<void>();

  readonly isConfigured = signal(false);
  readonly validating = signal(false);
  readonly validationResult = signal<'success' | 'error' | null>(null);
  readonly validationMessage = signal<string | null>(null);
  readonly saving = signal(false);

  protected apiKey = '';
  protected model = 'nova-3';

  ngOnInit(): void {
    this.loadStatus();

    this.validateSubject
      .pipe(
        debounceTime(500),
        switchMap(() => {
          if (!this.apiKey || this.apiKey.length < 10) {
            this.validating.set(false);
            this.validationResult.set(null);
            this.validationMessage.set(null);
            return EMPTY;
          }
          this.validating.set(true);
          return this.http
            .post<ValidateResponse>('/api/users/me/transcription-provider/validate', {
              apiKey: this.apiKey,
            })
            .pipe(catchError(() => of({ success: false, error: 'Connection failed' } as ValidateResponse)));
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.validating.set(false);
        if (result.success) {
          this.validationResult.set('success');
          this.validationMessage.set('API key is valid');
        } else {
          this.validationResult.set('error');
          this.validationMessage.set(result.error ?? 'Validation failed');
        }
      });
  }

  onApiKeyInput(): void {
    this.validationResult.set(null);
    this.validationMessage.set(null);
    this.validateSubject.next();
  }

  testConnection(): void {
    this.validateSubject.next();
  }

  canSave(): boolean {
    const hasModel = !!this.model.trim();
    const keyValid =
      (this.isConfigured() && !this.apiKey) ||
      (!!this.apiKey && this.validationResult() === 'success');
    return hasModel && keyValid && !this.validating();
  }

  save(): void {
    if (!this.model.trim()) return;

    this.saving.set(true);
    this.http
      .put('/api/users/me/transcription-provider', {
        provider: 'Deepgram',
        apiKey: this.apiKey || undefined,
        model: this.model.trim(),
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.isConfigured.set(true);
          this.apiKey = '';
          this.messageService.add({
            severity: 'success',
            summary: 'Transcription provider configured',
          });
        },
        error: () => {
          this.saving.set(false);
          this.messageService.add({
            severity: 'error',
            summary: 'Failed to configure transcription provider',
          });
        },
      });
  }

  confirmRemove(): void {
    this.confirmationService.confirm({
      message: 'Remove your Deepgram configuration? You can reconfigure it at any time.',
      header: 'Remove Transcription Provider',
      acceptLabel: 'Remove',
      rejectLabel: 'Cancel',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.remove(),
    });
  }

  private remove(): void {
    this.http.delete('/api/users/me/transcription-provider').subscribe({
      next: () => {
        this.isConfigured.set(false);
        this.apiKey = '';
        this.model = 'nova-3';
        this.validationResult.set(null);
        this.validationMessage.set(null);
        this.messageService.add({
          severity: 'success',
          summary: 'Transcription provider removed',
        });
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Failed to remove transcription provider',
        });
      },
    });
  }

  private loadStatus(): void {
    this.http
      .get<TranscriptionProviderStatus>('/api/users/me/transcription-provider')
      .subscribe({
        next: (status) => {
          if (status.isConfigured) {
            this.isConfigured.set(true);
            this.model = status.model ?? 'nova-3';
          }
        },
        error: () => {
          // Silently handle — user just hasn't configured yet
        },
      });
  }
}
