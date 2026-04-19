# Quick Note Voice

Add voice-to-text capture to the existing Quick Capture dialog. The user taps a mic button, speaks their thought, and a transcript is saved as a capture — processed by the same extraction pipeline as meeting transcripts.

## User Flow

1. User triggers Quick Capture (FAB button, sidebar, or keyboard shortcut)
2. Dialog opens with two modes: **Voice** and **Type** (type is existing behavior)
3. User taps **Voice** → mic permission requested (first time only)
4. User speaks → real-time interim transcript appears in the text area
5. User taps **Stop** → final transcript populates the text area
6. User can review/edit the text if needed
7. User taps **Save** → Capture created with type=`quick-note`, source=`voice`
8. Extraction triggers automatically

## Technical Approach

### Audio Capture
- Uses `navigator.mediaDevices.getUserMedia({ audio: true })` — mic only, no tab audio
- Single channel (mono) — no stereo mixing needed
- Same `AudioWorklet` PCM processor as browser-audio-capture (shared code)

### Transcription
- Reuses the `DeepgramTranscriptionService` from browser-audio-capture
- Same WebSocket relay endpoint (`/api/transcription/stream`)
- Single-channel mode with Deepgram diarization disabled (single speaker)
- Interim results shown in real-time as user speaks

### Shared Infrastructure
The voice note feature shares the Deepgram pipeline with browser-audio-capture:
- Same `AudioWorklet` processor
- Same WebSocket relay endpoint
- Same Deepgram API key configuration
- Different connection parameters: mono, no multichannel, no diarization

## Quick Capture Dialog Changes

### Mode Toggle
- Two mode buttons at top of dialog: `[mic icon] Voice` | `[keyboard icon] Type`
- Default mode: Type (existing behavior preserved)
- Voice mode only available if Deepgram is configured (otherwise button disabled with tooltip)

### Voice Mode UI
- Large mic button (centered, prominent)
- States: idle → recording → processing → done
- While recording: mic button pulses, duration counter, interim text appears below
- On stop: final text populates the editable text area
- User can switch to Type mode to edit the transcribed text before saving
- Cancel discards the recording and returns to idle state

### Type Mode UI
- Unchanged from existing Quick Capture behavior
- Text area for typing, save button

### Saving
- Both modes produce the same output: a Capture with text content
- Voice mode sets: `type=quick-note`, `source=voice`
- Type mode sets: `type=quick-note`, `source=typed` (existing behavior, add source field)

## Error Handling

- **Mic permission denied**: show inline message, offer to switch to Type mode
- **Deepgram not configured**: Voice button disabled with "Configure Deepgram in Settings" tooltip
- **Deepgram connection fails**: show error, offer to switch to Type mode. If partial transcript was captured, preserve it in the text area.
- **Empty transcript** (user clicked record/stop without speaking): show warning, don't allow save of empty capture

## Acceptance Criteria

- [ ] Quick Capture dialog has Voice and Type mode toggle
- [ ] Voice mode records from mic using `getUserMedia`
- [ ] Real-time interim transcript visible while speaking
- [ ] Final transcript populates editable text area on stop
- [ ] User can edit transcribed text before saving
- [ ] Save creates Capture with type=`quick-note`, source=`voice`
- [ ] Extraction triggers automatically after save
- [ ] Type mode unchanged from current behavior
- [ ] Voice mode disabled when Deepgram not configured
- [ ] Mic permission denial handled gracefully with fallback to Type mode
- [ ] Empty transcript prevented from being saved
- [ ] Shared AudioWorklet and WebSocket infrastructure with browser-audio-capture
- [ ] Voice mode works on Chrome, Edge; graceful fallback on unsupported browsers
