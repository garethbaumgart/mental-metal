import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateInitiativeRequest,
  Initiative,
  InitiativeStatus,
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

  refreshSummary(id: string): Observable<Initiative> {
    return this.http.post<Initiative>(`${this.baseUrl}/${id}/refresh-summary`, {});
  }
}
