## Why

When AI extraction identifies people mentioned in a capture, unresolved names are silently dropped -- no commitment is created for them, and the user has no visibility into what was missed. This means actionable items tied to people not yet in the system are lost without the user knowing. Surfacing unresolved people and providing inline quick-create enables the user to capture all commitments from a single extraction pass. Closes #173.

## What Changes

- Surface unresolved person names to the user after extraction completes, showing which names could not be matched to existing people
- Add a "quick-create person" inline flow from the capture detail view so users can create people without navigating away
- Add a "link to existing person" option for unresolved names where the AI match was ambiguous
- After resolving previously-unresolved people, automatically spawn the commitments that were skipped during initial extraction
- Frontend derives unresolved person count by filtering `PeopleMentioned` where `PersonId` is null (no separate DTO field needed)

## Non-goals

- Fully automated person creation (always require user confirmation)
- Changing the fuzzy matching algorithm in NameResolutionService
- Bulk person import or CSV upload
- Modifying the AI extraction prompt to improve name resolution
- Making Commitment.PersonId nullable (commitments still require a person)

## Capabilities

### New Capabilities
- `unresolved-people-review`: Surface unresolved people from AI extraction and allow users to quick-create new people or link to existing ones, then spawn skipped commitments

### Modified Capabilities
- `capture-ai-extraction`: Add unresolved people data to extraction results and capture response; add endpoint to resolve unresolved people post-extraction

## Impact

- **Domain:** Capture aggregate gains unresolved-people tracking on AiExtraction value object (already partially there via PersonMention with null PersonId)
- **Application:** AutoExtractCaptureHandler records skipped commitments for later resolution; new handler for resolving unresolved people post-extraction
- **API:** New endpoint `POST /api/captures/{captureId}/resolve-person-mention/quick-create` to create a person and resolve the mention in one step; existing `POST /api/captures/{captureId}/resolve-person-mention` extended to spawn skipped commitments after resolution
- **Frontend:** Capture detail view gains an unresolved-people review panel with quick-create and link-existing actions
- **Aggregates affected:** Capture (extraction metadata), Person (creation), Commitment (deferred spawning)
- **Dependencies:** person-management (create person API), commitment-tracking (create commitment), capture-ai-extraction (extraction pipeline)
