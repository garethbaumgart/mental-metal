export type CaptureType = 'QuickNote' | 'Transcript' | 'MeetingNotes' | 'AudioRecording';

export type CaptureSource = 'Upload' | 'Bookmarklet' | 'AudioCapture' | 'Typed' | 'Voice';

export type ProcessingStatus = 'Raw' | 'Processing' | 'Processed' | 'Failed';

export type TranscriptionStatus =
  | 'NotApplicable'
  | 'Pending'
  | 'InProgress'
  | 'Transcribed'
  | 'Failed';

export type CommitmentConfidence = 'High' | 'Medium' | 'Low';

export type CommitmentDirection = 'MineToThem' | 'TheirsToMe';

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

export interface PersonMention {
  rawName: string;
  personId: string | null;
  context: string | null;
}

export interface ExtractedCommitment {
  description: string;
  direction: CommitmentDirection;
  personRawName: string | null;
  personId: string | null;
  dueDate: string | null;
  confidence: CommitmentConfidence;
  spawnedCommitmentId: string | null;
}

export interface InitiativeTag {
  rawName: string;
  initiativeId: string | null;
  context: string | null;
}

export interface AiExtraction {
  summary: string;
  peopleMentioned: PersonMention[];
  commitments: ExtractedCommitment[];
  decisions: string[];
  risks: string[];
  initiativeTags: InitiativeTag[];
  extractedAt: string;
  detectedCaptureType: CaptureType | null;
}

export interface Capture {
  id: string;
  userId: string;
  rawContent: string;
  captureType: CaptureType;
  captureSource: CaptureSource | null;
  processingStatus: ProcessingStatus;
  aiExtraction: AiExtraction | null;
  failureReason: string | null;
  linkedPersonIds: string[];
  linkedInitiativeIds: string[];
  spawnedCommitmentIds: string[];
  title: string | null;
  capturedAt: string;
  processedAt: string | null;
  updatedAt: string;
}

export interface CreateCaptureRequest {
  rawContent: string;
  type: CaptureType;
  source?: CaptureSource;
  title?: string;
}

export interface UpdateCaptureMetadataRequest {
  title: string | null;
}
