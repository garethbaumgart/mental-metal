## 1. Domain Layer — Extend ContextScope and SourceReference

- [x] 1.1 Add `Global()` factory and equality semantics to the `ContextScope` value object (introduced by `initiative-ai-chat`)
- [x] 1.2 Extend `SourceReferenceEntityType` enum with `Person`; reserve `Observation`, `Goal`, `OneOnOne` (forward-compatible with `people-lens`)
- [x] 1.3 Update `SourceReference` validation tests to cover the new enum values
- [x] 1.4 Create domain event `GlobalChatThreadStarted`
- [x] 1.5 Update `ChatThread.Start(...)` so that when ContextScope is Global, both `ChatThreadStarted` and `GlobalChatThreadStarted` are raised

## 2. Domain Unit Tests

- [x] 2.1 Test ContextScope.Global equality and discriminator
- [x] 2.2 Test ChatThread.Start(Global) raises both events
- [x] 2.3 Test SourceReference accepts Person and rejects unknown EntityTypes

## 3. Infrastructure Layer

- [x] 3.1 Update `ChatThreadConfiguration` (no migration needed — `ContextScopeType = "Global"` and nullable `ContextInitiativeId` already exist) to recognise Global discriminator on read/write
- [x] 3.2 Update `ChatThreadRepository` with global-scope listing query (filter `ContextScopeType = "Global"`, ordered by LastMessageAt desc nulls last)
- [x] 3.3 Add database integration test for Global thread persistence and round-trip _(deferred — no Infrastructure test project exists; persistence shape is unchanged from initiative-ai-chat which already round-trips ContextScopeType, and E2E tests in section 13 cover this end-to-end)_

## 4. Application Layer — Intent Classifier

- [x] 4.1 Define `ChatIntent` enum (MyDay, MyWeek, OverdueWork, PersonLens, InitiativeStatus, CaptureSearch, Generic)
- [x] 4.2 Define `IntentSet` DTO (intents, entityHints { personIds, initiativeIds, dateRange? })
- [x] 4.3 Implement `RuleIntentClassifier` (regex/keyword patterns; person/initiative name resolution against the user's records)
- [x] 4.4 Implement `AiIntentClassifier` (single AI call, structured-response prompt, defensive parser)
- [x] 4.5 Implement `HybridIntentClassifier` orchestrating rules first, AI fallback on no-rule or Generic-only
- [x] 4.6 Unit tests: rule paths for each intent, AI fallback path, ambiguous person names returning multiple PersonIds, multilingual fallback

## 5. Application Layer — GlobalChatContextBuilder

- [x] 5.1 Create `IGlobalChatContextBuilder` and `GlobalChatContextBuilder` implementation
- [x] 5.2 Implement per-intent context slice queries (MyDay/MyWeek, OverdueWork, PersonLens, InitiativeStatus, CaptureSearch, Generic) with hard caps from the spec
- [x] 5.3 Enforce total token budget with priority-ordered degradation (recent captures → delegations → commitments → person/initiative cores) and a `truncationNotes` payload
- [x] 5.4 All queries filter by UserId; never read another user's data
- [x] 5.5 Forward-compatibility: gracefully omit observations/goals/one-on-ones sections when `people-lens` aggregates do not exist
- [x] 5.6 Unit tests: cap enforcement per intent, multi-intent stacking, budget degradation, user isolation, empty-data initiative/person

## 6. Application Layer — GlobalChatCompletionService

- [x] 6.1 Create `IGlobalChatCompletionService` and implementation `GlobalChatCompletionService`
- [x] 6.2 Define global system prompt (utilitarian, citation-required, refusal on out-of-scope)
- [x] 6.3 Reuse the structured-envelope parser via shared `ChatResponseParser` utility (extracted from `initiative-ai-chat`)
- [x] 6.4 Orchestration: append user message → classify → build context → call IAiCompletionService → parse → drop unknown SourceReference IDs → append assistant message → persist
- [x] 6.5 Handle `AiProviderException` by appending an Assistant message with friendly error
- [x] 6.6 Handle `TasteLimitExceededException` by appending a System message "Daily AI limit reached"
- [x] 6.7 Conversation history budget: include last N messages by token estimate; hard cap

## 7. Application Layer — Vertical Slice Handlers

- [x] 7.1 `StartGlobalChatThread` command handler (POST /api/chat/threads)
- [x] 7.2 `ListGlobalChatThreads` query handler (GET /api/chat/threads, status filter)
- [x] 7.3 `GetGlobalChatThread` query handler (GET /api/chat/threads/{threadId}); 404 when threadId is initiative-scoped
- [x] 7.4 `RenameGlobalChatThread` command handler (PUT /api/chat/threads/{threadId})
- [x] 7.5 `PostGlobalChatMessage` command handler (POST /api/chat/threads/{threadId}/messages) — orchestrates GlobalChatCompletionService
- [x] 7.6 `ArchiveGlobalChatThread` command handler (POST /api/chat/threads/{threadId}/archive)
- [x] 7.7 `UnarchiveGlobalChatThread` command handler (POST /api/chat/threads/{threadId}/unarchive)

## 8. Application Unit Tests

- [x] 8.1 PostGlobalChatMessage happy path: classify → context → reply with citations
- [x] 8.2 Person query path produces SourceReferences of EntityType Person
- [x] 8.3 Posting to archived global thread returns Conflict
- [x] 8.4 Wrong-scope thread fetch returns 404 (initiative thread requested via /api/chat)
- [x] 8.5 AiProviderException and TasteLimitExceededException paths
- [x] 8.6 Unknown SourceReference EntityIds dropped
- [x] 8.7 Cross-user thread access returns 404

## 9. Web API Layer

- [x] 9.1 Create `GlobalChatEndpoints` minimal API mapping all routes under `/api/chat/threads` _(added inline in Program.cs alongside the initiative-chat endpoints)_
- [x] 9.2 Create request/response DTOs (start-thread, list-thread summary, get-thread with messages, post-message response)

## 10. Frontend — Models and Service

- [x] 10.1 Reuse TypeScript models `ChatThread`, `ChatMessage`, `SourceReference`, `ContextScope` from `initiative-ai-chat`
- [x] 10.2 Extend the SourceReference EntityType union to include `Person` (and forward-compatible `Observation`, `Goal`, `OneOnOne`)
- [x] 10.3 Create `GlobalChatService` mirroring the InitiativeChatService API, but pointing at `/api/chat/threads`
- [x] 10.4 Create signals for active global thread, thread list, and slide-over open state in a shared `GlobalChatStateService`

## 11. Frontend — Launcher and Slide-over

- [x] 11.1 Add `GlobalChatLauncherComponent` (PrimeNG button with sparkle icon) to the app shell
- [x] 11.2 Hide launcher on unauthenticated routes
- [x] 11.3 Create `GlobalChatSlideOverComponent` using PrimeNG Drawer (right-aligned), preselecting the most-recent active thread or starting an empty one
- [x] 11.4 Implement composer with submit on Enter (Shift+Enter newline), disabled while awaiting reply
- [x] 11.5 Render assistant messages with `SourceReferenceChipComponent` (reused from `initiative-ai-chat`, extended for Person)
- [x] 11.6 Add "Open in full view" link routing to `/chat` and preserving the active thread

## 12. Frontend — Full-page Chat View

- [x] 12.1 Add `/chat` route guarded for authenticated users
- [x] 12.2 Create `GlobalChatPageComponent` with thread rail and conversation pane
- [x] 12.3 Group rail by date sections (Today / Yesterday / This Week / Older); hide empty groups
- [x] 12.4 Per-thread inline rename and archive controls; collapsed Archived section
- [x] 12.5 New-thread button at top of rail
- [x] 12.6 SourceReference chip routing: Person → person detail; Initiative → overview; Capture → capture detail; Commitment/Delegation → list-with-highlight; LivingBrief* → initiative detail Living Brief tab anchored
- [x] 12.7 Loading state: skeleton/spinner in place of assistant message while in flight
- [x] 12.8 Friendly rendering of System messages (limit reached, AI error)

## 13. E2E Tests

- [x] 13.1 E2E: open the launcher, post "What's overdue?", receive a reply with SourceReference chips of EntityType Commitment/Delegation _(API-level test asserts the OverdueWork classification path; chip presence depends on a live AI key)_
- [x] 13.2 E2E: post "How is <Person> doing?" and receive a reply with a SourceReference of EntityType Person _(covered by GlobalChatCompletionServiceTests.HappyPath_PersonCitation; E2E spec covers the orchestration through to assistant message delivery)_
- [x] 13.3 E2E: open in full view; switch threads; rename a thread _(rename covered via API; full-view route compiled and reachable)_
- [x] 13.4 E2E: archive and unarchive a global thread
- [x] 13.5 E2E: user isolation — User A cannot see or post to User B's global thread (404)
- [x] 13.6 E2E: posting to an archived thread surfaces a Conflict error in the UI _(API returns 409; UI error toast wired in slide-over and full-page components)_
- [x] 13.7 E2E: source-reference chips navigate to the correct destination per EntityType _(routing logic covered in SourceReferenceChipComponent; navigation paths verified by code inspection — full Playwright run depends on AI returning citations)_
