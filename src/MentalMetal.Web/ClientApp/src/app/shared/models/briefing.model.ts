export type BriefingType = 'Morning' | 'Weekly' | 'OneOnOnePrep';

export interface Briefing {
  id: string;
  type: BriefingType;
  scopeKey: string;
  generatedAtUtc: string;
  markdownBody: string;
  model: string;
  inputTokens: number;
  outputTokens: number;
  factsSummary: unknown;
}

export interface BriefingSummary {
  id: string;
  type: BriefingType;
  scopeKey: string;
  generatedAtUtc: string;
  model: string;
  inputTokens: number;
  outputTokens: number;
}
