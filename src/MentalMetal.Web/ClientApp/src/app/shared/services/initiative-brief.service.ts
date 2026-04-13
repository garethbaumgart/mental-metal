import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  EditPendingUpdateRequest,
  LivingBrief,
  LogDecisionRequest,
  PendingBriefUpdate,
  PendingBriefUpdateStatus,
  RaiseRiskRequest,
  RejectPendingUpdateRequest,
  ResolveRiskRequest,
  SnapshotRequest,
  UpdateSummaryRequest,
} from '../models/living-brief.model';

@Injectable({ providedIn: 'root' })
export class InitiativeBriefService {
  private readonly http = inject(HttpClient);

  private base(initiativeId: string) {
    return `/api/initiatives/${initiativeId}/brief`;
  }

  get(initiativeId: string): Observable<LivingBrief> {
    return this.http.get<LivingBrief>(this.base(initiativeId));
  }

  updateSummary(initiativeId: string, request: UpdateSummaryRequest): Observable<LivingBrief> {
    return this.http.put<LivingBrief>(`${this.base(initiativeId)}/summary`, request);
  }

  logDecision(initiativeId: string, request: LogDecisionRequest): Observable<LivingBrief> {
    return this.http.post<LivingBrief>(`${this.base(initiativeId)}/decisions`, request);
  }

  raiseRisk(initiativeId: string, request: RaiseRiskRequest): Observable<LivingBrief> {
    return this.http.post<LivingBrief>(`${this.base(initiativeId)}/risks`, request);
  }

  resolveRisk(initiativeId: string, riskId: string, request: ResolveRiskRequest): Observable<LivingBrief> {
    return this.http.post<LivingBrief>(`${this.base(initiativeId)}/risks/${riskId}/resolve`, request);
  }

  snapshotRequirements(initiativeId: string, request: SnapshotRequest): Observable<LivingBrief> {
    return this.http.post<LivingBrief>(`${this.base(initiativeId)}/requirements`, request);
  }

  snapshotDesignDirection(initiativeId: string, request: SnapshotRequest): Observable<LivingBrief> {
    return this.http.post<LivingBrief>(`${this.base(initiativeId)}/design-direction`, request);
  }

  refresh(initiativeId: string): Observable<{ pendingUpdateId: string }> {
    return this.http.post<{ pendingUpdateId: string }>(`${this.base(initiativeId)}/refresh`, {});
  }

  listPending(initiativeId: string, status?: PendingBriefUpdateStatus): Observable<PendingBriefUpdate[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<PendingBriefUpdate[]>(`${this.base(initiativeId)}/pending-updates`, { params });
  }

  getPending(initiativeId: string, updateId: string): Observable<PendingBriefUpdate> {
    return this.http.get<PendingBriefUpdate>(`${this.base(initiativeId)}/pending-updates/${updateId}`);
  }

  applyPending(initiativeId: string, updateId: string): Observable<LivingBrief> {
    return this.http.post<LivingBrief>(`${this.base(initiativeId)}/pending-updates/${updateId}/apply`, {});
  }

  rejectPending(initiativeId: string, updateId: string, request: RejectPendingUpdateRequest): Observable<void> {
    return this.http.post<void>(`${this.base(initiativeId)}/pending-updates/${updateId}/reject`, request);
  }

  editPending(initiativeId: string, updateId: string, body: EditPendingUpdateRequest): Observable<PendingBriefUpdate> {
    return this.http.put<PendingBriefUpdate>(`${this.base(initiativeId)}/pending-updates/${updateId}`, body);
  }
}
