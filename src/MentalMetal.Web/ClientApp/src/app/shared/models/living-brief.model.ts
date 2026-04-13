export type BriefSource = 'Manual' | 'AI';
export type RiskSeverity = 'Low' | 'Medium' | 'High' | 'Critical';
export type RiskStatus = 'Open' | 'Resolved';
export type PendingBriefUpdateStatus = 'Pending' | 'Edited' | 'Applied' | 'Rejected' | 'Failed';

export interface KeyDecision {
  id: string;
  description: string;
  rationale?: string | null;
  source: BriefSource;
  sourceCaptureIds: string[];
  loggedAt: string;
}

export interface Risk {
  id: string;
  description: string;
  severity: RiskSeverity;
  status: RiskStatus;
  source: BriefSource;
  sourceCaptureIds: string[];
  raisedAt: string;
  resolvedAt?: string | null;
  resolutionNote?: string | null;
}

export interface BriefSnapshot {
  id: string;
  content: string;
  source: BriefSource;
  sourceCaptureIds: string[];
  capturedAt: string;
}

export interface LivingBrief {
  initiativeId: string;
  summary: string;
  summaryLastRefreshedAt?: string | null;
  briefVersion: number;
  summarySource: BriefSource;
  summarySourceCaptureIds: string[];
  keyDecisions: KeyDecision[];
  risks: Risk[];
  requirementsHistory: BriefSnapshot[];
  designDirectionHistory: BriefSnapshot[];
}

export interface ProposedDecision {
  description: string;
  rationale?: string | null;
  sourceCaptureIds: string[];
}

export interface ProposedRisk {
  description: string;
  severity: RiskSeverity;
  sourceCaptureIds: string[];
}

export interface BriefUpdateProposal {
  proposedSummary?: string | null;
  newDecisions: ProposedDecision[];
  newRisks: ProposedRisk[];
  risksToResolve: string[];
  proposedRequirementsContent?: string | null;
  proposedDesignDirectionContent?: string | null;
  sourceCaptureIds: string[];
  aiConfidence?: number | null;
  rationale?: string | null;
}

export interface PendingBriefUpdate {
  id: string;
  initiativeId: string;
  status: PendingBriefUpdateStatus;
  briefVersionAtProposal: number;
  currentInitiativeBriefVersion: number;
  isStale: boolean;
  failureReason?: string | null;
  proposal: BriefUpdateProposal;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateSummaryRequest { summary: string; }
export interface LogDecisionRequest { description: string; rationale?: string | null; }
export interface RaiseRiskRequest { description: string; severity: RiskSeverity; }
export interface ResolveRiskRequest { resolutionNote?: string | null; }
export interface SnapshotRequest { content: string; }
export interface RejectPendingUpdateRequest { reason?: string | null; }

// Mirrors the server-side EditPendingUpdateRequest DTO. Each field is optional —
// omitting a field on the request leaves the corresponding part of the proposal unchanged.
// Severity is sent as the enum string (matching ProposedRiskDto on the server).
export interface EditPendingUpdateRequest {
  proposedSummary?: string | null;
  newDecisions?: ProposedDecision[] | null;
  newRisks?: { description: string; severity: RiskSeverity; sourceCaptureIds: string[] }[] | null;
  risksToResolve?: string[] | null;
  proposedRequirementsContent?: string | null;
  proposedDesignDirectionContent?: string | null;
  rationale?: string | null;
}
