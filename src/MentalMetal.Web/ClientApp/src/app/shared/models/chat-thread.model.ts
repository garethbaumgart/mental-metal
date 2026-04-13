export type ChatRole = 'User' | 'Assistant' | 'System';
export type ChatThreadStatus = 'Active' | 'Archived';

export type SourceReferenceEntityType =
  | 'Capture'
  | 'Commitment'
  | 'Delegation'
  | 'LivingBriefDecision'
  | 'LivingBriefRisk'
  | 'LivingBriefRequirements'
  | 'LivingBriefDesignDirection'
  | 'Initiative';

export interface SourceReference {
  entityType: SourceReferenceEntityType;
  entityId: string;
  snippetText?: string | null;
  relevanceScore?: number | null;
}

export interface TokenUsage {
  promptTokens: number;
  completionTokens: number;
}

export interface ChatMessage {
  messageOrdinal: number;
  role: ChatRole;
  content: string;
  createdAt: string;
  sourceReferences: SourceReference[];
  tokenUsage?: TokenUsage | null;
}

export interface ChatThread {
  id: string;
  userId: string;
  contextScopeType: 'Initiative' | 'Global';
  contextInitiativeId?: string | null;
  title: string;
  status: ChatThreadStatus;
  createdAt: string;
  lastMessageAt?: string | null;
  messageCount: number;
  messages: ChatMessage[];
}

export interface ChatThreadSummary {
  id: string;
  title: string;
  status: ChatThreadStatus;
  createdAt: string;
  lastMessageAt?: string | null;
  messageCount: number;
}

export interface RenameChatThreadRequest {
  title: string;
}

export interface PostChatMessageRequest {
  content: string;
}

export interface PostChatMessageResponse {
  userMessage: ChatMessage;
  assistantMessage: ChatMessage;
}
