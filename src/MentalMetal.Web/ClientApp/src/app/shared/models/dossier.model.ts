import { CommitmentConfidence, CommitmentDirection } from './commitment.model';

export interface PersonDossier {
  personId: string;
  personName: string;
  synthesis: string;
  openCommitments: DossierCommitment[];
  transcriptMentions: TranscriptMention[];
  unresolvedMentions: UnresolvedMention[];
  generatedAt: string;
}

export interface DossierCommitment {
  id: string;
  description: string;
  direction: CommitmentDirection;
  dueDate: string | null;
  isOverdue: boolean;
  confidence: CommitmentConfidence;
}

export interface TranscriptMention {
  captureId: string;
  captureTitle: string | null;
  capturedAt: string;
  extractionSummary: string | null;
  mentionContext: string | null;
}

export interface UnresolvedMention {
  captureId: string;
  rawName: string;
  context: string | null;
}
