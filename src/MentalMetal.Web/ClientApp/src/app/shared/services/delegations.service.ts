import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  BlockDelegationRequest,
  CompleteDelegationRequest,
  CreateDelegationRequest,
  Delegation,
  DelegationPriority,
  DelegationStatus,
  FollowUpDelegationRequest,
  ReassignDelegationRequest,
  ReprioritizeDelegationRequest,
  UpdateDelegationDueDateRequest,
  UpdateDelegationRequest,
} from '../models/delegation.model';

@Injectable({ providedIn: 'root' })
export class DelegationsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/delegations';

  list(
    status?: DelegationStatus,
    priority?: DelegationPriority,
    delegatePersonId?: string,
    initiativeId?: string,
  ): Observable<Delegation[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    if (priority) {
      params = params.set('priority', priority);
    }
    if (delegatePersonId) {
      params = params.set('delegatePersonId', delegatePersonId);
    }
    if (initiativeId) {
      params = params.set('initiativeId', initiativeId);
    }
    return this.http.get<Delegation[]>(this.baseUrl, { params });
  }

  get(id: string): Observable<Delegation> {
    return this.http.get<Delegation>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateDelegationRequest): Observable<Delegation> {
    return this.http.post<Delegation>(this.baseUrl, request);
  }

  update(id: string, request: UpdateDelegationRequest): Observable<Delegation> {
    return this.http.put<Delegation>(`${this.baseUrl}/${id}`, request);
  }

  start(id: string): Observable<Delegation> {
    return this.http.post<Delegation>(`${this.baseUrl}/${id}/start`, {});
  }

  complete(id: string, request?: CompleteDelegationRequest): Observable<Delegation> {
    return this.http.post<Delegation>(`${this.baseUrl}/${id}/complete`, request ?? {});
  }

  block(id: string, request: BlockDelegationRequest): Observable<Delegation> {
    return this.http.post<Delegation>(`${this.baseUrl}/${id}/block`, request);
  }

  unblock(id: string): Observable<Delegation> {
    return this.http.post<Delegation>(`${this.baseUrl}/${id}/unblock`, {});
  }

  followUp(id: string, request?: FollowUpDelegationRequest): Observable<Delegation> {
    return this.http.post<Delegation>(`${this.baseUrl}/${id}/follow-up`, request ?? {});
  }

  updateDueDate(id: string, request: UpdateDelegationDueDateRequest): Observable<Delegation> {
    return this.http.put<Delegation>(`${this.baseUrl}/${id}/due-date`, request);
  }

  reprioritize(id: string, request: ReprioritizeDelegationRequest): Observable<Delegation> {
    return this.http.put<Delegation>(`${this.baseUrl}/${id}/priority`, request);
  }

  reassign(id: string, request: ReassignDelegationRequest): Observable<Delegation> {
    return this.http.post<Delegation>(`${this.baseUrl}/${id}/reassign`, request);
  }
}
