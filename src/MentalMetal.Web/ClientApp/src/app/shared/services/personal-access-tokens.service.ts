import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PatSummary {
  id: string;
  name: string;
  scopes: string[];
  createdAt: string;
  lastUsedAt: string | null;
  revokedAt: string | null;
}

export interface PatCreated {
  id: string;
  name: string;
  scopes: string[];
  createdAt: string;
  token: string;
}

export interface CreatePatRequest {
  name: string;
  scopes: string[];
}

@Injectable({ providedIn: 'root' })
export class PersonalAccessTokensService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/personal-access-tokens';

  list(): Observable<PatSummary[]> {
    return this.http.get<PatSummary[]>(this.baseUrl);
  }

  create(request: CreatePatRequest): Observable<PatCreated> {
    return this.http.post<PatCreated>(this.baseUrl, request);
  }

  revoke(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/revoke`, {});
  }
}
