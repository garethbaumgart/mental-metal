export type ObservationTag = 'Win' | 'Growth' | 'Concern' | 'FeedbackGiven';

export interface Observation {
  id: string;
  userId: string;
  personId: string;
  description: string;
  tag: ObservationTag;
  occurredAt: string;
  sourceCaptureId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateObservationRequest {
  personId: string;
  description: string;
  tag: ObservationTag;
  occurredAt?: string | null;
  sourceCaptureId?: string | null;
}

export interface UpdateObservationRequest {
  description: string;
  tag: ObservationTag;
}
