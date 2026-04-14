import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { catchError, EMPTY, finalize, tap } from 'rxjs';
import { MyQueueFilterParams, MyQueueResponse } from './my-queue.models';

@Injectable({ providedIn: 'root' })
export class MyQueueService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/my-queue';

  readonly response = signal<MyQueueResponse | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  load(filters: MyQueueFilterParams = {}): void {
    this.loading.set(true);
    this.error.set(null);

    let params = new HttpParams();
    if (filters.scope) {
      params = params.set('scope', filters.scope.toLowerCase());
    }
    if (filters.itemType && filters.itemType.length > 0) {
      for (const t of filters.itemType) {
        params = params.append('itemType', t.toLowerCase());
      }
    }
    if (filters.personId) {
      params = params.set('personId', filters.personId);
    }
    if (filters.initiativeId) {
      params = params.set('initiativeId', filters.initiativeId);
    }

    this.http.get<MyQueueResponse>(this.baseUrl, { params })
      .pipe(
        tap((res) => this.response.set(res)),
        catchError(() => {
          this.error.set('Failed to load queue.');
          return EMPTY;
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe();
  }
}
