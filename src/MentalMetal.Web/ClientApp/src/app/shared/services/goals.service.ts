import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateGoalRequest,
  DeferGoalRequest,
  Goal,
  GoalStatus,
  GoalType,
  PersonEvidenceSummary,
  RecordCheckInRequest,
  UpdateGoalRequest,
} from '../models/goal.model';

@Injectable({ providedIn: 'root' })
export class GoalsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/goals';

  list(personId?: string, goalType?: GoalType, status?: GoalStatus): Observable<Goal[]> {
    let params = new HttpParams();
    if (personId) params = params.set('personId', personId);
    if (goalType) params = params.set('goalType', goalType);
    if (status) params = params.set('status', status);
    return this.http.get<Goal[]>(this.baseUrl, { params });
  }

  get(id: string): Observable<Goal> {
    return this.http.get<Goal>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateGoalRequest): Observable<Goal> {
    return this.http.post<Goal>(this.baseUrl, request);
  }

  update(id: string, request: UpdateGoalRequest): Observable<Goal> {
    return this.http.put<Goal>(`${this.baseUrl}/${id}`, request);
  }

  achieve(id: string): Observable<Goal> {
    return this.http.post<Goal>(`${this.baseUrl}/${id}/achieve`, {});
  }

  miss(id: string): Observable<Goal> {
    return this.http.post<Goal>(`${this.baseUrl}/${id}/miss`, {});
  }

  defer(id: string, request: DeferGoalRequest): Observable<Goal> {
    return this.http.post<Goal>(`${this.baseUrl}/${id}/defer`, request);
  }

  reactivate(id: string): Observable<Goal> {
    return this.http.post<Goal>(`${this.baseUrl}/${id}/reactivate`, {});
  }

  recordCheckIn(id: string, request: RecordCheckInRequest): Observable<Goal> {
    return this.http.post<Goal>(`${this.baseUrl}/${id}/check-ins`, request);
  }

  getPersonEvidenceSummary(personId: string, from?: string, to?: string): Observable<PersonEvidenceSummary> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<PersonEvidenceSummary>(`/api/people/${personId}/evidence-summary`, { params });
  }
}
