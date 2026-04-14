export type QueueItemType = 'Commitment' | 'Delegation' | 'Capture';
export type QueueScope = 'All' | 'Overdue' | 'Today' | 'ThisWeek';

export interface QueueItem {
  itemType: QueueItemType;
  id: string;
  title: string;
  status: string;
  dueDate: string | null;
  isOverdue: boolean;
  personId: string | null;
  personName: string | null;
  initiativeId: string | null;
  initiativeName: string | null;
  daysSinceCaptured: number | null;
  lastFollowedUpAt: string | null;
  priorityScore: number;
  suggestDelegate: boolean;
}

export interface QueueCounts {
  overdue: number;
  dueSoon: number;
  staleCaptures: number;
  staleDelegations: number;
  total: number;
}

export interface QueueFilters {
  scope: QueueScope;
  itemType: QueueItemType[];
  personId: string | null;
  initiativeId: string | null;
}

export interface MyQueueResponse {
  items: QueueItem[];
  counts: QueueCounts;
  filters: QueueFilters;
}

export interface MyQueueFilterParams {
  scope?: QueueScope;
  itemType?: QueueItemType[];
  personId?: string;
  initiativeId?: string;
}
