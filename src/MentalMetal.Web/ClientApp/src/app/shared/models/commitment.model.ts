export type CommitmentDirection = 'MineToThem' | 'TheirsToMe';

export type CommitmentStatus = 'Open' | 'Completed' | 'Cancelled';

export interface Commitment {
  id: string;
  userId: string;
  description: string;
  direction: CommitmentDirection;
  personId: string;
  initiativeId: string | null;
  sourceCaptureId: string | null;
  dueDate: string | null;
  status: CommitmentStatus;
  completedAt: string | null;
  notes: string | null;
  isOverdue: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCommitmentRequest {
  description: string;
  direction: CommitmentDirection;
  personId: string;
  dueDate?: string;
  initiativeId?: string;
  sourceCaptureId?: string;
  notes?: string;
}

export interface UpdateCommitmentRequest {
  description: string;
  notes: string | null;
}

export interface CompleteCommitmentRequest {
  notes?: string;
}

export interface CancelCommitmentRequest {
  reason?: string;
}

export interface UpdateDueDateRequest {
  dueDate: string | null;
}

export interface LinkInitiativeRequest {
  initiativeId: string;
}
