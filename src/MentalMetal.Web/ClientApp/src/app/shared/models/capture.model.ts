export type CaptureType = 'QuickNote' | 'Transcript' | 'MeetingNotes' | 'AudioRecording';

export type ProcessingStatus = 'Raw' | 'Processing' | 'Processed' | 'Failed';

export type TranscriptionStatus =
  | 'NotApplicable'
  | 'Pending'
  | 'InProgress'
  | 'Transcribed'
  | 'Failed';

export interface TranscriptSegment {
  startSeconds: number;
  endSeconds: number;
  speakerLabel: string;
  text: string;
  linkedPersonId: string | null;
}

export interface CaptureTranscript {
  captureId: string;
  transcriptionStatus: TranscriptionStatus;
  segments: TranscriptSegment[];
}

export interface SpeakerMapping {
  speakerLabel: string;
  personId: string;
}

export interface UpdateCaptureSpeakersRequest {
  mappings: SpeakerMapping[];
}

export type ExtractionStatus = 'None' | 'Pending' | 'Confirmed' | 'Discarded';

export interface ExtractedCommitment {
  description: string;
  direction: 'MineToThem' | 'TheirsToMe';
  personHint: string | null;
  dueDate: string | null;
}

export interface ExtractedDelegation {
  description: string;
  personHint: string | null;
  dueDate: string | null;
}

export interface ExtractedObservation {
  description: string;
  personHint: string | null;
  tag: string | null;
}

export interface AiExtraction {
  summary: string;
  commitments: ExtractedCommitment[];
  delegations: ExtractedDelegation[];
  observations: ExtractedObservation[];
  decisions: string[];
  risksIdentified: string[];
  suggestedPersonLinks: string[];
  suggestedInitiativeLinks: string[];
  confidenceScore: number;
}

export interface Capture {
  id: string;
  userId: string;
  rawContent: string;
  captureType: CaptureType;
  processingStatus: ProcessingStatus;
  extractionStatus: ExtractionStatus;
  aiExtraction: AiExtraction | null;
  failureReason: string | null;
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

export interface ConfirmExtractionResponse {
  capture: Capture;
  warnings: string[];
}
