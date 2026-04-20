export type CommitmentDirection = 'MineToThem' | 'TheirsToMe';

export type CommitmentStatus = 'Open' | 'Completed' | 'Cancelled' | 'Dismissed';

export type CommitmentConfidence = 'High' | 'Medium' | 'Low';

export interface Commitment {
  id: string;
  userId: string;
  description: string;
  direction: CommitmentDirection;
  personId: string;
  initiativeId: string | null;
  sourceCaptureId: string | null;
  sourceStartOffset: number | null;
  sourceEndOffset: number | null;
  confidence: CommitmentConfidence;
  dueDate: string | null;
  status: CommitmentStatus;
  completedAt: string | null;
  dismissedAt: string | null;
  notes: string | null;
  isOverdue: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CompleteCommitmentRequest {
  notes?: string;
}

export interface UpdateCommitmentRequest {
  description?: string;
  direction?: CommitmentDirection;
  dueDate?: string;
  clearDueDate?: boolean;
  notes?: string;
  clearNotes?: boolean;
}
