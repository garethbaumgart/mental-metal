export type InitiativeStatus = 'Active' | 'OnHold' | 'Completed' | 'Cancelled';

export interface Initiative {
  id: string;
  userId: string;
  title: string;
  status: InitiativeStatus;
  autoSummary: string | null;
  lastSummaryRefreshedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateInitiativeRequest {
  title: string;
}

export interface UpdateTitleRequest {
  title: string;
}
