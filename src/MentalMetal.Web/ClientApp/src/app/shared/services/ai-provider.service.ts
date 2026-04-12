import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import {
  AiProviderStatus,
  AvailableModelsResponse,
  ConfigureAiProviderRequest,
  ValidateAiProviderRequest,
  ValidateAiProviderResponse,
} from '../models/ai-provider.model';

@Injectable({ providedIn: 'root' })
export class AiProviderService {
  private readonly http = inject(HttpClient);

  readonly status = signal<AiProviderStatus | null>(null);
  readonly loading = signal(false);

  loadStatus(): void {
    this.loading.set(true);
    this.http.get<AiProviderStatus>('/api/users/me/ai-provider').subscribe({
      next: (status) => {
        this.status.set(status);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  configure(request: ConfigureAiProviderRequest): Observable<void> {
    return this.http.put<void>('/api/users/me/ai-provider', request).pipe(
      tap(() => this.loadStatus()),
    );
  }

  validate(request: ValidateAiProviderRequest): Observable<ValidateAiProviderResponse> {
    return this.http.post<ValidateAiProviderResponse>('/api/users/me/ai-provider/validate', request);
  }

  remove(): Observable<void> {
    return this.http.delete<void>('/api/users/me/ai-provider').pipe(
      tap(() => this.loadStatus()),
    );
  }

  getModels(provider: string): Observable<AvailableModelsResponse> {
    return this.http.get<AvailableModelsResponse>('/api/ai/models', {
      params: { provider },
    });
  }
}
