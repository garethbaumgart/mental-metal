import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CancelCommitmentRequest,
  Commitment,
  CommitmentDirection,
  CommitmentStatus,
  CompleteCommitmentRequest,
  CreateCommitmentRequest,
  UpdateCommitmentRequest,
  UpdateDueDateRequest,
} from '../models/commitment.model';

@Injectable({ providedIn: 'root' })
export class CommitmentsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/commitments';

  list(
    direction?: CommitmentDirection,
    status?: CommitmentStatus,
    personId?: string,
    initiativeId?: string,
    overdue?: boolean,
  ): Observable<Commitment[]> {
    let params = new HttpParams();
    if (direction) {
      params = params.set('direction', direction);
    }
    if (status) {
      params = params.set('status', status);
    }
    if (personId) {
      params = params.set('personId', personId);
    }
    if (initiativeId) {
      params = params.set('initiativeId', initiativeId);
    }
    if (overdue !== undefined) {
      params = params.set('overdue', overdue.toString());
    }
    return this.http.get<Commitment[]>(this.baseUrl, { params });
  }

  get(id: string): Observable<Commitment> {
    return this.http.get<Commitment>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateCommitmentRequest): Observable<Commitment> {
    return this.http.post<Commitment>(this.baseUrl, request);
  }

  update(id: string, request: UpdateCommitmentRequest): Observable<Commitment> {
    return this.http.put<Commitment>(`${this.baseUrl}/${id}`, request);
  }

  complete(id: string, request?: CompleteCommitmentRequest): Observable<Commitment> {
    return this.http.post<Commitment>(`${this.baseUrl}/${id}/complete`, request ?? {});
  }

  cancel(id: string, request?: CancelCommitmentRequest): Observable<Commitment> {
    return this.http.post<Commitment>(`${this.baseUrl}/${id}/cancel`, request ?? {});
  }

  reopen(id: string): Observable<Commitment> {
    return this.http.post<Commitment>(`${this.baseUrl}/${id}/reopen`, {});
  }

  updateDueDate(id: string, request: UpdateDueDateRequest): Observable<Commitment> {
    return this.http.put<Commitment>(`${this.baseUrl}/${id}/due-date`, request);
  }

  linkInitiative(id: string, initiativeId: string): Observable<Commitment> {
    return this.http.post<Commitment>(`${this.baseUrl}/${id}/link-initiative`, { initiativeId });
  }
}
