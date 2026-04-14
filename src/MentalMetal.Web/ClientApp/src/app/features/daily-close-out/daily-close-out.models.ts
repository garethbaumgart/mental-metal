import { AiExtraction, CaptureType, ExtractionStatus, ProcessingStatus } from '../../shared/models/capture.model';

export interface CloseOutQueueItem {
  id: string;
  rawContent: string;
  captureType: CaptureType;
  processingStatus: ProcessingStatus;
  extractionStatus: ExtractionStatus;
  extractionResolved: boolean;
  aiExtraction: AiExtraction | null;
  failureReason: string | null;
  linkedPersonIds: string[];
  linkedInitiativeIds: string[];
  title: string | null;
  capturedAt: string;
  processedAt: string | null;
}

export interface CloseOutQueueCounts {
  total: number;
  raw: number;
  processing: number;
  processed: number;
  failed: number;
}

export interface CloseOutQueueResponse {
  items: CloseOutQueueItem[];
  counts: CloseOutQueueCounts;
}

export interface ReassignCaptureRequest {
  personIds: string[];
  initiativeIds: string[];
}

export interface CloseOutDayRequest {
  date?: string;
}

export interface DailyCloseOutLog {
  id: string;
  date: string;
  closedAtUtc: string;
  confirmedCount: number;
  discardedCount: number;
  remainingCount: number;
}
