export type DelegationStatus = 'Assigned' | 'InProgress' | 'Completed' | 'Blocked';

export type DelegationPriority = 'Low' | 'Medium' | 'High' | 'Urgent';

export interface Delegation {
  id: string;
  userId: string;
  description: string;
  delegatePersonId: string;
  initiativeId: string | null;
  sourceCaptureId: string | null;
  dueDate: string | null;
  status: DelegationStatus;
  priority: DelegationPriority;
  completedAt: string | null;
  notes: string | null;
  lastFollowedUpAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateDelegationRequest {
  description: string;
  delegatePersonId: string;
  dueDate?: string;
  initiativeId?: string;
  priority?: DelegationPriority;
  sourceCaptureId?: string;
  notes?: string;
}

export interface UpdateDelegationRequest {
  description: string;
  notes: string | null;
}

export interface CompleteDelegationRequest {
  notes?: string;
}

export interface BlockDelegationRequest {
  reason: string;
}

export interface FollowUpDelegationRequest {
  notes?: string;
}

export interface UpdateDelegationDueDateRequest {
  dueDate: string | null;
}

export interface ReprioritizeDelegationRequest {
  priority: DelegationPriority;
}

export interface ReassignDelegationRequest {
  delegatePersonId: string;
}
