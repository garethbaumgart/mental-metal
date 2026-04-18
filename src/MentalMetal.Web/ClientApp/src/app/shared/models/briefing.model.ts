import { CommitmentConfidence, CommitmentDirection } from './commitment.model';

export interface DailyBrief {
  narrative: string;
  freshCommitments: BriefCommitment[];
  dueToday: BriefCommitment[];
  overdue: BriefCommitment[];
  peopleActivity: PersonActivity[];
  captureCount: number;
  generatedAt: string;
}

export interface BriefCommitment {
  id: string;
  description: string;
  direction: CommitmentDirection;
  personId: string;
  personName: string | null;
  dueDate: string | null;
  isOverdue: boolean;
  confidence: CommitmentConfidence;
}

export interface PersonActivity {
  personId: string;
  personName: string;
  mentionCount: number;
}

export interface WeeklyBrief {
  narrative: string;
  crossConversationInsights: string[];
  decisions: string[];
  commitmentStatus: CommitmentStatusSummary;
  risks: string[];
  initiativeActivity: InitiativeActivity[];
  dateRange: DateRange;
  generatedAt: string;
}

export interface CommitmentStatusSummary {
  newCount: number;
  completedCount: number;
  overdueCount: number;
  totalOpen: number;
}

export interface InitiativeActivity {
  initiativeId: string;
  title: string;
  captureCount: number;
  autoSummary: string | null;
}

export interface DateRange {
  start: string;
  end: string;
}
