import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Commitment,
  CommitmentDirection,
  CommitmentStatus,
  CompleteCommitmentRequest,
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

  complete(id: string, request?: CompleteCommitmentRequest): Observable<Commitment> {
    return this.http.post<Commitment>(`${this.baseUrl}/${id}/complete`, request ?? {});
  }

  dismiss(id: string): Observable<Commitment> {
    return this.http.post<Commitment>(`${this.baseUrl}/${id}/dismiss`, {});
  }

  reopen(id: string): Observable<Commitment> {
    return this.http.post<Commitment>(`${this.baseUrl}/${id}/reopen`, {});
  }
}
