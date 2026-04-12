export type CaptureType = 'QuickNote' | 'Transcript' | 'MeetingNotes';

export type ProcessingStatus = 'Raw' | 'Processing' | 'Processed' | 'Failed';

export interface Capture {
  id: string;
  userId: string;
  rawContent: string;
  captureType: CaptureType;
  processingStatus: ProcessingStatus;
  aiExtraction: string | null;
  linkedPersonIds: string[];
  linkedInitiativeIds: string[];
  spawnedCommitmentIds: string[];
  spawnedDelegationIds: string[];
  spawnedObservationIds: string[];
  title: string | null;
  capturedAt: string;
  processedAt: string | null;
  source: string | null;
  updatedAt: string;
}

export interface CreateCaptureRequest {
  rawContent: string;
  type: CaptureType;
  title?: string;
  source?: string;
}

export interface UpdateCaptureMetadataRequest {
  title: string | null;
  source: string | null;
}

export interface LinkPersonRequest {
  personId: string;
}

export interface LinkInitiativeRequest {
  initiativeId: string;
}
