import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CloseOutDayRequest,
  CloseOutQueueResponse,
  DailyCloseOutLog,
  ReassignCaptureRequest,
} from './daily-close-out.models';
import { CloseOutQueueItem } from './daily-close-out.models';

@Injectable({ providedIn: 'root' })
export class DailyCloseOutService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/daily-close-out';

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
