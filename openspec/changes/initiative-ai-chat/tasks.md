## 1. Domain Layer — ChatThread aggregate

- [ ] 1.1 Create `ChatRole` enum (User, Assistant, System) and `ChatThreadStatus` enum (Active, Archived) in `src/MentalMetal.Domain/ChatThreads/`
- [ ] 1.2 Create `ContextScope` value object with `Initiative(InitiativeId)` factory; reserve `Global()` for global-ai-chat (not implemented in this spec)
- [ ] 1.3 Create `SourceReferenceEntityType` enum (Capture, Commitment, Delegation, LivingBriefDecision, LivingBriefRisk, LivingBriefRequirements, LivingBriefDesignDirection, Initiative)
- [ ] 1.4 Create `SourceReference` value object (EntityType, EntityId, SnippetText?, RelevanceScore?) with validation
- [ ] 1.5 Create `TokenUsage` value object (PromptTokens, CompletionTokens)
- [ ] 1.6 Create `ChatMessage` value object (MessageOrdinal, Role, Content, CreatedAt, SourceReferences, TokenUsage?) with validation that SourceReferences is empty unless Role is Assistant
- [ ] 1.7 Create `ChatThread` aggregate root with Id, UserId, ContextScope, Title, Status, CreatedAt, LastMessageAt, MessageCount, Messages
- [ ] 1.8 Implement factory `Start(userId, contextScope)`; raise `ChatThreadStarted`
- [ ] 1.9 Implement `AppendUserMessage(content)` returning the new message; raise `ChatMessageSent`; auto-set Title from first user message; update LastMessageAt and MessageCount
- [ ] 1.10 Implement `AppendAssistantMessage(content, sourceReferences, tokenUsage?)`; raise `ChatMessageReceived`
- [ ] 1.11 Implement `AppendSystemMessage(content)` for error / limit notices
- [ ] 1.12 Implement `Rename(title, source)` with validation; raise `ChatThreadRenamed`
- [ ] 1.13 Implement `Archive()` and `Unarchive()` with status guards; raise `ChatThreadArchived` / unarchive equivalent
- [ ] 1.14 Create `IChatThreadRepository` interface
- [ ] 1.15 Create domain events: `ChatThreadStarted`, `ChatMessageSent`, `ChatMessageReceived`, `ChatThreadRenamed`, `ChatThreadArchived`

## 2. Domain Unit Tests

- [ ] 2.1 Test ChatThread.Start invariants
- [ ] 2.2 Test message ordinal contiguity and updates to MessageCount/LastMessageAt
- [ ] 2.3 Test SourceReferences rejected on User/System messages
- [ ] 2.4 Test auto-title from first user message (truncated at 80 chars with ellipsis)
- [ ] 2.5 Test Rename validation (empty/whitespace rejected)
- [ ] 2.6 Test Archive/Unarchive guards (re-archive rejected)
- [ ] 2.7 Test ContextScope.Initiative validation

## 3. Infrastructure Layer

- [ ] 3.1 Create `ChatThreadConfiguration` (table `ChatThreads`, JSONB Messages column, JSONB ContextScope columns or discriminator + nullable InitiativeId)
- [ ] 3.2 Implement `ChatThreadRepository` with filtered listings (by user, by initiative, by status, ordered by LastMessageAt DESC NULLS LAST)
- [ ] 3.3 Register repository in DI
- [ ] 3.4 Add EF Core migration `AddChatThreads`

## 4. Application Layer — Context Builder

- [ ] 4.1 Create `IInitiativeChatContextBuilder` interface and `InitiativeChatContextBuilder` implementation
- [ ] 4.2 Implement `Build(userId, initiativeId, recentMessages)` enforcing caps (20 decisions, all open risks, latest req/design snapshots, 50 commitments, 50 delegations, 30 capture summaries) and filtering by UserId
- [ ] 4.3 Define DTO `InitiativeChatContextPayload` returned by the builder
- [ ] 4.4 Unit test the builder: caps enforced, cross-user data excluded, empty initiative produces minimal payload

## 5. Application Layer — Chat Completion Service

- [ ] 5.1 Create `IInitiativeChatCompletionService` and implementation `InitiativeChatCompletionService`
- [ ] 5.2 Define structured AI response envelope DTO `{ assistantText: string, sourceReferences: SourceReference[] }` and JSON schema; implement defensive parser with fallback to "raw text, no citations"
- [ ] 5.3 Define system prompt template: ground answers in supplied context, cite every claim, refuse out-of-scope politely
- [ ] 5.4 Implement orchestration: append user message -> build context -> call IAiCompletionService -> parse response -> drop unknown SourceReference EntityIds -> append assistant message -> persist
- [ ] 5.5 Handle `AiProviderException` by appending an Assistant message with friendly error text
- [ ] 5.6 Handle `TasteLimitExceededException` by appending a System message "Daily AI limit reached"
- [ ] 5.7 Conversation history budget: include last N messages by token estimate; enforce a hard cap

## 6. Application Layer — Vertical Slice Handlers

- [ ] 6.1 `StartInitiativeChatThread` command handler (POST /api/initiatives/{id}/chat/threads)
- [ ] 6.2 `ListInitiativeChatThreads` query handler (GET /api/initiatives/{id}/chat/threads, filterable by status)
- [ ] 6.3 `GetInitiativeChatThread` query handler (GET /api/initiatives/{id}/chat/threads/{threadId})
- [ ] 6.4 `RenameInitiativeChatThread` command handler (PUT /api/initiatives/{id}/chat/threads/{threadId})
- [ ] 6.5 `PostInitiativeChatMessage` command handler (POST /api/initiatives/{id}/chat/threads/{threadId}/messages) — orchestrates the completion service
- [ ] 6.6 `ArchiveInitiativeChatThread` command handler (POST /api/initiatives/{id}/chat/threads/{threadId}/archive)
- [ ] 6.7 `UnarchiveInitiativeChatThread` command handler (POST /api/initiatives/{id}/chat/threads/{threadId}/unarchive)

## 7. Application Unit Tests

- [ ] 7.1 Test PostInitiativeChatMessage happy path: appends user + assistant messages with citations
- [ ] 7.2 Test fall-back parser: malformed AI envelope persists raw text with empty citations
- [ ] 7.3 Test invalid SourceReference EntityIds dropped before persisting
- [ ] 7.4 Test AiProviderException path appends friendly Assistant message
- [ ] 7.5 Test TasteLimitExceededException path appends System message
- [ ] 7.6 Test posting to Archived thread returns Conflict
- [ ] 7.7 Test cross-user isolation in handlers (404 for cross-user threads)

## 8. Web API Layer

- [ ] 8.1 Create `InitiativeChatEndpoints` minimal API mapping all routes under `/api/initiatives/{id}/chat/threads`
- [ ] 8.2 Create request/response DTOs for thread, message, source-reference, post-message response

## 9. Frontend — Models and Service

- [ ] 9.1 Create TypeScript models: `ChatThread`, `ChatMessage`, `SourceReference`, `TokenUsage`, `ContextScope`
- [ ] 9.2 Create `InitiativeChatService` with thread CRUD, listing, archive/unarchive, post-message methods
- [ ] 9.3 Create signals for active thread and message list state

## 10. Frontend — Initiative Chat Tab

- [ ] 10.1 Add "Chat" tab to the initiative detail Tabs container (next to "Living Brief")
- [ ] 10.2 Create `InitiativeChatTabComponent` shell with two-pane layout
- [ ] 10.3 Create `ChatThreadRailComponent` showing active threads (sorted by LastMessageAt desc) and collapsed Archived section, with new-thread button and per-thread rename/archive actions
- [ ] 10.4 Create `ChatThreadViewComponent` displaying messages in order (User right, Assistant left, System centred), timestamp and token-usage tooltips
- [ ] 10.5 Create `ChatMessageComposerComponent` with textarea, submit on Enter (Shift+Enter newline), disabled while awaiting reply
- [ ] 10.6 Create `SourceReferenceChipComponent` rendering typed chips and routing on click to capture/commitment/delegation detail or to the Living Brief tab anchored at the relevant section
- [ ] 10.7 Render Failed/limit System messages distinctly (PrimeNG Message component) so users understand AI errors
- [ ] 10.8 Loading state: show a skeleton or spinner in place of the assistant message while the request is in flight

## 11. E2E Tests

- [ ] 11.1 E2E: start a thread, post a question, receive a reply
- [ ] 11.2 E2E: source reference chips are clickable and navigate to the correct record
- [ ] 11.3 E2E: archive a thread, list shows it under Archived only
- [ ] 11.4 E2E: rename a thread persists across reloads
- [ ] 11.5 E2E: user isolation — User A cannot read User B's thread (404)
- [ ] 11.6 E2E: posting to an archived thread returns Conflict and the UI surfaces an error
