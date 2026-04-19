# Person V2

Strip the Person aggregate to its essential role: a stable identity that the AI resolves transcript mentions against. Everything else about a person — performance signals, relationship dynamics, open commitments — is derived from captures, not manually entered.

## Domain Changes

### Fields Kept
- `Id`: Guid
- `UserId`: Guid (multi-tenancy boundary)
- `CanonicalName`: string (the "real" name, e.g. "Alice Johnson")
- `Type`: PersonType enum (direct-report, peer, stakeholder, external)

### Fields Added
- `Aliases`: List<string> — alternative names the AI and transcription might produce for this person. Stored as JSONB array in PostgreSQL.

### Fields Removed
- `CareerDetails` value object (level, aspirations, growth areas)
- `CandidateDetails` value object (resume URL, source, applied date)
- `PipelineStatus` enum (cv-review, phone-screen, onsite, offer, rejected)
- Any interview-related or goal-related associations

### Invariants
- `CanonicalName` is required and non-empty
- `Aliases` may be empty but never null
- `Aliases` entries are case-insensitive unique within a Person (no duplicate aliases)
- A given alias string must be unique across all People for the same User (no two people share an alias)
- `Type` defaults to `direct-report` if not specified

## API Changes

### Kept Endpoints (simplified)
- `POST /api/people` — Create person (name, type, optional aliases)
- `GET /api/people` — List all people for user
- `GET /api/people/{id}` — Get person detail
- `PUT /api/people/{id}` �� Update canonical name, type
- `POST /api/people/{id}/archive` — Soft delete

### New Endpoints
- `PUT /api/people/{id}/aliases` — Replace the full alias list
- `POST /api/people/{id}/aliases` — Add a single alias (convenience for AI learning)

### Removed Endpoints
- `POST /api/people/{id}/career-details` — removed
- `POST /api/people/{id}/candidate-details` — removed
- `POST /api/people/{id}/advance-pipeline` — removed
- `PUT /api/people/{id}/type` changes from separate endpoint to part of `PUT /api/people/{id}`

## Name Resolution

The core function of Person V2 is to be the target of name resolution during AI extraction.

### Resolution Algorithm

Given a raw name string from a transcript (e.g. "Ali"):

1. Exact match against `CanonicalName` (case-insensitive) → match
2. Exact match against any entry in `Aliases` (case-insensitive) → match
3. Fuzzy match: if the raw name is a substring of `CanonicalName` or vice versa (min 3 chars), and the match is unambiguous (only one Person matches) → match
4. No match → store as "unresolved mention" in extraction

### Alias Learning Flow

When user resolves an unresolved mention:
1. User selects which Person the mention refers to (or creates a new Person)
2. The raw name string is automatically added to that Person's `Aliases`
3. Future extractions resolve that string without user intervention

This is exposed in the People Dossier UI (see `people-dossier` spec) as an "unresolved mentions" section.

## Frontend Changes

### Person List Page
- Simplified: shows name, type, alias count, and **mention count** (number of captures mentioning this person in the last 7 days)
- Remove career details, pipeline status columns
- Add activity indicator (mentions this week)

### Person Detail Page
- Replaced by the People Dossier view (see `people-dossier` spec)
- The detail page becomes the dossier — all information is AI-derived from captures
- Alias management (add/edit/remove aliases) available via inline edit

### Create Person Dialog
- Fields: Name, Type, Aliases (comma-separated or tag input)
- Remove career details and candidate details sections

## Migration

Part of the V2 migration:
- Add `aliases` JSONB column to `people` table (default: empty array `[]`)
- Drop career-detail columns from `people` table
- Drop candidate-detail columns from `people` table
- Drop `pipeline_status` column from `people` table

## Acceptance Criteria

- [ ] Person aggregate has only: Id, UserId, CanonicalName, Aliases, Type
- [ ] Aliases stored as JSONB array, queryable
- [ ] Alias uniqueness enforced per-user (no two people share an alias)
- [ ] Create/update endpoints accept aliases
- [ ] Dedicated alias management endpoints work (PUT replaces, POST adds)
- [ ] Career details, candidate details, and pipeline status fully removed from domain, DTOs, and UI
- [ ] Person list shows mention count from last 7 days
- [ ] Person detail page redirects to / is replaced by People Dossier
- [ ] Name resolution algorithm matches against canonical name and aliases
- [ ] Fuzzy substring matching works with minimum 3-char threshold
- [ ] Unresolved mentions surface in extraction results
- [ ] Resolving an unresolved mention auto-adds the alias
- [ ] Domain tests cover alias invariants (uniqueness, case-insensitivity)
- [ ] Integration tests cover alias CRUD and name resolution
