# Browser Audio Capture

Port the proven audio capture system from the Praxis-note project to enable recording Google Meet calls directly from the browser when Google's built-in recording is unavailable. Uses native Web Audio API (no Chrome extension) with Deepgram for real-time streaming transcription.

## Architecture

Ported from Praxis-note with minimal adaptation to fit Mental Metal's existing Angular + .NET stack.

### Client-Side Components (Angular)

**AudioRecorderService** (port from `praxis-note/audio-recorder.service.ts`)
- Two capture modes:
  - `microphone` — mic only, for in-person conversations or phone calls
  - `both` — stereo mix: left channel = local user (mic), right channel = remote participants (tab audio)
- Uses `navigator.mediaDevices.getUserMedia()` for microphone
- Uses `navigator.mediaDevices.getDisplayMedia()` for tab/system audio
- Web Audio API `ChannelMerger` mixes streams into stereo
- `MediaRecorder` produces browser-playable blob (for optional local playback, not stored server-side)
- `AudioWorklet` processor captures raw PCM Int16 for transcription streaming
- Audio level metering via `AnalyserNode` for real-time visualiser (16-bar frequency display)

**Recovery logic** (from Praxis-note):
- Max 5 audio recovery attempts per 60-second window
- Auto-reconnect mic if track ends unexpectedly
- Falls back to mic-only if system audio track is lost
- Rebuilds AudioContext and PCM capture pipeline on recovery

**DeepgramTranscriptionService** (port from `praxis-note/deepgram-transcription.service.ts`)
- WebSocket connection to backend relay endpoint
- Streams raw PCM audio chunks (flushed every 250ms)
- Receives interim and final transcript results
- Two transcription modes:
  - Single-channel with diarization (mic-only or container formats)
  - Multichannel stereo (when both mic + tab audio captured as raw PCM)

**Reconnection logic** (from Praxis-note):
- Max 10 reconnect attempts per session
- Max 20 total reconnects across recording lifetime
- Exponential backoff: 500ms → 15 seconds
- Audio buffered during WebSocket reconnect, flushed on restore

**AudioWorklet processor** (`audio-pcm-processor.js`)
- Runs in AudioWorklet thread for low-latency processing
- Converts Float32 multichannel audio → interleaved Int16 PCM (little-endian)
- Buffers chunks and posts to main thread every 250ms

### Backend Components (.NET)

**TranscriptionEndpoints** (new, modelled on Praxis-note)
- `GET /api/transcription/status` — health check (is Deepgram configured and reachable?)
- `WS /api/transcription/stream` — WebSocket relay between browser and Deepgram
  - Authenticates via session cookie (same as existing auth)
  - Opens upstream WebSocket to Deepgram with user's API key
  - Relays binary audio frames from browser → Deepgram
  - Relays JSON transcript results from Deepgram → browser
  - Closes both connections on disconnect from either side

**Deepgram Configuration**
- API key stored per-user alongside existing AI provider config (new field in user settings)
- Or: shared application-level Deepgram key (configured in appsettings) — decision deferred to implementation
- Connection parameters: model=nova-3, encoding=linear16, sample_rate=48000, channels=2, interim_results=true, punctuate=true, diarize=true

## Recording Flow

1. User clicks "Record Meeting" on Captures page
2. Browser prompts for screen/tab sharing (for tab audio) and mic access
3. Recording starts — audio visualiser shows levels, interim transcript appears in real-time
4. User clicks "Stop Recording"
5. Final transcript assembled from all final results
6. Capture created: type=`meeting-recording`, source=`audio-capture`, content=transcript text
7. Extraction pipeline triggers automatically (see `ai-auto-extraction-v2` spec)

## Recording UI

### Recording Panel
- Appears inline on the Captures page (not a dialog — user needs to see it while in the meeting)
- Shows: recording duration, audio level visualiser, interim transcript (scrolling)
- Controls: Stop, Pause/Resume (if supported), Cancel (discard without saving)
- Mic indicator: shows if mic audio is being captured
- Tab audio indicator: shows if system audio is being captured

### Post-Recording
- On stop: shows full transcript for review (read-only, not editable)
- "Save" creates the capture and starts extraction
- "Discard" throws away everything (no server-side storage of audio or transcript)
- No audio file stored server-side — only transcript text

### Settings Integration
- Deepgram API key configuration in Settings page (new section below AI Provider)
- Test button to verify key validity
- Status indicator: "Deepgram connected" / "Not configured"

## What Is NOT Ported

- Raw audio file storage (Praxis-note stores nothing; Mental Metal follows same pattern)
- Meeting entity (Praxis-note has a Meeting aggregate; Mental Metal uses Capture)
- AI analysis (Praxis-note has its own; Mental Metal uses the existing extraction pipeline)
- Meeting notes editing (Praxis-note allows manual notes; Mental Metal relies on transcript + extraction)

## Permissions and Browser Requirements

- `getDisplayMedia()` requires user gesture (button click) and HTTPS
- Chrome/Edge: fully supported. Firefox: `getDisplayMedia()` for tab audio may have limitations
- Safari: `getDisplayMedia()` not supported — mic-only mode is the fallback
- The user must select the correct Google Meet tab when prompted for screen sharing
- No Chrome extension required — all APIs are standard Web Platform

## Error Handling

- **Mic permission denied**: show error, cannot record
- **Screen share cancelled**: fall back to mic-only mode with diarization
- **Deepgram connection fails**: buffer audio, retry with backoff; if unrecoverable, save what was transcribed so far
- **Browser tab closed during recording**: audio track ends, recovery logic attempts to save partial transcript
- **Network interruption**: WebSocket reconnection with buffered audio flush

## Acceptance Criteria

- [ ] AudioRecorderService ported and adapted to Mental Metal's Angular project
- [ ] DeepgramTranscriptionService ported and adapted
- [ ] AudioWorklet processor (`audio-pcm-processor.js`) ported to `public/` assets
- [ ] WebSocket relay endpoint (`/api/transcription/stream`) implemented in .NET
- [ ] Health check endpoint (`/api/transcription/status`) implemented
- [ ] Stereo capture works: mic on left channel, tab audio on right channel
- [ ] Mic-only fallback works when screen share is cancelled
- [ ] Real-time interim transcript visible during recording
- [ ] Final transcript assembled and saved as Capture on stop
- [ ] Capture type set to `meeting-recording`, source set to `audio-capture`
- [ ] Extraction triggers automatically after capture creation
- [ ] No audio files stored server-side (transcript text only)
- [ ] Audio level visualiser works during recording
- [ ] Recovery logic handles mic disconnection (max 5 attempts per 60s)
- [ ] WebSocket reconnection handles network blips (exponential backoff)
- [ ] Audio buffered during WebSocket reconnect and flushed on restore
- [ ] Deepgram API key configurable in Settings
- [ ] Settings shows connection status indicator
- [ ] Discard option available during/after recording
- [ ] Works on Chrome and Edge; graceful degradation on Firefox/Safari
