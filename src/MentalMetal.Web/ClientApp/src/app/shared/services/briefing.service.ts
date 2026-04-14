import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { Briefing, BriefingSummary, BriefingType } from '../models/briefing.model';

@Injectable({ providedIn: 'root' })
export class BriefingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/briefings';

  // Last successfully loaded briefing per type, exposed as signals so widgets
  // and pages can consume them without re-issuing HTTP themselves.
  readonly latestMorning = signal<Briefing | null>(null);
  readonly latestWeekly = signal<Briefing | null>(null);

  loadMorning(force = false): Observable<Briefing> {
    return this.postBriefing<Briefing>('/morning', force).pipe(
      tap((b) => this.latestMorning.set(b)),
    );
  }

  loadWeekly(force = false): Observable<Briefing> {
    return this.postBriefing<Briefing>('/weekly', force).pipe(
      tap((b) => this.latestWeekly.set(b)),
    );
  }

  loadOneOnOnePrep(personId: string, force = false): Observable<Briefing> {
    return this.postBriefing<Briefing>(`/one-on-one/${personId}`, force);
  }

  recent(type?: BriefingType, limit?: number): Observable<BriefingSummary[]> {
    let params = new HttpParams();
    if (type) params = params.set('type', type);
    if (limit !== undefined) params = params.set('limit', String(limit));
    return this.http.get<BriefingSummary[]>(`${this.baseUrl}/recent`, { params });
  }

  getById(id: string): Observable<Briefing> {
    return this.http.get<Briefing>(`${this.baseUrl}/${id}`);
  }

  private postBriefing<T>(path: string, force: boolean): Observable<T> {
    const params = force ? new HttpParams().set('force', 'true') : undefined;
    return this.http.post<T>(`${this.baseUrl}${path}`, null, { params });
  }
}
