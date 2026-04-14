export type CadenceType = 'Daily' | 'Weekly' | 'Biweekly' | 'Monthly' | 'Custom';

export type NudgeDayOfWeek =
  | 'Sunday'
  | 'Monday'
  | 'Tuesday'
  | 'Wednesday'
  | 'Thursday'
  | 'Friday'
  | 'Saturday';

export interface NudgeCadence {
  type: CadenceType;
  customIntervalDays: number | null;
  dayOfWeek: NudgeDayOfWeek | null;
  dayOfMonth: number | null;
}

export interface Nudge {
  id: string;
  userId: string;
  title: string;
  cadence: NudgeCadence;
  startDate: string;
  nextDueDate: string | null;
  lastNudgedAt: string | null;
  personId: string | null;
  initiativeId: string | null;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateNudgeRequest {
  title: string;
  cadenceType: CadenceType;
  customIntervalDays?: number | null;
  dayOfWeek?: NudgeDayOfWeek | null;
  dayOfMonth?: number | null;
  startDate?: string | null;
  personId?: string | null;
  initiativeId?: string | null;
  notes?: string | null;
}

export interface UpdateNudgeRequest {
  title: string;
  notes: string | null;
  personId: string | null;
  initiativeId: string | null;
}

export interface UpdateCadenceRequest {
  cadenceType: CadenceType;
  customIntervalDays?: number | null;
  dayOfWeek?: NudgeDayOfWeek | null;
  dayOfMonth?: number | null;
}

export interface ListNudgesFilters {
  isActive?: boolean | null;
  personId?: string | null;
  initiativeId?: string | null;
  dueBefore?: string | null;
  dueWithinDays?: number | null;
}
