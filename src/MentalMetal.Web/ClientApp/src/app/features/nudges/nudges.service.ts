import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateNudgeRequest,
  ListNudgesFilters,
  Nudge,
  UpdateCadenceRequest,
  UpdateNudgeRequest,
} from './nudges.models';

@Injectable({ providedIn: 'root' })
export class NudgesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/nudges';

  list(filters: ListNudgesFilters = {}): Observable<Nudge[]> {
    let params = new HttpParams();
    if (filters.isActive !== undefined && filters.isActive !== null) {
      params = params.set('isActive', filters.isActive.toString());
    }
    if (filters.personId) {
      params = params.set('personId', filters.personId);
    }
    if (filters.initiativeId) {
      params = params.set('initiativeId', filters.initiativeId);
    }
    if (filters.dueBefore) {
      params = params.set('dueBefore', filters.dueBefore);
    }
    if (filters.dueWithinDays !== undefined && filters.dueWithinDays !== null) {
      params = params.set('dueWithinDays', filters.dueWithinDays.toString());
    }
    return this.http.get<Nudge[]>(this.baseUrl, { params });
  }

  get(id: string): Observable<Nudge> {
    return this.http.get<Nudge>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateNudgeRequest): Observable<Nudge> {
    return this.http.post<Nudge>(this.baseUrl, request);
  }

  update(id: string, request: UpdateNudgeRequest): Observable<Nudge> {
    return this.http.patch<Nudge>(`${this.baseUrl}/${id}`, request);
  }

  updateCadence(id: string, request: UpdateCadenceRequest): Observable<Nudge> {
    return this.http.patch<Nudge>(`${this.baseUrl}/${id}/cadence`, request);
  }

  markNudged(id: string): Observable<Nudge> {
    return this.http.post<Nudge>(`${this.baseUrl}/${id}/mark-nudged`, {});
  }

  pause(id: string): Observable<Nudge> {
    return this.http.post<Nudge>(`${this.baseUrl}/${id}/pause`, {});
  }

  resume(id: string): Observable<Nudge> {
    return this.http.post<Nudge>(`${this.baseUrl}/${id}/resume`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
