import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateObservationRequest,
  Observation,
  ObservationTag,
  UpdateObservationRequest,
} from '../models/observation.model';

@Injectable({ providedIn: 'root' })
export class ObservationsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/observations';

  list(personId?: string, tag?: ObservationTag, from?: string, to?: string): Observable<Observation[]> {
    let params = new HttpParams();
    if (personId) params = params.set('personId', personId);
    if (tag) params = params.set('tag', tag);
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<Observation[]>(this.baseUrl, { params });
  }

  get(id: string): Observable<Observation> {
    return this.http.get<Observation>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateObservationRequest): Observable<Observation> {
    return this.http.post<Observation>(this.baseUrl, request);
  }

  update(id: string, request: UpdateObservationRequest): Observable<Observation> {
    return this.http.put<Observation>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
