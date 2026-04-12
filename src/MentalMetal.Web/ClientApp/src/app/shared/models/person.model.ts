export type PersonType = 'DirectReport' | 'Stakeholder' | 'Candidate';
export type PipelineStatus = 'New' | 'Screening' | 'Interviewing' | 'OfferStage' | 'Hired' | 'Rejected' | 'Withdrawn';

export interface CareerDetails {
  level: string | null;
  aspirations: string | null;
  growthAreas: string | null;
}

export interface CandidateDetails {
  pipelineStatus: PipelineStatus;
  cvNotes: string | null;
  sourceChannel: string | null;
}

export interface Person {
  id: string;
  userId: string;
  name: string;
  type: PersonType;
  email: string | null;
  role: string | null;
  team: string | null;
  notes: string | null;
  careerDetails: CareerDetails | null;
  candidateDetails: CandidateDetails | null;
  isArchived: boolean;
  archivedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePersonRequest {
  name: string;
  type: PersonType;
  email?: string;
  role?: string;
  team?: string;
}

export interface UpdatePersonRequest {
  name: string;
  email: string | null;
  role: string | null;
  team: string | null;
  notes: string | null;
}

export interface UpdateCareerDetailsRequest {
  level: string | null;
  aspirations: string | null;
  growthAreas: string | null;
}

export interface UpdateCandidateDetailsRequest {
  cvNotes: string | null;
  sourceChannel: string | null;
}
