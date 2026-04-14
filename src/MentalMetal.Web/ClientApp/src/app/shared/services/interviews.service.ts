import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AdvanceStageRequest,
  CreateInterviewRequest,
  Interview,
  InterviewAnalysisResponse,
  InterviewScorecard,
  InterviewStage,
  RecordDecisionRequest,
  SetTranscriptRequest,
  UpdateInterviewRequest,
  UpsertScorecardRequest,
} from '../models/interview.model';

@Injectable({ providedIn: 'root' })
export class InterviewsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/interviews';

  list(candidatePersonId?: string, stage?: InterviewStage): Observable<Interview[]> {
    let params = new HttpParams();
    if (candidatePersonId) params = params.set('candidatePersonId', candidatePersonId);
    if (stage) params = params.set('stage', stage);
    return this.http.get<Interview[]>(this.baseUrl, { params });
  }

  get(id: string): Observable<Interview> {
    return this.http.get<Interview>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateInterviewRequest): Observable<Interview> {
    return this.http.post<Interview>(this.baseUrl, request);
  }

  update(id: string, request: UpdateInterviewRequest): Observable<Interview> {
    return this.http.patch<Interview>(`${this.baseUrl}/${id}`, request);
  }

  advance(id: string, request: AdvanceStageRequest): Observable<Interview> {
    return this.http.post<Interview>(`${this.baseUrl}/${id}/advance`, request);
  }

  recordDecision(id: string, request: RecordDecisionRequest): Observable<Interview> {
    return this.http.post<Interview>(`${this.baseUrl}/${id}/decision`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  addScorecard(id: string, request: UpsertScorecardRequest): Observable<InterviewScorecard> {
    return this.http.post<InterviewScorecard>(`${this.baseUrl}/${id}/scorecards`, request);
  }

  updateScorecard(id: string, scorecardId: string, request: UpsertScorecardRequest): Observable<InterviewScorecard> {
    return this.http.put<InterviewScorecard>(`${this.baseUrl}/${id}/scorecards/${scorecardId}`, request);
  }

  removeScorecard(id: string, scorecardId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/scorecards/${scorecardId}`);
  }

  setTranscript(id: string, request: SetTranscriptRequest): Observable<Interview> {
    return this.http.put<Interview>(`${this.baseUrl}/${id}/transcript`, request);
  }

  analyze(id: string): Observable<InterviewAnalysisResponse> {
    return this.http.post<InterviewAnalysisResponse>(`${this.baseUrl}/${id}/analyze`, {});
  }
}
