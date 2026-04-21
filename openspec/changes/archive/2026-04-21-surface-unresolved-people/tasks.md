## 1. Domain: Add PersonRawName to ExtractedCommitment

- [ ] 1.1 Add `PersonRawName` (string?) property to `ExtractedCommitment` record in `AiExtraction.cs`
- [ ] 1.2 Add unit tests verifying `ExtractedCommitment` stores and exposes `PersonRawName`

## 2. Application: Populate PersonRawName during extraction

- [ ] 2.1 Update `AutoExtractCaptureHandler` to set `PersonRawName` on each `ExtractedCommitment` from the AI response's `PersonRawName` field
- [ ] 2.2 Update `ExtractedCommitmentResponse` DTO in `CaptureDtos.cs` to include `PersonRawName`
- [ ] 2.3 Add/update unit tests for `AutoExtractCaptureHandler` verifying `PersonRawName` is populated on extracted commitments

## 3. Application: Extend ResolvePersonMentionHandler to spawn skipped commitments

- [ ] 3.1 Update `ResolvePersonMentionHandler` to find extracted commitments matching the resolved raw name (via `PersonRawName`) with High/Medium confidence and null `SpawnedCommitmentId`
- [ ] 3.2 Spawn Commitment entities for matching extracted commitments and update extraction with `SpawnedCommitmentId` and `PersonId`
- [ ] 3.3 Record spawned commitments on the capture via `RecordSpawnedCommitment`
- [ ] 3.4 Add unit tests for commitment spawning on person resolution (resolved with commitments, resolved without commitments, already-spawned not duplicated, Low confidence not spawned)

## 4. Application: QuickCreateAndResolveHandler

- [ ] 4.1 Create `QuickCreateAndResolveHandler` with request record (`RawName`, `PersonName`, `PersonType`) in `Captures/AutoExtract/`
- [ ] 4.2 Implement: create Person, add raw name as alias (if different from person name), delegate to resolve-mention logic (update extraction, link capture, spawn commitments)
- [ ] 4.3 Handle duplicate person name by returning conflict error
- [ ] 4.4 Add unit tests for quick-create flow (happy path, duplicate name conflict, same name as raw name skips alias, no extraction error)

## 5. Web: Register API endpoint

- [ ] 5.1 Add minimal API endpoint `POST /api/captures/{captureId}/resolve-person-mention/quick-create` mapped to `QuickCreateAndResolveHandler`
- [ ] 5.2 Add unit/integration test for the new endpoint (200 on success, 409 on duplicate, 400 on missing extraction)

## 6. Frontend: Unresolved people banner component

- [ ] 6.1 Create `UnresolvedPeopleBannerComponent` (standalone, signals) in `src/app/pages/captures/capture-detail/`
- [ ] 6.2 Implement banner displaying count and list of unresolved person mentions (filter `PeopleMentioned` where `PersonId` is null)
- [ ] 6.3 Add "Link to Existing" button per unresolved name with PrimeNG AutoComplete/Dropdown for person search
- [ ] 6.4 Add "Quick Create" button per unresolved name that opens quick-create dialog

## 7. Frontend: Quick-create person dialog

- [ ] 7.1 Create `QuickCreatePersonDialogComponent` (standalone, signals) using PrimeNG Dialog
- [ ] 7.2 Implement form with pre-filled name input and person type dropdown (defaulting to Stakeholder)
- [ ] 7.3 Wire dialog confirmation to call the quick-create-and-resolve API endpoint
- [ ] 7.4 Handle success (refresh capture data, close dialog) and error (show conflict message suggesting link-to-existing)

## 8. Frontend: Wire into capture detail view

- [ ] 8.1 Add `UnresolvedPeopleBannerComponent` to capture detail view template, shown when extraction has unresolved mentions
- [ ] 8.2 Wire "Link to Existing" flow to call existing resolve-person-mention API and refresh capture data
- [ ] 8.3 Add capture service method for the quick-create-and-resolve endpoint
- [ ] 8.4 Verify banner disappears when all mentions are resolved

## 9. Testing

- [ ] 9.1 Add backend integration tests for the full resolve-and-spawn flow (link-to-existing path)
- [ ] 9.2 Add backend integration tests for the full quick-create-and-resolve flow
- [ ] 9.3 Add frontend component tests for `UnresolvedPeopleBannerComponent` (shows/hides, button actions)
- [ ] 9.4 Add frontend component tests for `QuickCreatePersonDialogComponent` (pre-fill, submit, error handling)
