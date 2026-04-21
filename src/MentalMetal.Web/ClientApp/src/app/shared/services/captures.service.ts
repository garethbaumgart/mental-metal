import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Capture,
  CaptureTranscript,
  CaptureType,
  CreateCaptureRequest,
  ProcessingStatus,
  UpdateCaptureMetadataRequest,
  UpdateCaptureSpeakersRequest,
} from '../models/capture.model';
import { PersonType } from '../models/person.model';

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

  retry(id: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/retry`, {});
  }

  importFile(file: File, type?: CaptureType, title?: string, source?: string): Observable<{ id: string }> {
    const form = new FormData();
    form.append('file', file, file.name);
    if (type) form.append('type', type);
    if (title) form.append('title', title);
    if (source) form.append('sourceUrl', source);
    return this.http.post<{ id: string }>(`${this.baseUrl}/import`, form);
  }

  uploadAudio(blob: Blob, durationSeconds: number, title?: string): Observable<Capture> {
    const form = new FormData();
    const ext = blob.type.includes('webm')
      ? 'webm'
      : blob.type.includes('mp4')
      ? 'm4a'
      : blob.type.includes('wav')
      ? 'wav'
      : 'bin';
    form.append('file', blob, `recording.${ext}`);
    form.append('durationSeconds', durationSeconds.toString());
    if (title) form.append('title', title);
    return this.http.post<Capture>(`${this.baseUrl}/audio`, form);
  }

  retryTranscription(id: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/transcribe`, {});
  }

  getTranscript(id: string): Observable<CaptureTranscript> {
    return this.http.get<CaptureTranscript>(`${this.baseUrl}/${id}/transcript`);
  }

  updateSpeakers(id: string, request: UpdateCaptureSpeakersRequest): Observable<Capture> {
    return this.http.patch<Capture>(`${this.baseUrl}/${id}/speakers`, request);
  }

  resolvePersonMention(id: string, rawName: string, personId: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/resolve-person-mention`, { rawName, personId });
  }

  resolveInitiativeTag(id: string, rawName: string, initiativeId: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/resolve-initiative-tag`, { rawName, initiativeId });
  }

  quickCreateAndResolve(id: string, rawName: string, personName: string, personType: PersonType): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/resolve-person-mention/quick-create`, {
      rawName,
      personName,
      personType,
    });
  }
}
