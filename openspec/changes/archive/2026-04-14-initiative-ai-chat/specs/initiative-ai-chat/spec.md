## ADDED Requirements

### Requirement: ChatThread aggregate

The system SHALL define a `ChatThread` aggregate root with: `Id` (Guid), `UserId` (Guid, required), `ContextScope` (value object — for this spec, only `ContextScope.Initiative(InitiativeId)` is supported), `Title` (string, may be empty), `Status` (`Active | Archived`), `CreatedAt`, `LastMessageAt` (nullable until first message), `MessageCount` (int, denormalised), and `Messages` (ordered list of `ChatMessage` value objects). All ChatThreads SHALL be scoped to a single user via UserId.

#### Scenario: Aggregate invariants

- **WHEN** a ChatThread is created
- **THEN** Status is Active, MessageCount is 0, Messages is empty, LastMessageAt is null, ContextScope is set, and UserId is set

#### Scenario: User isolation

- **WHEN** User A creates a chat thread on Initiative X and User B has the same Initiative ID (impossible by design, but enforced)
- **THEN** User A's threads never appear in any of User B's queries, including listings, get-by-id, and message posts (HTTP 404 for User B)

### Requirement: ChatMessage value object

Each `ChatMessage` SHALL contain: `MessageOrdinal` (int, monotonically increasing within a thread, starting at 1), `Role` (`User | Assistant | System`), `Content` (string, may be empty for system messages with metadata-only payloads), `CreatedAt` (datetime), optional `SourceReferences` (only valid when Role is Assistant), optional `TokenUsage` (`{ promptTokens, completionTokens }`).

#### Scenario: Ordinal contiguity

- **WHEN** messages are appended to a thread in order
- **THEN** the MessageOrdinal of the Nth message is N (no gaps)

#### Scenario: SourceReferences only on assistant messages

- **WHEN** a User or System message is appended
- **THEN** SourceReferences MUST be empty

### Requirement: SourceReference value object

A `SourceReference` SHALL contain: `EntityType` (one of `Capture`, `Commitment`, `Delegation`, `LivingBriefDecision`, `LivingBriefRisk`, `LivingBriefRequirements`, `LivingBriefDesignDirection`, `Initiative`), `EntityId` (Guid), optional `SnippetText`, and optional `RelevanceScore` (decimal 0.0-1.0).

#### Scenario: Validate entity type

- **WHEN** a SourceReference is constructed with an unrecognised EntityType
- **THEN** construction throws a domain validation error

### Requirement: Start a new initiative chat thread

The system SHALL allow an authenticated user to start a new chat thread for one of their initiatives via `POST /api/initiatives/{id}/chat/threads`. The system SHALL create a ChatThread with `ContextScope.Initiative(initiativeId)`, empty Title, Status `Active`, and raise a `ChatThreadStarted` domain event.

#### Scenario: Start a thread

- **WHEN** an authenticated user sends `POST /api/initiatives/{id}/chat/threads` for an initiative they own
- **THEN** the system creates a new ChatThread, returns HTTP 201 with the thread (id, contextScope, title, status, createdAt, empty messages array)

#### Scenario: Initiative not found

- **WHEN** the InitiativeId does not exist or belongs to another user
- **THEN** the system returns HTTP 404 and no thread is created

### Requirement: List initiative chat threads

The system SHALL allow an authenticated user to list chat threads for one of their initiatives via `GET /api/initiatives/{id}/chat/threads`, optionally filtered by `status` (defaulting to `Active`). Threads SHALL be ordered by `LastMessageAt DESC` (nulls last, by `CreatedAt DESC`).

#### Scenario: List active threads

- **WHEN** an authenticated user sends `GET /api/initiatives/{id}/chat/threads`
- **THEN** the system returns active threads belonging to the user for that initiative, newest first, each thread summary includes id, title, status, createdAt, lastMessageAt, and messageCount

#### Scenario: List archived threads

- **WHEN** an authenticated user sends `GET /api/initiatives/{id}/chat/threads?status=Archived`
- **THEN** only archived threads are returned

#### Scenario: Empty list

- **WHEN** the user has no threads on the initiative
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Get a chat thread with messages

The system SHALL allow an authenticated user to fetch a thread including all messages via `GET /api/initiatives/{id}/chat/threads/{threadId}`.

#### Scenario: Get thread

- **WHEN** an authenticated user requests an existing thread
- **THEN** the response includes thread metadata and all messages in MessageOrdinal order, each message with role, content, createdAt, sourceReferences (for assistant messages), and tokenUsage when available

#### Scenario: Thread not found or wrong user

- **WHEN** the threadId does not exist, belongs to another user, or belongs to a different initiative than the URL specifies
- **THEN** the system returns HTTP 404

### Requirement: Post a user message and receive an assistant reply

The system SHALL allow an authenticated user to post a user message via `POST /api/initiatives/{id}/chat/threads/{threadId}/messages` with body `{ "content": "<text>" }`. The system SHALL: append the user message (Role User, next MessageOrdinal, raise `ChatMessageSent`), call `InitiativeChatContextBuilder` to assemble context, call `IAiCompletionService.CompleteAsync` with the system prompt + assembled context + recent conversation history + the user message, parse the assistant reply (text + source references), append it as a single Assistant message (raise `ChatMessageReceived`), update `LastMessageAt` and `MessageCount` (incremented by 2), persist the thread, and return HTTP 200 with `{ userMessage, assistantMessage }`.

#### Scenario: Post a question and receive a grounded answer

- **WHEN** an authenticated user posts "What did we decide about Postgres?" to a thread on an initiative whose Living Brief contains a decision "Adopt PostgreSQL"
- **THEN** the system appends the user message, generates an assistant reply that references the LivingBriefDecision SourceReference (EntityType LivingBriefDecision, EntityId of that decision), persists both messages, and returns HTTP 200

#### Scenario: Empty content rejected

- **WHEN** the user posts a message with empty or whitespace-only content
- **THEN** the system returns HTTP 400 and no messages are appended

#### Scenario: Thread is archived

- **WHEN** the user posts to an Archived thread
- **THEN** the system returns HTTP 409 Conflict

#### Scenario: AI provider failure

- **WHEN** the AI completion call throws an `AiProviderException`
- **THEN** the user message is still appended, an Assistant or System message containing a friendly error ("AI service unavailable, please retry") is appended in its place, and the system returns HTTP 200 with both messages

#### Scenario: Taste limit exceeded

- **WHEN** the AI completion call throws a `TasteLimitExceededException`
- **THEN** the user message is appended, a System message with content "Daily AI limit reached" is appended, and the system returns HTTP 200

#### Scenario: Invalid citations dropped

- **WHEN** the AI returns a SourceReference whose EntityId is not present in the assembled context payload
- **THEN** that SourceReference is dropped before persisting and the assistant message contains only validated references

#### Scenario: Malformed AI envelope falls back gracefully

- **WHEN** the AI response cannot be parsed into the structured envelope
- **THEN** the entire raw response text is persisted as the assistant message content with empty SourceReferences

### Requirement: Context assembly is bounded and user-scoped

The `InitiativeChatContextBuilder` SHALL produce a context payload from data belonging to the same user as the thread, capped as follows: at most the 20 most-recent KeyDecisions, all Open risks, the latest RequirementsSnapshot, the latest DesignDirectionSnapshot; up to 50 most-recent Commitments linked to the initiative (open + recently completed within 30 days); up to 50 most-recent Delegations linked to the initiative (active + recently completed within 30 days); the 30 most-recent confirmed Captures linked to the initiative (using each capture's `AiExtraction.Summary`).

#### Scenario: Context respects caps

- **WHEN** an initiative has 100 commitments and 100 captures
- **THEN** the assembled context contains at most 50 commitments and 30 capture summaries

#### Scenario: Context never crosses users

- **WHEN** building context for a thread belonging to User A
- **THEN** every commitment, delegation, capture, and brief field included is owned by User A; no User B records are read

#### Scenario: Empty initiative

- **WHEN** the initiative has no captures, commitments, delegations, or brief content
- **THEN** the assembled context contains only initiative metadata and the AI is invoked with an essentially empty knowledge base

### Requirement: Auto-generated thread title

When the first user message is appended to a thread with empty Title, the system SHALL set the Title to the user-message content truncated to 80 characters (with ellipsis if truncated). The system SHALL raise `ChatThreadRenamed` with `source = "AutoFromFirstMessage"`.

#### Scenario: Auto-title from first message

- **WHEN** a user posts the first message "What is blocking the API spec delivery?" to a new thread
- **THEN** the thread's Title becomes "What is blocking the API spec delivery?"

#### Scenario: Long first message truncated

- **WHEN** the first message is 200 characters long
- **THEN** the Title is the first 80 characters followed by an ellipsis

### Requirement: Rename a thread

The system SHALL allow an authenticated user to rename a thread via `PUT /api/initiatives/{id}/chat/threads/{threadId}` with body `{ "title": "..." }`. Title SHALL be at most 200 characters and not whitespace-only. The system SHALL raise `ChatThreadRenamed` with `source = "Manual"`.

#### Scenario: Rename a thread

- **WHEN** an authenticated user renames a thread to "Q3 Planning Discussion"
- **THEN** the Title is updated and the system returns HTTP 200

#### Scenario: Empty title rejected

- **WHEN** the title is empty or whitespace
- **THEN** the system returns HTTP 400

### Requirement: Archive and unarchive a thread

The system SHALL allow an authenticated user to archive an Active thread via `POST /api/initiatives/{id}/chat/threads/{threadId}/archive` (raise `ChatThreadArchived`) and to unarchive an Archived thread via `POST /api/initiatives/{id}/chat/threads/{threadId}/unarchive` (Status returns to Active).

#### Scenario: Archive an active thread

- **WHEN** an authenticated user archives an Active thread
- **THEN** Status becomes Archived and the system returns HTTP 200

#### Scenario: Unarchive an archived thread

- **WHEN** an authenticated user unarchives an Archived thread
- **THEN** Status becomes Active and the system returns HTTP 200

#### Scenario: Archive twice rejected

- **WHEN** the user attempts to archive an already-Archived thread (or unarchive an Active thread)
- **THEN** the system returns HTTP 409

### Requirement: Initiative chat tab in UI

The frontend SHALL provide a "Chat" tab on the initiative detail page. The tab SHALL display: a left rail listing active threads (with a collapsed Archived section), a main panel showing the selected thread's messages in order, a composer at the bottom for posting a new user message, a "New thread" action at the top of the rail, and rename/archive controls on each thread. Assistant messages SHALL render `SourceReference` chips that, when clicked, navigate to the underlying record (capture detail, commitment detail, delegation detail, or scroll to the corresponding decision/risk/snapshot in the Living Brief tab). Component state SHALL use Angular signals.

#### Scenario: Send a message and see a reply

- **WHEN** a user types a question into the composer and submits
- **THEN** the user message appears immediately, a loading indicator shows while the AI responds, and the assistant message renders with source-reference chips

#### Scenario: Click a source reference

- **WHEN** a user clicks a SourceReference chip on an assistant message of type "Capture"
- **THEN** the system navigates to that capture's detail page

#### Scenario: Switch threads

- **WHEN** a user clicks a different thread in the left rail
- **THEN** the main panel loads that thread's messages

#### Scenario: New thread

- **WHEN** a user clicks "New thread"
- **THEN** an empty thread is created and selected; the composer is focused
