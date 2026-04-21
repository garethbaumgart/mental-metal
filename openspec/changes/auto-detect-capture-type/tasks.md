## 1. Domain Layer

- [ ] 1.1 Add `CaptureReclassified` domain event record to `Capture` events (Id, oldType, newType)
- [ ] 1.2 Add `Reclassify(CaptureType newType)` method on `Capture` aggregate: guard Processing status, reject AudioRecording, no-op if same type, raise `CaptureReclassified` event
- [ ] 1.3 Add `DetectedCaptureType` (nullable `CaptureType?`) property to `AiExtraction` value object
- [ ] 1.4 Write domain unit tests for `Reclassify`: success cases (QuickNote->Transcript, QuickNote->MeetingNotes), no-op same type, reject AudioRecording, reject non-Processing status (Raw, Processed, Failed)

## 2. Application Layer

- [ ] 2.1 Add `detected_type` field to `ExtractionResponseDto` with `[JsonPropertyName("detected_type")]`
- [ ] 2.2 Update `ExtractionPromptBuilder.SystemPrompt` to instruct the AI to classify content type and include `detected_type` in the JSON schema
- [ ] 2.3 Update `AutoExtractCaptureHandler` to: parse `detected_type` from the DTO, set `DetectedCaptureType` on the `AiExtraction` object, call `capture.Reclassify(detectedType)` when valid and different from current type
- [ ] 2.4 Write unit tests for `AutoExtractCaptureHandler`: reclassification when detected type differs, no reclassification when same type, no reclassification when detected_type is null/unrecognized, DetectedCaptureType stored on AiExtraction

## 3. Infrastructure Layer

- [ ] 3.1 Add EF Core migration for `DetectedCaptureType` column on the AiExtraction owned entity (nullable enum column)
- [ ] 3.2 Update EF Core configuration if needed to map `AiExtraction.DetectedCaptureType`

## 4. Frontend

- [ ] 4.1 Add `detectedCaptureType` field to the capture response model/DTO in the Angular app
- [ ] 4.2 Update capture detail view to display a "Detected as: {type}" indicator when `detectedCaptureType` is present and differs from the capture's original type
- [ ] 4.3 Write frontend unit test for the detected type indicator display logic
