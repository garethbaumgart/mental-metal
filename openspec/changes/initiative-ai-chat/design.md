## Context

The Living Brief turns captures into structured initiative knowledge. Chat is the natural reading interface on top of that knowledge. This spec introduces the `ChatThread` aggregate that will be reused by `global-ai-chat`. The shape of `ChatThread`, `ChatMessage`, and `SourceReference` matters because `global-ai-chat` should not require a different aggregate.

### Dependencies

- `ai-provider-abstraction` (Tier 1) — `IAiCompletionService.CompleteAsync` and `TasteLimitExceededException`.
- `initiative-living-brief` (Tier 2) — the brief is the primary context source for chat.
- `initiative-management` (Tier 1) — InitiativeId is the binding.
- `commitment-tracking` and `delegation-tracking` (Tier 2) — read-only context sources.
- `capture-text` and `capture-ai-extraction` (Tier 2) — capture summaries are read for context.

## Goals / Non-Goals

**Goals:**

- Design `ChatThread` once such that `global-ai-chat` can adopt it by adding a new `ContextScope` variant — no fork, no aggregate split.
- Persist every assistant response with its `SourceReference` list so the user can click through to the underlying record.
- Keep AI calls synchronous from the user's perspective (request/response per message) but bounded — large initiatives must not blow the context window.
- Honour multi-tenant isolation strictly: a thread is invisible to another user, and context assembly never mixes users.
- Treat thread state (messages, ordinal, status) as part of the aggregate; treat per-call AI cost concerns (token budgets, retries) as application-layer concerns.

**Non-Goals:**

- Real-time streaming responses to the client.
- Multi-user / shared threads.
- Vector / semantic search.
- Tool-use / function-calling from chat (no "create commitment" actions).
- Cross-thread or cross-initiative memory.

## Decisions

### 1. ChatThread is a single aggregate root reused for both initiative and global chat

**Decision:** `ChatThread` has a `ContextScope` value object discriminating by variant. This spec implements `ContextScope.Initiative(InitiativeId)`. `global-ai-chat` will later add `ContextScope.Global()` without any structural change. The aggregate has a UserId (required), Title (auto-generated from first user message; user-renameable), CreatedAt, LastMessageAt, Status (`Active | Archived`), and an ordered list of embedded `ChatMessage` value objects.

**Rationale:** Threads behave the same way regardless of scope; only context assembly differs. One aggregate avoids duplication and keeps the chat UI consistent.

**Alternatives considered:**
- Two aggregates (`InitiativeChatThread`, `GlobalChatThread`) — rejected for duplication.
- One aggregate with a nullable `InitiativeId` — rejected because an explicit `ContextScope` is more expressive and future-proof for additional scopes.

### 2. Messages embedded on the thread; ordinal for stable order

**Decision:** `ChatMessage` is a value object embedded in `ChatThread.Messages`. Each message carries `MessageOrdinal` (monotonic int starting at 1), `Role`, `Content`, `CreatedAt`, optional `SourceReferences` (only on assistant messages), and optional `TokenUsage`. Persisted as JSONB.

**Rationale:** A thread without its messages is meaningless — embedding preserves aggregate invariants (e.g. ordinal is contiguous, roles alternate U/A after the optional system message). JSONB matches the storage pattern from `LivingBrief`.

**Trade-off:** Very long threads grow the aggregate row. **Mitigation:** Threads can be archived; new threads are cheap. A future spec can add server-side message pagination if real users hit the row-size ceiling.

### 3. SourceReference is a structured citation, not a pointer-with-text

**Decision:** `SourceReference` carries `EntityType` (one of `Capture | Commitment | Delegation | LivingBriefDecision | LivingBriefRisk | LivingBriefRequirements | LivingBriefDesignDirection | Initiative`), `EntityId` (Guid), optional `SnippetText` (the bit of context the AI quoted), and optional `RelevanceScore`. Frontend resolves the link target based on EntityType.

**Rationale:** Structured citations let the UI render typed chips ("[D] Decision: Adopt PostgreSQL") and route clicks. Free-text "as the brief says..." is unverifiable.

### 4. Context assembly is rule-based and bounded

**Decision:** `InitiativeChatContextBuilder.Build(userId, initiativeId, userQuestion, recentMessages)` returns a structured payload. `userId` is required and non-optional — every downstream query filters by it, and the handler must pass the authenticated user's ID even though the initiative is already user-scoped (defence in depth against future refactors that might weaken that invariant).

- `initiativeMetadata`: { id, name, status, milestones }
- `livingBrief`: { summary, recentDecisions[20], openRisks, latestRequirements, latestDesignDirection }
- `commitments`: open and recently-completed (within 30 days), max 50 items, projected to { id, description, direction, personName, status, dueDate }
- `delegations`: active (Assigned/InProgress/Blocked) plus recently-completed (within 30 days), max 50 items, projected to { id, description, delegateName, status, dueDate, blockedReason }
- `linkedCaptures`: most recent 30 confirmed captures' { id, createdAt, summary } from `AiExtraction.Summary`

Hard caps protect the context window. Items are ordered by recency. The `userQuestion` itself is NOT used for filtering in this spec — context is rule-based, not retrieval-based.

**Rationale:** Predictable cost and behaviour. Retrieval (vector search, hybrid search) is a known evolution, called out in non-goals; this design leaves room for it (a future `IChatContextBuilder` could be swapped in).

**Alternatives considered:**
- Embedding-based retrieval — deferred; adds a vector DB dependency and a re-indexing pipeline. Rule-based covers most "what did we decide?" / "who's blocked?" questions on a single initiative.

### 5. AI prompting strategy

**Decision:** A system prompt instructs the model to: answer using only the provided context; cite every factual claim with a `SourceReference` JSON entry; respond with a structured envelope `{ assistantText: string, sourceReferences: SourceReference[] }`; refuse out-of-scope questions politely. The user message is the natural-language question. Conversation history (last N messages, capped by token budget) is included as prior turns.

**Rationale:** Forcing a structured response keeps citations tractable. Refusal on out-of-scope keeps responses honest.

**Trade-off:** Models that don't reliably emit JSON degrade gracefully — the parser falls back to "assistant text only, no citations" if the envelope can't be parsed.

### 6. Synchronous request/response

**Decision:** `POST /chat/threads/{threadId}/messages` blocks until the AI completes and returns both the user message and the assistant message in one response. No streaming SSE in this spec.

**Rationale:** Simpler client; matches the existing capture-AI flow. Streaming is an enhancement.

### 7. Title auto-generation

**Decision:** When a thread is created, its title is empty. After the first user message, the application layer derives a title (truncated user message text, max 80 chars; the AI is not used for titling in this spec). The user can rename via `PUT /chat/threads/{threadId}` with `{ "title": "..." }`.

**Rationale:** Avoids an extra AI call. Trivial but useful for thread navigation.

### 8. API surface

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/initiatives/{id}/chat/threads` | POST | Start a new thread (returns thread with empty messages) |
| `/api/initiatives/{id}/chat/threads` | GET | List active threads for the initiative (filter by status) |
| `/api/initiatives/{id}/chat/threads/{threadId}` | GET | Get full thread including all messages |
| `/api/initiatives/{id}/chat/threads/{threadId}` | PUT | Rename thread |
| `/api/initiatives/{id}/chat/threads/{threadId}/messages` | POST | Post a user message and return the assistant reply |
| `/api/initiatives/{id}/chat/threads/{threadId}/archive` | POST | Archive the thread (soft) |
| `/api/initiatives/{id}/chat/threads/{threadId}/unarchive` | POST | Restore archived thread |

**Rationale:** Threads as a sub-resource of an initiative; messages as a sub-collection of a thread.

### 9. Frontend: chat tab on initiative detail

**Decision:** A new "Chat" tab next to "Living Brief" on the initiative detail page. Layout: left rail of threads (active by default; archived expandable), main panel showing the active thread (scrollable message list), composer at the bottom. Assistant messages render `SourceReference` chips that route to the source record (capture detail, commitment detail, etc.) when clicked. New-thread button at the top of the rail. All state via Angular signals; PrimeNG components first.

**Rationale:** Co-locates chat with the initiative the user is reading about; familiar two-pane chat layout.

### 10. Storage shape

**Decision:** Single `ChatThreads` table:

- `Id` (PK), `UserId`, `ContextScopeType` (string discriminator), `ContextInitiativeId` (nullable Guid), `Title`, `Status`, `CreatedAt`, `LastMessageAt`, `Messages` (JSONB), `MessageCount` (denormalised int for cheap listing).
- Indexes: `(UserId, ContextScopeType, ContextInitiativeId, Status, LastMessageAt DESC)`.

**Rationale:** One table now, ready for `global-ai-chat` (which will use `ContextScopeType = "Global"` with null `ContextInitiativeId`). Denormalised `MessageCount` keeps thread-list queries cheap.

## Risks / Trade-offs

- **[Context window overflow]** Very large initiatives could exceed the model's input budget. **Mitigation:** Hard caps in the context builder (50 commitments, 50 delegations, 30 captures); recent-message window for conversation history.
- **[Hallucinated citations]** The model could cite EntityIds that aren't in context. **Mitigation:** The parser validates that each `SourceReference.EntityId` appeared in the assembled context; unknown IDs are dropped before persisting.
- **[Multi-tenant context leak]** If context assembly accidentally pulled cross-user data, it would leak via the AI. **Mitigation:** Every context query is filtered by UserId; integration tests cover the isolation boundary.
- **[Thread row growth]** Long threads grow the JSONB column. **Mitigation:** Acceptable at Tier 2 scale; archiving is provided. Pagination/sharding deferred.
- **[Fragile JSON parsing]** Models occasionally don't emit clean JSON. **Mitigation:** Best-effort parse with fallback to "no citations"; persist the raw assistant text either way so the user always sees a reply.
- **[AI cost per turn]** Each message is a paid AI call. **Mitigation:** `TasteLimitExceededException` handled by returning a friendly assistant message ("Daily AI limit reached") that is persisted as a system message with no token usage; no thread corruption.

## Migration Plan

- One EF Core migration adds the `ChatThreads` table. No data migration; threads start empty.
- No breaking changes to existing aggregates.

## Open Questions

_(none — defaults documented: synchronous responses, rule-based context, manual title generation.)_
