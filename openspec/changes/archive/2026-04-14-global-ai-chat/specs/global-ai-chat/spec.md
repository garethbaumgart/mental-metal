## ADDED Requirements

### Requirement: Global ContextScope variant

The `ContextScope` value object (introduced by `initiative-ai-chat`) SHALL support a `Global()` variant carrying no fields. A `ChatThread` SHALL be considered global when its `ContextScope` is `Global()`. The persisted discriminator value is `"Global"` and `ContextInitiativeId` is `NULL` for global threads.

#### Scenario: Construct a global ContextScope

- **WHEN** `ContextScope.Global()` is constructed
- **THEN** the value object equals other `ContextScope.Global()` instances and is distinguishable from any `ContextScope.Initiative(_)` instance

#### Scenario: Persisted discriminator

- **WHEN** a global ChatThread is persisted
- **THEN** the row has ContextScopeType = "Global" and ContextInitiativeId = NULL

### Requirement: SourceReferenceEntityType extended with Person

The `SourceReferenceEntityType` enum SHALL include `Person` so that assistant messages can cite a Person record. The enum SHALL also reserve forward-compatible values `Observation`, `Goal`, and `OneOnOne` for use once `people-lens` is implemented.

#### Scenario: Cite a person

- **WHEN** an assistant message references a person ("Jane Doe is on track")
- **THEN** the message includes a SourceReference with EntityType `Person` and EntityId set to that person's Id

### Requirement: Start a global chat thread

The system SHALL allow an authenticated user to start a global chat thread via `POST /api/chat/threads`. The system SHALL create a ChatThread with `ContextScope.Global()`, empty Title, Status `Active`, and raise BOTH the generic `ChatThreadStarted` event and the specific `GlobalChatThreadStarted` event.

#### Scenario: Start a global thread

- **WHEN** an authenticated user sends `POST /api/chat/threads` with no body (or empty body)
- **THEN** the system creates a global ChatThread and returns HTTP 201 with the thread (id, contextScope = Global, empty title, empty messages, status Active)

#### Scenario: Both events raised

- **WHEN** a global thread is started
- **THEN** subscribers to `ChatThreadStarted` and subscribers to `GlobalChatThreadStarted` both receive an event for that thread

### Requirement: List global chat threads

The system SHALL allow an authenticated user to list their global chat threads via `GET /api/chat/threads`, optionally filtered by `status` (default `Active`). Threads SHALL be ordered by `LastMessageAt DESC` (nulls last, then `CreatedAt DESC`). Initiative-scoped threads SHALL NOT appear in this listing.

#### Scenario: List active global threads

- **WHEN** an authenticated user sends `GET /api/chat/threads`
- **THEN** the system returns only the user's global threads with Active status, newest first

#### Scenario: Initiative threads not included

- **WHEN** the user has both initiative-scoped and global threads
- **THEN** `GET /api/chat/threads` returns only global threads; `GET /api/initiatives/{id}/chat/threads` returns only that initiative's threads

#### Scenario: Empty list

- **WHEN** the user has no global threads
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Get a global chat thread

The system SHALL allow an authenticated user to fetch a global thread (with all messages) via `GET /api/chat/threads/{threadId}`.

#### Scenario: Get a global thread

- **WHEN** the user requests an existing global thread they own
- **THEN** the response includes thread metadata and all messages in MessageOrdinal order

#### Scenario: Wrong scope returns 404

- **WHEN** the user requests `GET /api/chat/threads/{threadId}` with the ID of an initiative-scoped thread
- **THEN** the system returns HTTP 404 (route is global-only)

#### Scenario: Cross-user thread

- **WHEN** the user requests a global thread owned by a different user
- **THEN** the system returns HTTP 404

### Requirement: Intent classification

The system SHALL classify each posted user message into an `IntentSet` containing one or more `ChatIntent` values from `{ MyDay, MyWeek, OverdueWork, PersonLens, InitiativeStatus, CaptureSearch, Generic }` plus `entityHints` of `{ personIds, initiativeIds, dateRange? }`. Classification SHALL be performed by a hybrid classifier: a deterministic rule layer first; an AI fallback only when no rule fires or only `Generic` is matched.

#### Scenario: Rule-classified person query

- **WHEN** the user posts "How is Jane Doe doing?" and a Person named "Jane Doe" exists for the user
- **THEN** the IntentSet includes `PersonLens` with that Person's Id in entityHints.personIds; no AI call is made for classification

#### Scenario: Rule-classified overdue query

- **WHEN** the user posts "What's overdue?"
- **THEN** the IntentSet includes `OverdueWork` and no AI call is made for classification

#### Scenario: AI-fallback classification

- **WHEN** the user posts a message that the rule layer cannot classify (e.g. "Anything I should be worried about?")
- **THEN** the system invokes an AI classifier with a structured response and uses its IntentSet

#### Scenario: Ambiguous person name

- **WHEN** the user posts "How is Sarah doing?" and two Persons named "Sarah Chen" and "Sarah Patel" exist
- **THEN** the IntentSet's entityHints.personIds includes BOTH Persons' Ids

#### Scenario: Generic fallback

- **WHEN** neither rules nor AI yield a classification
- **THEN** the IntentSet contains only `Generic`

### Requirement: GlobalChatContextBuilder is intent-driven, capped, and user-scoped

The `GlobalChatContextBuilder` SHALL assemble a context payload determined by the IntentSet. All queries SHALL be filtered by the thread's UserId. The following per-intent caps SHALL apply:

- `MyDay` / `MyWeek`: at most 30 commitments and 30 delegations matching the time window.
- `OverdueWork`: at most 30 overdue commitments and 30 overdue delegations.
- `PersonLens`: at most 5 persons, each with summary, open commitments (max 10), open delegations (max 10), and (when `people-lens` exists) most-recent observations (max 10), goals (max 10), and most-recent OneOnOne.
- `InitiativeStatus`: at most 5 initiatives, each with metadata, LivingBrief summary, open risks, recent decisions (max 10), and linked open commitments/delegations (max 20 each).
- `CaptureSearch`: at most 30 confirmed captures (within the date range when supplied).
- `Generic`: counts (open commitments/delegations/active initiatives), top 5 most-overdue commitments, top 5 most-recent captures.

The total assembled payload SHALL be hard-capped by a configurable token budget (default ~16k input tokens). When the budget would be exceeded, the builder SHALL trim sections in this priority order (least-important first): recent captures → delegations → commitments → person/initiative cores.

#### Scenario: Caps enforced

- **WHEN** the user has 200 commitments and the intent is `OverdueWork`
- **THEN** the assembled context contains at most 30 overdue commitments

#### Scenario: User isolation

- **WHEN** building context for a global thread owned by User A
- **THEN** every record in the payload (commitments, delegations, captures, persons, initiatives, observations, goals, one-on-ones) belongs to User A; no User B records are read

#### Scenario: Forward-compatible with people-lens absent

- **WHEN** `people-lens` is not yet implemented and a `PersonLens` intent is classified
- **THEN** the builder includes the Person summary, commitments, and delegations; observations/goals/one-on-ones sections are simply absent (no error)

#### Scenario: Token budget triggers degradation

- **WHEN** the assembled payload would exceed the configured token budget
- **THEN** the builder trims sections in priority order until under budget; trimmed sections are noted in the payload metadata so the assistant can mention truncation if relevant

#### Scenario: Generic intent payload

- **WHEN** the IntentSet is `{ Generic }`
- **THEN** the payload contains counts and the top-5 most-overdue commitments and top-5 most-recent captures only

### Requirement: Post a global chat message and receive an assistant reply

The system SHALL allow an authenticated user to post a user message via `POST /api/chat/threads/{threadId}/messages` with body `{ "content": "<text>" }`. The system SHALL: append the user message (raise `ChatMessageSent`), classify the question (intent + entity hints), build the context via `GlobalChatContextBuilder`, call `IAiCompletionService.CompleteAsync` with the global system prompt + assembled context + recent conversation history, parse the structured envelope, drop SourceReference EntityIds not present in the context, append the Assistant message (raise `ChatMessageReceived`), update LastMessageAt and MessageCount, and return HTTP 200 with `{ userMessage, assistantMessage }`.

#### Scenario: "What's on my plate today?"

- **WHEN** an authenticated user posts "What's on my plate today?" to a global thread
- **THEN** the IntentSet includes `MyDay`; the context contains today's commitments and delegations; the assistant reply lists them with SourceReference chips of EntityType Commitment and Delegation

#### Scenario: "How is Jane doing?"

- **WHEN** the user posts "How is Jane doing?" and a Person named "Jane" exists
- **THEN** the assembled context includes Jane's profile + linked work; the assistant reply cites SourceReference of EntityType Person (and any Commitments/Observations as applicable)

#### Scenario: Empty content rejected

- **WHEN** the content is empty or whitespace-only
- **THEN** the system returns HTTP 400 and no messages are appended

#### Scenario: Posting to an archived global thread rejected

- **WHEN** the user posts to an Archived global thread
- **THEN** the system returns HTTP 409 Conflict

#### Scenario: AI provider failure

- **WHEN** `IAiCompletionService.CompleteAsync` throws an `AiProviderException`
- **THEN** the user message is appended; an Assistant message containing a friendly error ("AI service unavailable, please retry") is appended; the system returns HTTP 200

#### Scenario: Taste limit exceeded

- **WHEN** the AI completion call throws a `TasteLimitExceededException`
- **THEN** the user message is appended; a System message "Daily AI limit reached" is appended; the system returns HTTP 200

#### Scenario: Hallucinated citations dropped

- **WHEN** the AI returns a SourceReference whose EntityId was not present in the assembled context
- **THEN** that SourceReference is dropped before persisting; only validated references appear on the persisted assistant message

#### Scenario: Malformed envelope falls back

- **WHEN** the AI response cannot be parsed into the structured envelope
- **THEN** the entire raw text is persisted as the assistant message content with empty SourceReferences

### Requirement: Auto-generated title for global threads

When the first user message is appended to a global thread with empty Title, the system SHALL set Title to the user-message content truncated to 80 characters (with ellipsis if truncated).

#### Scenario: Auto-title

- **WHEN** the user posts the first message "What's overdue this week?" to a new global thread
- **THEN** the thread's Title becomes "What's overdue this week?"

### Requirement: Rename, archive, and unarchive global threads

The system SHALL allow an authenticated user to rename a global thread via `PUT /api/chat/threads/{threadId}` with a non-empty title (max 200 chars), to archive an Active global thread via `POST /api/chat/threads/{threadId}/archive`, and to unarchive an Archived thread via `POST /api/chat/threads/{threadId}/unarchive`. Status guards behave identically to `initiative-ai-chat` thread operations.

#### Scenario: Rename a global thread

- **WHEN** the user renames a global thread to "Weekly review questions"
- **THEN** Title updates and the system returns HTTP 200

#### Scenario: Archive global thread

- **WHEN** the user archives an Active global thread
- **THEN** Status becomes Archived and the system returns HTTP 200

#### Scenario: Re-archive rejected

- **WHEN** the user archives an already-Archived thread (or unarchives an Active thread)
- **THEN** the system returns HTTP 409

### Requirement: Global chat launcher in app shell

The frontend SHALL render a global chat launcher button in the app shell, visible on every authenticated route. Clicking the launcher SHALL open a slide-over chat panel. The panel SHALL preselect the user's most-recent active global thread, or open an empty new thread when none exists. The slide-over SHALL contain the message list, composer, and an "Open in full view" link to `/chat`.

#### Scenario: Launcher visible on all authenticated routes

- **WHEN** a signed-in user is on any authenticated route
- **THEN** the launcher button is visible in the app shell

#### Scenario: Launcher not visible when signed out

- **WHEN** the user is on a public/unauthenticated route
- **THEN** the launcher is not rendered

#### Scenario: Open slide-over with most-recent thread

- **WHEN** a user with at least one active global thread clicks the launcher
- **THEN** the slide-over opens with the most-recent active thread loaded

#### Scenario: Open slide-over with no existing threads

- **WHEN** a user with no global threads clicks the launcher
- **THEN** the slide-over opens with an empty new thread; on first message post, the thread is created via `POST /api/chat/threads` and the message is then posted

### Requirement: Full-page global chat view

The frontend SHALL provide a full-page route at `/chat` containing: a left rail of global threads grouped by date (Today, Yesterday, This Week, Older), a main pane showing the selected thread, a composer at the bottom, a "New thread" button, and per-thread rename/archive controls. The full-page view and the slide-over share the same backend resource and signal state for the active thread.

#### Scenario: Navigate to full page

- **WHEN** a user clicks "Open in full view" from the slide-over
- **THEN** the system navigates to `/chat` with the same thread selected

#### Scenario: Threads grouped by date

- **WHEN** the user has threads with various LastMessageAt times
- **THEN** the rail groups them under Today / Yesterday / This Week / Older sections with empty groups hidden

#### Scenario: Source reference click navigates

- **WHEN** the user clicks a SourceReference chip on an assistant message
- **THEN** the system navigates to the underlying record:
  - `Person` → person detail page
  - `Initiative` → initiative detail (Overview tab)
  - `Capture` → capture detail
  - `Commitment` → commitment detail / list-with-highlight
  - `Delegation` → delegation detail / list-with-highlight
  - `LivingBrief*` → initiative detail (Living Brief tab) anchored at the relevant section
