export type InitiativeStatus = 'Active' | 'OnHold' | 'Completed' | 'Cancelled';

export interface Milestone {
  id: string;
  title: string;
  targetDate: string;
  description: string | null;
  isCompleted: boolean;
}

export interface Initiative {
  id: string;
  userId: string;
  title: string;
  status: InitiativeStatus;
  milestones: Milestone[];
  linkedPersonIds: string[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateInitiativeRequest {
  title: string;
}

export interface UpdateTitleRequest {
  title: string;
}

export interface MilestoneRequest {
  title: string;
  targetDate: string;
  description?: string;
}

export interface LinkPersonRequest {
  personId: string;
}
