import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Capture,
  CaptureTranscript,
  CaptureType,
  ConfirmExtractionResponse,
  CreateCaptureRequest,
  ProcessingStatus,
  UpdateCaptureMetadataRequest,
  UpdateCaptureSpeakersRequest,
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

  process(id: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/process`, {});
  }

  retry(id: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/retry`, {});
  }

  confirmExtraction(id: string): Observable<ConfirmExtractionResponse> {
    return this.http.post<ConfirmExtractionResponse>(`${this.baseUrl}/${id}/confirm-extraction`, {});
  }

  discardExtraction(id: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/discard-extraction`, {});
  }

  linkPerson(id: string, personId: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/link-person`, { personId });
  }

  unlinkPerson(id: string, personId: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/unlink-person`, { personId });
  }

  linkInitiative(id: string, initiativeId: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/link-initiative`, { initiativeId });
  }

  unlinkInitiative(id: string, initiativeId: string): Observable<Capture> {
    return this.http.post<Capture>(`${this.baseUrl}/${id}/unlink-initiative`, { initiativeId });
  }

  uploadAudio(blob: Blob, durationSeconds: number, title?: string, source?: string): Observable<Capture> {
    const form = new FormData();
    // Preserve MIME on the file part — the backend reads `file.ContentType`.
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
    if (source) form.append('source', source);
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
}
