## Why

Once an initiative has a Living Brief, linked captures, commitments, and delegations, the manager has a rich knowledge base — but interrogating it still requires clicking through screens. The natural way to ask "who's blocked on the API spec?" or "what did we decide about Postgres?" is conversation. This spec adds a multi-turn AI chat panel scoped to a single initiative, with answers grounded in that initiative's data and source-referenced back to the records that informed them.

This is a Tier 2 spec (`initiative-ai-chat`) that depends on `ai-provider-abstraction` (Tier 1) and `initiative-living-brief` (Tier 2). It introduces the `ChatThread` aggregate, which `global-ai-chat` (also Tier 2) will later reuse with a global scope.

## Non-goals

- **No global / cross-initiative chat** — that is `global-ai-chat`. This spec creates the ChatThread aggregate with an Initiative-scoped binding only.
- **No file uploads or image inputs** — text in, text out. Audio capture remains in `capture-audio` (Tier 3).
- **No actions from chat** ("create a commitment for me") — chat is read-only this tier. Action-from-chat is a future concern.
- **No streaming responses to the client** — responses are persisted whole, then returned. SSE streaming is a Tier 3 enhancement.
- **No semantic / vector search over historic data** — context assembly is rule-based (load the initiative's brief + linked records).
- **No shared / multi-user threads** — every thread is single-user-scoped.
- **No fine-tuning, no memory of previous threads** — each thread is independent.

## What Changes

- **New `ChatThread` aggregate root** in the Domain layer, scoped to a User and bound to an Initiative via `ContextScope` (a value object — this spec only implements `ContextScope.Initiative(initiativeId)`; `Global()` is reserved for `global-ai-chat`).
- **New `ChatMessage` value object** embedded on the thread (Role: `User | Assistant | System`, Content, CreatedAt, optional `SourceReferences` list, optional `TokenUsage`, and a `MessageOrdinal` for stable ordering).
- **New `SourceReference` value object** capturing a citation: `EntityType` (Capture | Commitment | Delegation | LivingBriefDecision | LivingBriefRisk | LivingBriefRequirements | LivingBriefDesignDirection | Initiative), `EntityId`, optional `SnippetText`, and an optional `RelevanceScore`.
- **New `InitiativeChatContextBuilder` Application service** that, given an InitiativeId and the user's question, assembles a structured context payload from the LivingBrief, linked Captures' `AiExtraction.Summary`, open Commitments linked to the initiative, and active Delegations linked to the initiative.
- **CQRS handlers and minimal API endpoints** under `/api/initiatives/{id}/chat/threads` for: starting a thread, listing threads, getting a thread (with all messages), posting a user message (which triggers the AI completion synchronously and returns the assistant message), renaming a thread, and archiving a thread.
- **EF Core persistence** for `ChatThread` and embedded messages (one new migration).
- **Angular initiative chat panel** on the initiative detail page — a new "Chat" tab with thread list, active conversation view, message composer, source-reference chips on assistant messages, and a "new thread" action.
- **Domain events** `ChatThreadStarted`, `ChatMessageSent`, `ChatMessageReceived`, `ChatThreadRenamed`, `ChatThreadArchived`.

## Capabilities

### New Capabilities

- `initiative-ai-chat`: Multi-turn AI chat scoped to a single Initiative — assembles context from the initiative's LivingBrief, linked captures, commitments, and delegations; responses cite the records that informed them; thread history persisted.

### Modified Capabilities

_(none — `initiative-living-brief`, `commitment-tracking`, `delegation-tracking`, and `capture-ai-extraction` are read from but not modified. The ChatThread aggregate is new and self-contained.)_

## Impact

- **Domain:** New `ChatThread` aggregate root with embedded `ChatMessage` value objects, `SourceReference` value object, `ContextScope` value object (initiative variant only), `ChatRole` and `ChatThreadStatus` enums.
- **Application:** New `InitiativeChatContextBuilder`, new `ChatCompletionService` orchestrator wrapping `IAiCompletionService`, new vertical-slice handlers under `Initiatives/Chat/`.
- **Infrastructure:** EF Core configuration for `ChatThreads` table with messages serialised as JSONB; one new migration.
- **Web API:** New endpoints under `/api/initiatives/{id}/chat/threads`.
- **Frontend:** New "Chat" tab on the initiative detail page, chat panel components, chat service.
- **AI prompting:** New system prompt for initiative-scoped chat; respects `IAiCompletionService` and `TasteLimitExceededException`.
- **Dependencies:** `ai-provider-abstraction`, `initiative-living-brief`, `initiative-management`, `commitment-tracking`, `delegation-tracking`, `capture-text`, `capture-ai-extraction`.
