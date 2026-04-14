export interface ActionItem {
  id: string;
  description: string;
  completed: boolean;
}

export interface FollowUp {
  id: string;
  description: string;
  resolved: boolean;
}

export interface OneOnOne {
  id: string;
  userId: string;
  personId: string;
  occurredAt: string;
  notes: string | null;
  moodRating: number | null;
  topics: string[];
  actionItems: ActionItem[];
  followUps: FollowUp[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateOneOnOneRequest {
  personId: string;
  occurredAt: string;
  notes?: string | null;
  topics?: string[];
  moodRating?: number | null;
}

export interface UpdateOneOnOneRequest {
  notes: string | null;
  topics: string[] | null;
  moodRating: number | null;
}

export interface AddActionItemRequest {
  description: string;
}

export interface AddFollowUpRequest {
  description: string;
}
