## Why

Initiative-scoped chat answers questions about a specific project. But many of the manager's day-to-day questions cut across the whole portfolio — "what's on my plate today?", "how is Jane doing?", "what's overdue?", "what did I capture last week about hiring?". A global chat surface lets the user ask anything from anywhere in the app, with the AI dynamically scoping the relevant context based on the question.

This is a Tier 2 spec (`global-ai-chat`) that depends on `ai-provider-abstraction` (Tier 1) and `initiative-ai-chat` (Tier 2). It reuses the `ChatThread` aggregate introduced by `initiative-ai-chat` by adding the `ContextScope.Global()` variant — no new aggregate.

## Non-goals

- **No new aggregate** — `ChatThread` and `ChatMessage` are reused; only the `Global` ContextScope variant is added.
- **No actions from chat** — read-only ("what's overdue?" yes; "mark commitment X complete" no). Action-from-chat is a future concern.
- **No streaming responses** — synchronous request/response, same as `initiative-ai-chat`.
- **No file uploads or images** — text only.
- **No semantic / vector retrieval** — context selection is rule-based with intent classification. A future spec can swap in semantic retrieval behind `IGlobalChatContextBuilder`.
- **No shared / multi-user threads.**
- **No persistent cross-thread "memory of you"** — each thread is independent; the model only sees the thread's own message history.
- **No specialised fast-path for one-shot questions** — every question goes through a thread (which may contain only one exchange).

## What Changes

- **Extend `ContextScope` value object** with the `Global()` variant (the slot was reserved by `initiative-ai-chat`). A `ChatThread.Start(...)` with `ContextScope.Global()` SHALL succeed and produce a thread with no Initiative binding.
- **New `IntentClassifier` Application service** that, given a user question, classifies the question into one or more `ChatIntent` categories (e.g. `MyDay`, `PersonLens`, `InitiativeStatus`, `OverdueWork`, `CaptureSearch`, `Generic`) and extracts entity hints (person names, initiative names, date ranges). Implementation: a small AI-assisted classifier OR a deterministic rule classifier — the design picks one and explains why.
- **New `GlobalChatContextBuilder` Application service** that, based on the classified intents and entity hints, assembles a bounded context payload from across the user's data: people summaries, initiatives, captures (recent), commitments (open + recent), delegations (active + recent), observations / goals / 1:1s when `people-lens` is present, with strict per-section caps and total token budget.
- **CQRS handlers and minimal API endpoints** under `/api/chat/threads` (note: NOT initiative-scoped) for: starting a global thread, listing global threads, getting a thread, posting a user message (which classifies, builds context, calls the AI, persists), renaming, archiving / unarchiving.
- **Frontend global chat launcher** in the app shell (icon button always visible), opening either a slide-over panel for quick questions or routing to a dedicated full-page chat view for deeper sessions. The full-page view has a thread rail (active threads grouped by date), conversation pane, and composer.
- **Domain events** `GlobalChatThreadStarted` (a typed alias / new event distinct from `ChatThreadStarted` so subscribers can filter), plus reuse of `ChatMessageSent`, `ChatMessageReceived`, `ChatThreadRenamed`, `ChatThreadArchived` from `initiative-ai-chat`.
- **Same EF Core table** as `initiative-ai-chat`'s `ChatThreads`, with `ContextScopeType = "Global"` and `ContextInitiativeId = NULL`. No new migration.

## Capabilities

### New Capabilities

- `global-ai-chat`: Cross-cutting AI chat available everywhere in the app, dynamically scoped by the user's question; queries across all of the user's data (people, initiatives, captures, commitments, delegations, and people-lens records when present); responses cite source records.

### Modified Capabilities

_(none — `initiative-ai-chat` is not modified at the spec level. The `ContextScope.Global()` slot was reserved by that spec; this proposal merely adds requirements for using it. If `initiative-ai-chat`'s spec needs an explicit modification to its `ContextScope` requirement, that will be handled at archive-merge time.)_

## Impact

- **Domain:** `ContextScope.Global()` variant is now exercised. New domain event `GlobalChatThreadStarted`. No new aggregate.
- **Application:** New `IntentClassifier`, new `GlobalChatContextBuilder`, new `GlobalChatCompletionService` (parallel to `InitiativeChatCompletionService`), new vertical-slice handlers under `Chat/Global/`.
- **Infrastructure:** No new migration; reuses the `ChatThreads` table from `initiative-ai-chat`.
- **Web API:** New endpoints under `/api/chat/threads` (top-level, not nested under initiative).
- **Frontend:** New global chat launcher in `AppShell`, slide-over quick-chat panel, full-page chat route, thread rail grouped by date.
- **AI prompting:** New global system prompt; new lightweight intent classification prompt (or rule-based) — picks one in design.
- **Dependencies:** `ai-provider-abstraction`, `initiative-ai-chat` (reuses `ChatThread`/`ChatMessage`/`SourceReference`), `initiative-living-brief`, `initiative-management`, `person-management`, `commitment-tracking`, `delegation-tracking`, `capture-text`, `capture-ai-extraction`. The spec is forward-compatible with `people-lens` (when present, observations/goals/1:1 records become eligible context).
