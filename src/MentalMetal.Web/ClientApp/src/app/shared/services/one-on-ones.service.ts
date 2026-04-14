import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AddActionItemRequest,
  AddFollowUpRequest,
  CreateOneOnOneRequest,
  OneOnOne,
  UpdateOneOnOneRequest,
} from '../models/one-on-one.model';

@Injectable({ providedIn: 'root' })
export class OneOnOnesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/one-on-ones';

  list(personId?: string): Observable<OneOnOne[]> {
    let params = new HttpParams();
    if (personId) {
      params = params.set('personId', personId);
    }
    return this.http.get<OneOnOne[]>(this.baseUrl, { params });
  }

  get(id: string): Observable<OneOnOne> {
    return this.http.get<OneOnOne>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateOneOnOneRequest): Observable<OneOnOne> {
    return this.http.post<OneOnOne>(this.baseUrl, request);
  }

  update(id: string, request: UpdateOneOnOneRequest): Observable<OneOnOne> {
    return this.http.put<OneOnOne>(`${this.baseUrl}/${id}`, request);
  }

  addActionItem(id: string, request: AddActionItemRequest): Observable<OneOnOne> {
    return this.http.post<OneOnOne>(`${this.baseUrl}/${id}/action-items`, request);
  }

  completeActionItem(id: string, itemId: string): Observable<OneOnOne> {
    return this.http.post<OneOnOne>(`${this.baseUrl}/${id}/action-items/${itemId}/complete`, {});
  }

  removeActionItem(id: string, itemId: string): Observable<OneOnOne> {
    return this.http.delete<OneOnOne>(`${this.baseUrl}/${id}/action-items/${itemId}`);
  }

  addFollowUp(id: string, request: AddFollowUpRequest): Observable<OneOnOne> {
    return this.http.post<OneOnOne>(`${this.baseUrl}/${id}/follow-ups`, request);
  }

  resolveFollowUp(id: string, followUpId: string): Observable<OneOnOne> {
    return this.http.post<OneOnOne>(`${this.baseUrl}/${id}/follow-ups/${followUpId}/resolve`, {});
  }
}
