import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom, Observable } from 'rxjs';
import { CapturesService } from '../../shared/services/captures.service';
import {
  CloseOutDayRequest,
  CloseOutQueueResponse,
  DailyCloseOutLog,
  ReassignCaptureRequest,
} from './daily-close-out.models';
import { CloseOutQueueItem } from './daily-close-out.models';

export interface ProcessAllRawResult {
  /** How many capture IDs were attempted before any short-circuit. */
  attempted: number;
  succeeded: number;
  failed: number;
  /**
   * True when any capture failed with 409 `ai_provider_not_configured`.
   * Callers should surface the "configure provider" UX and skip the
   * usual summary toast.
   */
  providerNotConfigured: boolean;
}

@Injectable({ providedIn: 'root' })
export class DailyCloseOutService {
  private readonly http = inject(HttpClient);
  private readonly captures = inject(CapturesService);
  private readonly baseUrl = '/api/daily-close-out';

  /**
   * Fan out AI extraction across a list of Raw capture IDs with bounded
   * parallelism. Errors on individual captures don't abort the batch;
   * the caller receives a summary with success / failure counts.
   *
   * If any capture fails with the well-known
   * `ai_provider_not_configured` code, the worker pool stops dispatching
   * new work. Note: requests already in flight at the moment the flag
   * flips will still complete — the guarantee is "no more requests are
   * started," not "pending requests are cancelled."
   *
   * The optional `onItemDone` callback fires after each capture
   * completes (success or failure) so callers can refresh per-card
   * state during a long-running batch.
   */
  async processAllRaw(
    captureIds: string[],
    parallelism = 3,
    onItemDone?: (id: string) => void,
  ): Promise<ProcessAllRawResult> {
    const result: ProcessAllRawResult = {
      attempted: 0,
      succeeded: 0,
      failed: 0,
      providerNotConfigured: false,
    };
    if (captureIds.length === 0) return result;

    // Clamp so callers passing 0 / negative don't accidentally create an
    // empty worker pool and leave every capture un-processed.
    const workerCount = Math.max(1, Math.min(parallelism, captureIds.length));

    let cursor = 0;
    const worker = async (): Promise<void> => {
      while (cursor < captureIds.length && !result.providerNotConfigured) {
        const id = captureIds[cursor++];
        result.attempted++;
        try {
          await firstValueFrom(this.captures.process(id));
          result.succeeded++;
        } catch (err) {
          if (
            err instanceof HttpErrorResponse &&
            err.status === 409 &&
            (err.error as { code?: string } | null)?.code === 'ai_provider_not_configured'
          ) {
            result.providerNotConfigured = true;
            onItemDone?.(id);
            return;
          }
          result.failed++;
        }
        onItemDone?.(id);
      }
    };

    await Promise.all(Array.from({ length: workerCount }, () => worker()));
    return result;
  }

  getQueue(): Observable<CloseOutQueueResponse> {
    return this.http.get<CloseOutQueueResponse>(`${this.baseUrl}/queue`);
  }

  quickDiscard(captureId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/captures/${captureId}/quick-discard`, {});
  }

  reassign(captureId: string, request: ReassignCaptureRequest): Observable<CloseOutQueueItem> {
    return this.http.post<CloseOutQueueItem>(
      `${this.baseUrl}/captures/${captureId}/reassign`,
      request,
    );
  }

  closeOutDay(request: CloseOutDayRequest = {}): Observable<DailyCloseOutLog> {
    return this.http.post<DailyCloseOutLog>(`${this.baseUrl}/close`, request);
  }

  getLog(limit?: number): Observable<DailyCloseOutLog[]> {
    let params = new HttpParams();
    if (limit !== undefined) {
      params = params.set('limit', String(limit));
    }
    return this.http.get<DailyCloseOutLog[]>(`${this.baseUrl}/log`, { params });
  }
}
