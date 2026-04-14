export type InterviewStage =
  | 'Applied'
  | 'ScreenScheduled'
  | 'ScreenCompleted'
  | 'OnsiteScheduled'
  | 'OnsiteCompleted'
  | 'OfferExtended'
  | 'Hired'
  | 'Rejected'
  | 'Withdrawn';

export const INTERVIEW_STAGES: InterviewStage[] = [
  'Applied',
  'ScreenScheduled',
  'ScreenCompleted',
  'OnsiteScheduled',
  'OnsiteCompleted',
  'OfferExtended',
  'Hired',
  'Rejected',
  'Withdrawn',
];

export type InterviewDecision =
  | 'StrongHire'
  | 'Hire'
  | 'LeanHire'
  | 'NoHire'
  | 'StrongNoHire';

export const INTERVIEW_DECISIONS: InterviewDecision[] = [
  'StrongHire',
  'Hire',
  'LeanHire',
  'NoHire',
  'StrongNoHire',
];

export interface InterviewScorecard {
  id: string;
  competency: string;
  rating: number;
  notes: string | null;
  recordedAtUtc: string;
}

export interface InterviewTranscript {
  rawText: string;
  summary: string | null;
  recommendedDecision: InterviewDecision | null;
  riskSignals: string[];
  analyzedAtUtc: string | null;
  model: string | null;
}

export interface Interview {
  id: string;
  userId: string;
  candidatePersonId: string;
  roleTitle: string;
  stage: InterviewStage;
  scheduledAtUtc: string | null;
  completedAtUtc: string | null;
  decision: InterviewDecision | null;
  transcript: InterviewTranscript | null;
  scorecards: InterviewScorecard[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateInterviewRequest {
  candidatePersonId: string;
  roleTitle: string;
  scheduledAtUtc?: string | null;
}

export interface UpdateInterviewRequest {
  roleTitle?: string | null;
  scheduledAtUtc?: string | null;
  clearScheduledAt?: boolean;
}

export interface AdvanceStageRequest {
  targetStage: InterviewStage;
}

export interface RecordDecisionRequest {
  decision: InterviewDecision;
}

export interface UpsertScorecardRequest {
  competency: string;
  rating: number;
  notes?: string | null;
}

export interface SetTranscriptRequest {
  rawText: string;
}

export interface InterviewAnalysisResponse {
  summary: string;
  recommendedDecision: InterviewDecision | null;
  riskSignals: string[];
  model: string;
  analyzedAtUtc: string;
  warning: string | null;
}
