import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  signal,
  OnInit,
  DestroyRef,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, EMPTY, of, debounceTime, switchMap, catchError } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { RadioButtonModule } from 'primeng/radiobutton';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { MessageService, ConfirmationService } from 'primeng/api';
import { AiProviderService } from '../../shared/services/ai-provider.service';
import {
  AI_PROVIDERS,
  AiModelInfo,
  AiProviderType,
  ProviderOption,
} from '../../shared/models/ai-provider.model';

@Component({
  selector: 'app-ai-provider-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    SelectModule,
    RadioButtonModule,
    ToastModule,
    ConfirmDialogModule,
    ProgressSpinnerModule,
  ],
  providers: [ConfirmationService],
  template: `
    <section class="flex flex-col gap-4">
      <h2 class="text-xl font-semibold">AI Provider</h2>

      <!-- Provider Selection Cards -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
        @for (provider of providers; track provider.name) {
          <p-card
            [class]="'cursor-pointer ' + (selectedProvider() === provider.name ? 'border-2 border-primary' : 'border border-surface-200')"
            (click)="selectProvider(provider.name)"
          >
            <div class="flex items-center gap-3">
              <p-radioButton
                [value]="provider.name"
                [(ngModel)]="selectedProviderModel"
                name="provider"
              />
              <i [class]="provider.icon + ' text-xl'"></i>
              <span class="font-medium">{{ provider.label }}</span>
            </div>
          </p-card>
        }
      </div>

      <!-- Configuration Form (visible when provider selected) -->
      @if (selectedProvider()) {
        <div class="flex flex-col gap-4 mt-2">
          <!-- Deep link -->
          @if (selectedProviderOption(); as opt) {
            <a
              [href]="opt.keyUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="text-sm text-primary hover:underline"
            >
              Don't have one? &rarr; {{ opt.keyUrlLabel }}
            </a>
          }

          <!-- API Key Input -->
          <div class="flex flex-col gap-2">
            <label for="apiKey" class="text-sm font-medium text-muted-color">API Key</label>
            <div class="flex items-center gap-2">
              <input
                pInputText
                id="apiKey"
                type="password"
                [(ngModel)]="apiKey"
                (input)="onApiKeyInput()"
                [placeholder]="isConfigured() ? '••••••••••••••••' : 'Paste your API key'"
                class="w-full"
              />
              @if (validating()) {
                <p-progressSpinner
                  [style]="{ width: '24px', height: '24px' }"
                  strokeWidth="4"
                />
              }
              @if (validationResult() === 'success') {
                <i class="pi pi-check-circle text-primary text-xl"></i>
              }
              @if (validationResult() === 'error') {
                <i class="pi pi-times-circle text-danger text-xl"></i>
              }
            </div>
            @if (validationMessage()) {
              <small [class]="validationResult() === 'success' ? 'text-primary' : 'text-danger'">
                {{ validationMessage() }}
              </small>
            }
          </div>

          <!-- Model Dropdown -->
          <div class="flex flex-col gap-2">
            <label for="model" class="text-sm font-medium text-muted-color">Model</label>
            <p-select
              id="model"
              [options]="models()"
              optionLabel="name"
              optionValue="id"
              [(ngModel)]="selectedModel"
              (ngModelChange)="onModelChange()"
              placeholder="Select model"
              class="w-full"
              [loading]="loadingModels()"
            />
          </div>

          <!-- Action Buttons -->
          <div class="flex gap-2">
            <p-button
              label="Save"
              (onClick)="save()"
              [loading]="saving()"
              [disabled]="!canSave()"
            />
            @if (isConfigured()) {
              <p-button
                label="Remove Provider"
                severity="danger"
                [outlined]="true"
                (onClick)="confirmRemove()"
              />
            }
          </div>
        </div>
      }

      <!-- Status when no provider configured -->
      @if (!selectedProvider() && !isConfigured()) {
        <p class="text-muted-color text-sm">AI Provider: Not configured</p>
      }
    </section>
    <p-confirmDialog />
  `,
})
export class AiProviderSettingsComponent implements OnInit {
  private readonly aiProviderService = inject(AiProviderService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly validateSubject = new Subject<void>();
  private statusPopulated = false;

  constructor() {
    // React to status signal changes to pre-populate form
    effect(() => {
      const status = this.aiProviderService.status();
      if (status && !this.statusPopulated) {
        this.statusPopulated = true;
        if (status.isConfigured && status.provider) {
          this.populateFromStatus(status.provider as AiProviderType, status.model);
        }
      }
    });
  }

  readonly providers = AI_PROVIDERS;
  readonly selectedProvider = signal<AiProviderType | null>(null);
  readonly selectedProviderOption = signal<ProviderOption | null>(null);
  readonly models = signal<AiModelInfo[]>([]);
  readonly loadingModels = signal(false);
  readonly validating = signal(false);
  readonly validationResult = signal<'success' | 'error' | null>(null);
  readonly validationMessage = signal<string | null>(null);
  readonly saving = signal(false);
  readonly isConfigured = signal(false);

  protected selectedProviderModel = '';
  protected apiKey = '';
  protected selectedModel = '';

  ngOnInit(): void {
    this.aiProviderService.loadStatus();

    // Set up debounced auto-validate
    this.validateSubject
      .pipe(
        debounceTime(500),
        switchMap(() => {
          const provider = this.selectedProvider();
          const model = this.selectedModel;
          if (!provider || !this.apiKey || this.apiKey.length < 10 || !model) {
            this.validating.set(false);
            this.validationResult.set(null);
            this.validationMessage.set(null);
            return EMPTY;
          }
          this.validating.set(true);
          return this.aiProviderService.validate({
            provider,
            apiKey: this.apiKey,
            model,
          }).pipe(
            catchError(() => of({ success: false as const, error: 'Connection failed', modelName: null })),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.validating.set(false);
        if (result.success) {
          this.validationResult.set('success');
          this.validationMessage.set(result.modelName ? `Connected to ${result.modelName}` : 'Validation successful');
        } else {
          this.validationResult.set('error');
          this.validationMessage.set(result.error ?? 'Validation failed');
        }
      });
  }

  selectProvider(provider: AiProviderType): void {
    this.selectedProvider.set(provider);
    this.selectedProviderModel = provider;
    this.selectedProviderOption.set(
      this.providers.find((p) => p.name === provider) ?? null,
    );
    this.selectedModel = '';
    this.apiKey = '';
    this.loadModels(provider);
    // Reset validation when provider changes
    this.validating.set(false);
    this.validationResult.set(null);
    this.validationMessage.set(null);
  }

  onApiKeyInput(): void {
    // Clear stale validation so canSave() disables Save until the new key is validated
    this.validationResult.set(null);
    this.validationMessage.set(null);
    this.validateSubject.next();
  }

  onModelChange(): void {
    // Clear stale validation when model changes — the previous result was for a different model
    this.validationResult.set(null);
    this.validationMessage.set(null);
    this.validateSubject.next();
  }

  canSave(): boolean {
    const provider = this.selectedProvider();
    const hasModel = !!this.selectedModel;
    const status = this.aiProviderService.status();
    const sameProvider = !!(status?.isConfigured && status.provider === provider);
    // Allow save if: (a) keeping the same provider with existing key (no new key entered),
    // or (b) a new key was entered and validation passed
    const keyValid = (sameProvider && !this.apiKey) || (!!this.apiKey && this.validationResult() === 'success');
    return !!provider && hasModel && keyValid && !this.validating();
  }

  save(): void {
    const provider = this.selectedProvider();
    if (!provider || !this.selectedModel) return;

    this.saving.set(true);
    this.aiProviderService
      .configure({
        provider,
        apiKey: this.apiKey || undefined,
        model: this.selectedModel,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.isConfigured.set(true);
          this.apiKey = '';
          this.messageService.add({
            severity: 'success',
            summary: 'AI provider configured',
          });
        },
        error: () => {
          this.saving.set(false);
          this.messageService.add({
            severity: 'error',
            summary: 'Failed to configure AI provider',
          });
        },
      });
  }

  confirmRemove(): void {
    this.confirmationService.confirm({
      message: 'Remove your AI provider configuration? You can reconfigure it at any time.',
      header: 'Remove AI Provider',
      acceptLabel: 'Remove',
      rejectLabel: 'Cancel',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.remove(),
    });
  }

  private remove(): void {
    this.aiProviderService.remove().subscribe({
      next: () => {
        this.isConfigured.set(false);
        this.selectedProvider.set(null);
        this.selectedProviderModel = '';
        this.apiKey = '';
        this.selectedModel = '';
        this.models.set([]);
        this.validationResult.set(null);
        this.validationMessage.set(null);
        this.messageService.add({
          severity: 'success',
          summary: 'AI provider removed',
        });
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Failed to remove AI provider',
        });
      },
    });
  }

  private loadModels(provider: AiProviderType): void {
    this.loadingModels.set(true);
    this.aiProviderService.getModels(provider).subscribe({
      next: (response) => {
        this.models.set(response.models);
        this.loadingModels.set(false);
        // Auto-select default model if none selected
        if (!this.selectedModel) {
          const defaultModel = response.models.find((m) => m.isDefault);
          if (defaultModel) {
            this.selectedModel = defaultModel.id;
          }
        }
      },
      error: () => this.loadingModels.set(false),
    });
  }

  private populateFromStatus(provider: AiProviderType, model: string | null): void {
    this.isConfigured.set(true);
    this.selectProvider(provider);
    if (model) {
      this.selectedModel = model;
    }
  }
}
