import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateInitiativeRequest,
  Initiative,
  InitiativeStatus,
  LinkPersonRequest,
  MilestoneRequest,
  UpdateTitleRequest,
} from '../models/initiative.model';

@Injectable({ providedIn: 'root' })
export class InitiativesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/initiatives';

  list(status?: InitiativeStatus): Observable<Initiative[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<Initiative[]>(this.baseUrl, { params });
  }

  get(id: string): Observable<Initiative> {
    return this.http.get<Initiative>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateInitiativeRequest): Observable<Initiative> {
    return this.http.post<Initiative>(this.baseUrl, request);
  }

  updateTitle(id: string, request: UpdateTitleRequest): Observable<Initiative> {
    return this.http.put<Initiative>(`${this.baseUrl}/${id}`, request);
  }

  changeStatus(id: string, newStatus: InitiativeStatus): Observable<Initiative> {
    return this.http.put<Initiative>(`${this.baseUrl}/${id}/status`, { newStatus });
  }

  addMilestone(id: string, request: MilestoneRequest): Observable<Initiative> {
    return this.http.post<Initiative>(`${this.baseUrl}/${id}/milestones`, request);
  }

  updateMilestone(id: string, milestoneId: string, request: MilestoneRequest): Observable<Initiative> {
    return this.http.put<Initiative>(`${this.baseUrl}/${id}/milestones/${milestoneId}`, request);
  }

  removeMilestone(id: string, milestoneId: string): Observable<Initiative> {
    return this.http.delete<Initiative>(`${this.baseUrl}/${id}/milestones/${milestoneId}`);
  }

  completeMilestone(id: string, milestoneId: string): Observable<Initiative> {
    return this.http.post<Initiative>(`${this.baseUrl}/${id}/milestones/${milestoneId}/complete`, {});
  }

  linkPerson(id: string, personId: string): Observable<Initiative> {
    return this.http.post<Initiative>(`${this.baseUrl}/${id}/link-person`, { personId } as LinkPersonRequest);
  }

  unlinkPerson(id: string, personId: string): Observable<Initiative> {
    return this.http.delete<Initiative>(`${this.baseUrl}/${id}/link-person/${personId}`);
  }
}
