export type GoalType = 'Development' | 'Performance';
export type GoalStatus = 'Active' | 'Achieved' | 'Missed' | 'Deferred';

export interface GoalCheckIn {
  id: string;
  note: string;
  progress: number | null;
  recordedAt: string;
}

export interface Goal {
  id: string;
  userId: string;
  personId: string;
  title: string;
  description: string | null;
  goalType: GoalType;
  status: GoalStatus;
  targetDate: string | null;
  deferralReason: string | null;
  achievedAt: string | null;
  checkIns: GoalCheckIn[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateGoalRequest {
  personId: string;
  title: string;
  goalType: GoalType;
  description?: string | null;
  targetDate?: string | null;
}

export interface UpdateGoalRequest {
  title: string;
  description: string | null;
  targetDate: string | null;
}

export interface DeferGoalRequest {
  reason?: string | null;
}

export interface RecordCheckInRequest {
  note: string;
  progress?: number | null;
}

export interface PersonEvidenceSummary {
  personId: string;
  from: string;
  to: string;
  observationsWin: number;
  observationsGrowth: number;
  observationsConcern: number;
  observationsFeedbackGiven: number;
  goalsAchieved: number;
  goalsMissed: number;
  goalsActive: number;
  goalsDeferred: number;
  commitmentsCompletedOnTime: number;
  commitmentsCompletedLate: number;
  commitmentsOpen: number;
  delegationsCompleted: number;
  delegationsInProgress: number;
  hasAny: boolean;
}
