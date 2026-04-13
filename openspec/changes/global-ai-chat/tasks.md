## 1. Domain Layer — Extend ContextScope and SourceReference

- [ ] 1.1 Add `Global()` factory and equality semantics to the `ContextScope` value object (introduced by `initiative-ai-chat`)
- [ ] 1.2 Extend `SourceReferenceEntityType` enum with `Person`; reserve `Observation`, `Goal`, `OneOnOne` (forward-compatible with `people-lens`)
- [ ] 1.3 Update `SourceReference` validation tests to cover the new enum values
- [ ] 1.4 Create domain event `GlobalChatThreadStarted`
- [ ] 1.5 Update `ChatThread.Start(...)` so that when ContextScope is Global, both `ChatThreadStarted` and `GlobalChatThreadStarted` are raised

## 2. Domain Unit Tests

- [ ] 2.1 Test ContextScope.Global equality and discriminator
- [ ] 2.2 Test ChatThread.Start(Global) raises both events
- [ ] 2.3 Test SourceReference accepts Person and rejects unknown EntityTypes

## 3. Infrastructure Layer

- [ ] 3.1 Update `ChatThreadConfiguration` (no migration needed — `ContextScopeType = "Global"` and nullable `ContextInitiativeId` already exist) to recognise Global discriminator on read/write
- [ ] 3.2 Update `ChatThreadRepository` with global-scope listing query (filter `ContextScopeType = "Global"`, ordered by LastMessageAt desc nulls last)
- [ ] 3.3 Add database integration test for Global thread persistence and round-trip

## 4. Application Layer — Intent Classifier

- [ ] 4.1 Define `ChatIntent` enum (MyDay, MyWeek, OverdueWork, PersonLens, InitiativeStatus, CaptureSearch, Generic)
- [ ] 4.2 Define `IntentSet` DTO (intents, entityHints { personIds, initiativeIds, dateRange? })
- [ ] 4.3 Implement `RuleIntentClassifier` (regex/keyword patterns; person/initiative name resolution against the user's records)
- [ ] 4.4 Implement `AiIntentClassifier` (single AI call, structured-response prompt, defensive parser)
- [ ] 4.5 Implement `HybridIntentClassifier` orchestrating rules first, AI fallback on no-rule or Generic-only
- [ ] 4.6 Unit tests: rule paths for each intent, AI fallback path, ambiguous person names returning multiple PersonIds, multilingual fallback

## 5. Application Layer — GlobalChatContextBuilder

- [ ] 5.1 Create `IGlobalChatContextBuilder` and `GlobalChatContextBuilder` implementation
- [ ] 5.2 Implement per-intent context slice queries (MyDay/MyWeek, OverdueWork, PersonLens, InitiativeStatus, CaptureSearch, Generic) with hard caps from the spec
- [ ] 5.3 Enforce total token budget with priority-ordered degradation (recent captures → delegations → commitments → person/initiative cores) and a `truncationNotes` payload
- [ ] 5.4 All queries filter by UserId; never read another user's data
- [ ] 5.5 Forward-compatibility: gracefully omit observations/goals/one-on-ones sections when `people-lens` aggregates do not exist
- [ ] 5.6 Unit tests: cap enforcement per intent, multi-intent stacking, budget degradation, user isolation, empty-data initiative/person

## 6. Application Layer — GlobalChatCompletionService

- [ ] 6.1 Create `IGlobalChatCompletionService` and implementation `GlobalChatCompletionService`
- [ ] 6.2 Define global system prompt (utilitarian, citation-required, refusal on out-of-scope)
- [ ] 6.3 Reuse the structured-envelope parser via shared `ChatResponseParser` utility (extracted from `initiative-ai-chat`)
- [ ] 6.4 Orchestration: append user message → classify → build context → call IAiCompletionService → parse → drop unknown SourceReference IDs → append assistant message → persist
- [ ] 6.5 Handle `AiProviderException` by appending an Assistant message with friendly error
- [ ] 6.6 Handle `TasteLimitExceededException` by appending a System message "Daily AI limit reached"
- [ ] 6.7 Conversation history budget: include last N messages by token estimate; hard cap

## 7. Application Layer — Vertical Slice Handlers

- [ ] 7.1 `StartGlobalChatThread` command handler (POST /api/chat/threads)
- [ ] 7.2 `ListGlobalChatThreads` query handler (GET /api/chat/threads, status filter)
- [ ] 7.3 `GetGlobalChatThread` query handler (GET /api/chat/threads/{threadId}); 404 when threadId is initiative-scoped
- [ ] 7.4 `RenameGlobalChatThread` command handler (PUT /api/chat/threads/{threadId})
- [ ] 7.5 `PostGlobalChatMessage` command handler (POST /api/chat/threads/{threadId}/messages) — orchestrates GlobalChatCompletionService
- [ ] 7.6 `ArchiveGlobalChatThread` command handler (POST /archive)
- [ ] 7.7 `UnarchiveGlobalChatThread` command handler (POST /unarchive)

## 8. Application Unit Tests

- [ ] 8.1 PostGlobalChatMessage happy path: classify → context → reply with citations
- [ ] 8.2 Person query path produces SourceReferences of EntityType Person
- [ ] 8.3 Posting to archived global thread returns Conflict
- [ ] 8.4 Wrong-scope thread fetch returns 404 (initiative thread requested via /api/chat)
- [ ] 8.5 AiProviderException and TasteLimitExceededException paths
- [ ] 8.6 Unknown SourceReference EntityIds dropped
- [ ] 8.7 Cross-user thread access returns 404

## 9. Web API Layer

- [ ] 9.1 Create `GlobalChatEndpoints` minimal API mapping all routes under `/api/chat/threads`
- [ ] 9.2 Create request/response DTOs (start-thread, list-thread summary, get-thread with messages, post-message response)

## 10. Frontend — Models and Service

- [ ] 10.1 Reuse TypeScript models `ChatThread`, `ChatMessage`, `SourceReference`, `ContextScope` from `initiative-ai-chat`
- [ ] 10.2 Extend the SourceReference EntityType union to include `Person` (and forward-compatible `Observation`, `Goal`, `OneOnOne`)
- [ ] 10.3 Create `GlobalChatService` mirroring the InitiativeChatService API, but pointing at `/api/chat/threads`
- [ ] 10.4 Create signals for active global thread, thread list, and slide-over open state in a shared `GlobalChatStateService`

## 11. Frontend — Launcher and Slide-over

- [ ] 11.1 Add `GlobalChatLauncherComponent` (PrimeNG button with sparkle icon) to the app shell
- [ ] 11.2 Hide launcher on unauthenticated routes
- [ ] 11.3 Create `GlobalChatSlideOverComponent` using PrimeNG Sidebar (right-aligned), preselecting the most-recent active thread or starting an empty one
- [ ] 11.4 Implement composer with submit on Enter (Shift+Enter newline), disabled while awaiting reply
- [ ] 11.5 Render assistant messages with `SourceReferenceChipComponent` (reused from `initiative-ai-chat`, extended for Person)
- [ ] 11.6 Add "Open in full view" link routing to `/chat` and preserving the active thread

## 12. Frontend — Full-page Chat View

- [ ] 12.1 Add `/chat` route guarded for authenticated users
- [ ] 12.2 Create `GlobalChatPageComponent` with thread rail and conversation pane
- [ ] 12.3 Group rail by date sections (Today / Yesterday / This Week / Older); hide empty groups
- [ ] 12.4 Per-thread inline rename and archive controls; collapsed Archived section
- [ ] 12.5 New-thread button at top of rail
- [ ] 12.6 SourceReference chip routing: Person → person detail; Initiative → overview; Capture → capture detail; Commitment/Delegation → list-with-highlight; LivingBrief* → initiative detail Living Brief tab anchored
- [ ] 12.7 Loading state: skeleton/spinner in place of assistant message while in flight
- [ ] 12.8 Friendly rendering of System messages (limit reached, AI error)

## 13. E2E Tests

- [ ] 13.1 E2E: open the launcher, post "What's overdue?", receive a reply with SourceReference chips of EntityType Commitment/Delegation
- [ ] 13.2 E2E: post "How is <Person> doing?" and receive a reply with a SourceReference of EntityType Person
- [ ] 13.3 E2E: open in full view; switch threads; rename a thread
- [ ] 13.4 E2E: archive and unarchive a global thread
- [ ] 13.5 E2E: user isolation — User A cannot see or post to User B's global thread (404)
- [ ] 13.6 E2E: posting to an archived thread surfaces a Conflict error in the UI
- [ ] 13.7 E2E: source-reference chips navigate to the correct destination per EntityType
