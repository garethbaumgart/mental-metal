import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Capture,
  CaptureType,
  CreateCaptureRequest,
  LinkInitiativeRequest,
  LinkPersonRequest,
  ProcessingStatus,
  UpdateCaptureMetadataRequest,
} from '../models/capture.model';

@Injectable({ providedIn: 'root' })
export class CapturesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/captures';

  list(type?: CaptureType, status?: ProcessingStatus): Observable<Capture[]> {
    let params = new HttpParams();
    if (type) {
      params = params.set('type', type);
    }
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<Capture[]>(this.baseUrl, { params });
  }

  get(id: string): Observable<Capture> {
    return this.http.get<Capture>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateCaptureRequest): Observable<Capture> {
    return this.http.post<Capture>(this.baseUrl, request);
  }

  updateMetadata(id: string, request: UpdateCaptureMetadataRequest): Observable<Capture> {
    return this.http.put<Capture>(`${this.baseUrl}/${id}`, request);
  }

  linkPerson(id: string, personId: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/link-person`, { personId } as LinkPersonRequest);
  }

  unlinkPerson(id: string, personId: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/unlink-person`, { personId } as LinkPersonRequest);
  }

  linkInitiative(id: string, initiativeId: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/link-initiative`, { initiativeId } as LinkInitiativeRequest);
  }

  unlinkInitiative(id: string, initiativeId: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/unlink-initiative`, { initiativeId } as LinkInitiativeRequest);
  }
}
