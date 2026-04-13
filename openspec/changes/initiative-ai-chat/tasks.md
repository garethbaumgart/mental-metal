## 1. Domain Layer — ChatThread aggregate

- [x] 1.1 Create `ChatRole` enum (User, Assistant, System) and `ChatThreadStatus` enum (Active, Archived) in `src/MentalMetal.Domain/ChatThreads/`
- [x] 1.2 Create `ContextScope` value object with `Initiative(InitiativeId)` factory; reserve `Global()` for global-ai-chat (not implemented in this spec)
- [x] 1.3 Create `SourceReferenceEntityType` enum (Capture, Commitment, Delegation, LivingBriefDecision, LivingBriefRisk, LivingBriefRequirements, LivingBriefDesignDirection, Initiative)
- [x] 1.4 Create `SourceReference` value object (EntityType, EntityId, SnippetText?, RelevanceScore?) with validation
- [x] 1.5 Create `TokenUsage` value object (PromptTokens, CompletionTokens)
- [x] 1.6 Create `ChatMessage` value object (MessageOrdinal, Role, Content, CreatedAt, SourceReferences, TokenUsage?) with validation that SourceReferences is empty unless Role is Assistant
- [x] 1.7 Create `ChatThread` aggregate root with Id, UserId, ContextScope, Title, Status, CreatedAt, LastMessageAt, MessageCount, Messages
- [x] 1.8 Implement factory `Start(userId, contextScope)`; raise `ChatThreadStarted`
- [x] 1.9 Implement `AppendUserMessage(content)` returning the new message; raise `ChatMessageSent`; auto-set Title from first user message; update LastMessageAt and MessageCount
- [x] 1.10 Implement `AppendAssistantMessage(content, sourceReferences, tokenUsage?)`; raise `ChatMessageReceived`
- [x] 1.11 Implement `AppendSystemMessage(content)` for error / limit notices
- [x] 1.12 Implement `Rename(title, source)` with validation; raise `ChatThreadRenamed`
- [x] 1.13 Implement `Archive()` and `Unarchive()` with status guards; raise `ChatThreadArchived` / unarchive equivalent
- [x] 1.14 Create `IChatThreadRepository` interface
- [x] 1.15 Create domain events: `ChatThreadStarted`, `ChatMessageSent`, `ChatMessageReceived`, `ChatThreadRenamed`, `ChatThreadArchived`

## 2. Domain Unit Tests

- [x] 2.1 Test ChatThread.Start invariants
- [x] 2.2 Test message ordinal contiguity and updates to MessageCount/LastMessageAt
- [x] 2.3 Test SourceReferences rejected on User/System messages
- [x] 2.4 Test auto-title from first user message (truncated at 80 chars with ellipsis)
- [x] 2.5 Test Rename validation (empty/whitespace rejected)
- [x] 2.6 Test Archive/Unarchive guards (re-archive rejected)
- [x] 2.7 Test ContextScope.Initiative validation

## 3. Infrastructure Layer

- [x] 3.1 Create `ChatThreadConfiguration` (table `ChatThreads`, JSONB Messages column, JSONB ContextScope columns or discriminator + nullable InitiativeId)
- [x] 3.2 Implement `ChatThreadRepository` with filtered listings (by user, by initiative, by status, ordered by LastMessageAt DESC NULLS LAST)
- [x] 3.3 Register repository in DI
- [x] 3.4 Add EF Core migration `AddChatThreads`

## 4. Application Layer — Context Builder

- [x] 4.1 Create `IInitiativeChatContextBuilder` interface and `InitiativeChatContextBuilder` implementation
- [x] 4.2 Implement `Build(userId, initiativeId, userQuestion, recentMessages)` — `userId` is required and non-optional; every downstream query filters by it. Enforce caps (20 decisions, all open risks, latest req/design snapshots, 50 commitments, 50 delegations, 30 capture summaries).
- [x] 4.3 Define DTO `InitiativeChatContextPayload` returned by the builder
- [x] 4.4 Unit test the builder: caps enforced, cross-user data excluded, empty initiative produces minimal payload

## 5. Application Layer — Chat Completion Service

- [x] 5.1 Create `IInitiativeChatCompletionService` and implementation `InitiativeChatCompletionService`
- [x] 5.2 Define structured AI response envelope DTO `{ assistantText: string, sourceReferences: SourceReference[] }` and JSON schema; implement defensive parser with fallback to "raw text, no citations"
- [x] 5.3 Define system prompt template: ground answers in supplied context, cite every claim, refuse out-of-scope politely
- [x] 5.4 Implement orchestration: append user message -> build context -> call IAiCompletionService -> parse response -> drop unknown SourceReference EntityIds -> append assistant message -> persist
- [x] 5.5 Handle `AiProviderException` by appending an Assistant message with friendly error text
- [x] 5.6 Handle `TasteLimitExceededException` by appending a System message "Daily AI limit reached"
- [x] 5.7 Conversation history budget: include last N messages by token estimate; enforce a hard cap

## 6. Application Layer — Vertical Slice Handlers

- [x] 6.1 `StartInitiativeChatThread` command handler (POST /api/initiatives/{id}/chat/threads)
- [x] 6.2 `ListInitiativeChatThreads` query handler (GET /api/initiatives/{id}/chat/threads, filterable by status)
- [x] 6.3 `GetInitiativeChatThread` query handler (GET /api/initiatives/{id}/chat/threads/{threadId})
- [x] 6.4 `RenameInitiativeChatThread` command handler (PUT /api/initiatives/{id}/chat/threads/{threadId})
- [x] 6.5 `PostInitiativeChatMessage` command handler (POST /api/initiatives/{id}/chat/threads/{threadId}/messages) — orchestrates the completion service
- [x] 6.6 `ArchiveInitiativeChatThread` command handler (POST /api/initiatives/{id}/chat/threads/{threadId}/archive)
- [x] 6.7 `UnarchiveInitiativeChatThread` command handler (POST /api/initiatives/{id}/chat/threads/{threadId}/unarchive)

## 7. Application Unit Tests

- [x] 7.1 Test PostInitiativeChatMessage happy path: appends user + assistant messages with citations
- [x] 7.2 Test fall-back parser: malformed AI envelope persists raw text with empty citations
- [x] 7.3 Test invalid SourceReference EntityIds dropped before persisting
- [x] 7.4 Test AiProviderException path appends friendly Assistant message
- [x] 7.5 Test TasteLimitExceededException path appends System message
- [x] 7.6 Test posting to Archived thread returns Conflict
- [x] 7.7 Test cross-user isolation in handlers (404 for cross-user threads)

## 8. Web API Layer

- [x] 8.1 Create `InitiativeChatEndpoints` minimal API mapping all routes under `/api/initiatives/{id}/chat/threads`
- [x] 8.2 Create request/response DTOs for thread, message, source-reference, post-message response

## 9. Frontend — Models and Service

- [x] 9.1 Create TypeScript models: `ChatThread`, `ChatMessage`, `SourceReference`, `TokenUsage`, `ContextScope`
- [x] 9.2 Create `InitiativeChatService` with thread CRUD, listing, archive/unarchive, post-message methods
- [x] 9.3 Create signals for active thread and message list state

## 10. Frontend — Initiative Chat Tab

- [x] 10.1 Add "Chat" tab to the initiative detail Tabs container (next to "Living Brief")
- [x] 10.2 Create `InitiativeChatTabComponent` shell with two-pane layout
- [x] 10.3 Create `ChatThreadRailComponent` showing active threads (sorted by LastMessageAt desc) and collapsed Archived section, with new-thread button and per-thread rename/archive actions (inlined into InitiativeChatTabComponent)
- [x] 10.4 Create `ChatThreadViewComponent` displaying messages in order (User right, Assistant left, System centred), timestamp and token-usage tooltips (inlined)
- [x] 10.5 Create `ChatMessageComposerComponent` with textarea, submit on Enter (Shift+Enter newline), disabled while awaiting reply (inlined)
- [x] 10.6 Create `SourceReferenceChipComponent` rendering typed chips and routing on click to capture/commitment/delegation detail or to the Living Brief tab anchored at the relevant section
- [x] 10.7 Render Failed/limit System messages distinctly (PrimeNG Message component) so users understand AI errors
- [x] 10.8 Loading state: show a skeleton or spinner in place of the assistant message while the request is in flight

## 11. E2E Tests

- [x] 11.1 E2E: start a thread, post a question, receive a reply
- [x] 11.2a Unit-test coverage: source reference chips route to correct record (covered by unit tests on the chip component)
- [ ] 11.2b E2E: source reference chips are clickable and navigate to the correct record (deferred — depends on live AI citations)
- [x] 11.3 E2E: archive a thread, list shows it under Archived only
- [x] 11.4 E2E: rename a thread persists across reloads
- [x] 11.5 E2E: user isolation — User A cannot read User B's thread (404)
- [x] 11.6 E2E: posting to an archived thread returns Conflict and the UI surfaces an error
- [ ] 11.7 Manual smoke test against staging: start thread, ask question, verify assistant reply and source chips render correctly
