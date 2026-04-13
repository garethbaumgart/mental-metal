## Context

`initiative-ai-chat` introduced the `ChatThread` aggregate with a `ContextScope` value object designed to support multiple variants. This spec exercises the `Global` variant. The new work is almost entirely in the application layer: intent classification and dynamic context assembly across the whole user data set.

The user-facing entry is an always-visible launcher in the app shell. Two modes are supported by a single underlying flow: a slide-over panel for quick one-off questions and a full-page route for longer sessions. Both create or resume the same `ChatThread` records.

### Dependencies

- `ai-provider-abstraction` (Tier 1) — `IAiCompletionService` and `TasteLimitExceededException`.
- `initiative-ai-chat` (Tier 2) — `ChatThread` aggregate, `ChatMessage`/`SourceReference`/`ContextScope` value objects, repository, base events. This spec depends on that aggregate existing; it does not redefine it.
- `person-management`, `initiative-management`, `commitment-tracking`, `delegation-tracking`, `capture-text`, `capture-ai-extraction`, `initiative-living-brief` — read-only context sources.
- Forward-compatible with `people-lens` (when present, additional context).

## Goals / Non-Goals

**Goals:**

- Reuse `ChatThread` as-is. No fork, no parallel aggregate.
- Make global chat answer the most common question types correctly: "what's on my plate?", "how is <person> doing?", "status of <initiative>?", "what's overdue?", "what did I capture about <topic>?".
- Keep context bounded. The total payload to the AI MUST fit comfortably in a typical 100k-token window with room to spare for response.
- Cite every factual claim with a `SourceReference` typed correctly (Person, Initiative, Capture, Commitment, Delegation, etc.).
- Multi-tenant isolation enforced at every context query.

**Non-Goals:**

- Tool-use / actions from chat.
- Streaming responses.
- Vector / semantic search.
- Persistent cross-thread memory.
- A "personality" or persona — the assistant is utilitarian.

## Decisions

### 1. Reuse `ChatThread` via `ContextScope.Global()`

**Decision:** Add no new aggregate. `ChatThread.Start(userId, ContextScope.Global())` produces a global thread. The `ContextScope.Global()` variant carries no fields. Repository queries filter by `ContextScopeType` and (for initiative scope) `ContextInitiativeId`.

**Rationale:** Threads behave identically; only context assembly differs. This was the explicit design intent of `initiative-ai-chat`.

**Consequence:** A single `ChatThreads` table holds both initiative and global threads, distinguished by `ContextScopeType`. Listings filter accordingly.

### 2. Distinct `GlobalChatThreadStarted` event

**Decision:** Raise `GlobalChatThreadStarted` (in addition to or instead of the base `ChatThreadStarted`). Implementation choice: raise both — `ChatThreadStarted` for generic subscribers, `GlobalChatThreadStarted` for global-specific subscribers (e.g. usage analytics, future "trending questions" features).

**Rationale:** Lets subscribers filter without inspecting the ContextScope payload. Cheap to add now.

**Alternatives considered:**
- Single event with scope inside payload — rejected because filtering inside a handler is ugly and event-bus consumers prefer typed channels.

### 3. Intent classification: hybrid (rule-based first, AI fallback)

**Decision:** A two-layer `IntentClassifier`:

1. **Rule layer (cheap, deterministic):** regex/keyword matches against the user question for high-precision intents:
   - `OverdueWork`: matches "overdue", "late", "behind".
   - `MyDay` / `MyWeek`: matches "today", "this week", "what's on my plate".
   - `PersonLens`: matches a known person name (substring/full-name match against the user's people).
   - `InitiativeStatus`: matches a known initiative name.
   - `CaptureSearch`: matches "I captured", "what did I write about", date phrases.
2. **AI layer (fallback):** if no rule fires (or only `Generic` matches), a single small AI call classifies into the same intent set with a structured response. Cached per-thread for similar follow-up questions.

The classifier returns `IntentSet` = `{ intents: ChatIntent[], entityHints: { personIds[], initiativeIds[], dateRange? } }`. Entity hints are resolved by the rule layer (matching names against the user's known people/initiatives) so the context builder can pull targeted records.

**Rationale:** Most expected questions are rule-classifiable cheaply. AI fallback handles long-tail. This balances cost, latency, and coverage.

**Trade-off:** The rule layer is brittle in a multilingual sense (English-first). Documented as a limitation.

**Alternatives considered:**
- AI classifier only — rejected for cost/latency on every message.
- Rule-only — rejected because long-tail "fuzzy" questions degrade to `Generic` (everything in context, capped).

### 4. `GlobalChatContextBuilder` — intent-driven context with hard caps

**Decision:** Given an `IntentSet`, the builder pulls a per-intent context slice:

- `MyDay` / `MyWeek`: open commitments due today/this week (mine-to-them and theirs-to-me, max 30); active delegations due today/this week (max 30); 1:1s scheduled today/this week (when `people-lens` is present).
- `OverdueWork`: overdue commitments (max 30) and overdue delegations (max 30).
- `PersonLens(personIds)`: for each person (max 5), pull person summary, open commitments + delegations + observations + goals + most-recent 1:1 (when `people-lens` is present).
- `InitiativeStatus(initiativeIds)`: for each initiative (max 5), pull initiative metadata + LivingBrief summary + open risks + recent decisions (max 10) + open commitments/delegations linked to it (max 20 each).
- `CaptureSearch(dateRange?)`: most-recent 30 confirmed captures (within date range if specified), each as { id, createdAt, summary, linkedPersonNames, linkedInitiativeNames }.
- `Generic`: a "lightweight everything" — counts (open commitments, open delegations, active initiatives), top 5 most-overdue commitments, top 5 most-recent captures.

Multiple intents stack additively up to a global section-count cap; total payload is hard-capped at a configurable token budget (default ~16k input tokens of context, leaving margin for system prompt and response). When the cap would be exceeded, the builder degrades gracefully: trims the lowest-priority sections first (recent captures → delegations → commitments → person/initiative cores).

All queries are filtered by UserId.

**Rationale:** Predictable cost, predictable behaviour, and graceful degradation at the upper bound.

### 5. AI prompting strategy

**Decision:** Same structured envelope as `initiative-ai-chat`: `{ assistantText: string, sourceReferences: SourceReference[] }`. The system prompt is global-flavoured: "You are the user's work assistant. Answer using only the supplied context. Cite every factual claim. If the question cannot be answered from the context, say so and suggest what to capture or add." Conversation history (recent N messages, capped by token estimate) is included.

**Rationale:** Consistent with initiative chat. Same parser, same fallback ("if envelope unparseable, persist raw text").

### 6. `SourceReference` types — extend in `initiative-ai-chat`'s enum?

**Decision:** This spec uses the same `SourceReferenceEntityType` enum. The enum already includes `Capture`, `Commitment`, `Delegation`, `Initiative`, and the LivingBrief variants. We additionally need `Person` (for "how is Jane doing?" answers). The enum SHALL be extended to include `Person` (and forward-compatible `Observation`, `Goal`, `OneOnOne` for `people-lens`).

This is a small modification to a value object owned by `initiative-ai-chat`. We declare it as part of this spec's `ADDED Requirements` (the `SourceReferenceEntityType` enum SHALL include these additional values). The proposal section noted no Modified Capabilities; if reviewers feel the enum extension warrants a delta on `initiative-ai-chat`'s spec, we will add it at archive-merge time. The pragmatic call: enum extensions are additive and harmless.

**Rationale:** Avoid a parallel enum and inconsistent typing across initiative and global chat.

### 7. API surface

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/chat/threads` | POST | Start a new global thread |
| `/api/chat/threads` | GET | List the user's global threads (filterable by status, ordered by LastMessageAt) |
| `/api/chat/threads/{threadId}` | GET | Get the full thread including messages |
| `/api/chat/threads/{threadId}` | PUT | Rename |
| `/api/chat/threads/{threadId}/messages` | POST | Post a user message; returns the assistant reply |
| `/api/chat/threads/{threadId}/archive` | POST | Archive |
| `/api/chat/threads/{threadId}/unarchive` | POST | Unarchive |

**Rationale:** Top-level `/api/chat` namespace; mirrors the initiative-scoped surface but unbound from any initiative.

### 8. Frontend: launcher + slide-over + full page

**Decision:** Add a global chat launcher (PrimeNG button with a sparkle icon) in the app shell, always visible. Click opens a slide-over panel (PrimeNG Sidebar from the right) with the most-recent active thread preselected (or an empty new thread if none). The slide-over has the composer and message list, plus an "Open in full view" link that navigates to `/chat` — a full-page route with a thread rail (grouped by date: Today, Yesterday, This Week, Older), main conversation pane, composer, and rename/archive controls.

**Rationale:** Quick-question users get the slide-over; deeper sessions get the full page. Both share the same backend resource.

### 9. Multi-tenant isolation, restated

**Decision:** Every read in `GlobalChatContextBuilder` is filtered by the thread's `UserId`. Repositories enforce UserId at the persistence layer. Integration tests cover each intent path with a two-user fixture.

### 10. Reuse of `InitiativeChatCompletionService`?

**Decision:** Do **not** reuse. Create a separate `GlobalChatCompletionService` with a different system prompt and a different context builder. Both services share the structured-envelope parser via a small shared utility (`ChatResponseParser`).

**Rationale:** Same shape, different policies. Sharing the orchestrator would require flag-based branching that obscures the difference. Sharing the parser is appropriate and small.

## Risks / Trade-offs

- **[Intent misclassification]** A misclassified intent yields irrelevant context and a disappointing answer. **Mitigation:** Hybrid classifier (Decision 3); `Generic` fallback always provides a baseline; the user can rephrase. Telemetry on classification confidence to tune rules.
- **[Cost drift]** Global chat is enticing — users may use it heavily. Each turn is an AI call (plus an optional classifier call). **Mitigation:** Existing `TasteLimitExceededException` handling per `ai-provider-abstraction`; classifier cached per thread for follow-ups.
- **[Context window pressure]** "Tell me everything" questions could blow the budget. **Mitigation:** Hard caps + graceful degradation in the context builder (Decision 4).
- **[Stale data]** Context is built at request time, so it's fresh — but the assistant's reply may be quoted/screenshotted later and become stale. **Mitigation:** Each assistant message stores `CreatedAt`; SourceReference chips remain clickable; not solvable in chat itself.
- **[Hallucinated citations]** Same risk as `initiative-ai-chat`. **Mitigation:** Same defence — drop SourceReference EntityIds not present in the assembled context before persisting.
- **[Person-name ambiguity]** Two people named "Sarah" → entity hints become ambiguous. **Mitigation:** Hint-resolver returns ALL matches; context builder includes both, the assistant disambiguates in its reply ("Did you mean Sarah Chen or Sarah Patel?").
- **[Multi-language gaps]** Rule-based intent layer is English-first. **Mitigation:** AI fallback covers other languages, accepting the latency cost.

## Migration Plan

- No new EF Core migration. Reuses `ChatThreads` from `initiative-ai-chat`.
- The `SourceReferenceEntityType` enum gains `Person` (and forward `Observation`, `Goal`, `OneOnOne`); these are additive and require no migration.
- Frontend ships behind a feature flag `globalChatEnabled` (per-user preference, default `true`) so rollout can be paused if AI cost spikes.

## Open Questions

_(none — intent classifier is hybrid; enum extension declared additive; reuse boundary explicit.)_
