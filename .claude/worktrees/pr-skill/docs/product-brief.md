# My Work Companion — Product Brief

## What is it?

A web application for engineering managers who are too busy to stay organized. It passively accumulates context from your day — meeting transcripts, quick notes, ad-hoc captures — and uses AI to build a living, queryable picture of your people, projects, and priorities. You capture raw; it organizes, links, and surfaces what matters.

## Who is it for?

Senior engineering managers (and similar leadership roles) who manage multiple direct reports across teams, lead cross-team technical initiatives, and spend their days in back-to-back meetings where action items, decisions, and commitments are made and immediately forgotten.

## The problem

Your information is scattered across your head, Google Docs, Confluence, Jira, Slack, and meeting transcripts. By Friday you've forgotten what you promised on Monday. Performance review time is a scramble. You don't know what you've delegated or what's overdue. You've tried tools before but you're too busy to maintain them.

## Core features

### 1. Quick Capture
A persistent, low-friction input for dumping raw thoughts, notes, and meeting context as they happen. No forms, no categorization required. Just text in, AI figures out the rest — linking it to the right person, initiative, or commitment.

### 2. Transcript Paste + AI Extraction
Paste a meeting transcript (from Gemini, or any source). AI extracts: action items, decisions made, commitments (yours and theirs), risks raised, and requirement/design changes. Each extraction is linked to the relevant people and initiatives automatically.

### 3. Audio Recording + AI Processing
A one-tap "Record" / "Stop" flow for capturing meetings and ad-hoc calls directly in the app. No always-on surveillance — you consciously choose which conversations to capture. When you stop recording:

1. **Transcription** — audio is transcribed via local model or cloud API
2. **Speaker diarization** — distinct speakers are identified from audio patterns
3. **Speaker identification** — speakers are matched to known People using a combination of: calendar context (who is in this meeting right now?), name mentions in conversation, and voice profiles that improve over time as the system learns each person's voice across sessions
4. **AI extraction** — action items, decisions, commitments, and risks are extracted and attributed to the identified speaker ("Sarah committed to delivering the API spec by Friday")
5. **Auto-linking** — content is linked to the relevant People and Initiatives
6. **Quick confirmation** — surfaces immediately or in your daily close-out: "I detected a meeting with Sarah and Mike about Project X. 3 action items, 1 commitment. Look right?" You confirm or adjust in 15 seconds

This is intentional-but-effortless capture. One click to start, one click to stop, everything else is automated. The same AI extraction pipeline handles audio recordings, pasted transcripts, and quick text captures — the input method varies, the processing is the same.

**Privacy by design:** Recording is always opt-in per session. The app never records without explicit user action. Sensitive meetings (1:1s, HR conversations) can be skipped at the user's discretion. Audio is always discarded after transcription — only the transcript and extracted data are retained. No audio is ever stored.

### 4. Daily & Weekly Briefing
Your morning command center. The app prepares your day/week: upcoming meetings with pre-loaded context, 1:1 prep (what's outstanding per person), commitments due, initiative risks, interviews scheduled. You open the app and know exactly what needs your brain today.

### 5. Initiative Living Briefs
Each initiative has an AI-maintained living document: current state summary, key decisions with who/when/why, open risks, requirements as they stand today (with evolution history), design direction, your commitments, delegated work, and cross-team dependencies. Every new capture or transcript that relates to the initiative automatically updates the brief. This is your single source of truth — not Jira-level ticket tracking, but the leadership overlay that tells you where things actually stand.

### 6. People Lens
Each direct report has a profile that accumulates over time: 1:1 history, observations (wins, growth moments, concerns), current goals and progress, delegated items and their status, and career notes. At quarterly review time, the evidence is already there. For candidates: CV notes, interview scorecards, transcript summaries, and hiring recommendations.

### 7. Bidirectional Commitments
Track what you owe others and what others owe you. Surfaced in briefings, linked to people and initiatives, with nudges when things go overdue. No more "I forgot I promised that" or "they never got back to me."

### 8. My Queue
A single prioritized view: action items, overdue delegations, commitments due, unprocessed captures needing triage, upcoming prep, and risks needing attention. Filterable by person, initiative, urgency, or "can I delegate this?"

### 9. Daily Close-Out
A 2-minute end-of-day ritual. The app shows uncategorized or uncertain captures. You confirm, reassign, or discard with minimal effort. Keeps data quality high without requiring discipline throughout the day.

### 10. Global AI Chat
Available everywhere in the app, context-aware based on where you are. Ask questions in natural language: "When did the launch date for Project X slip?", "What has Sarah delivered this quarter?", "What did I promise in last week's leadership meeting?" It queries across all your stored data — captures, transcripts, decisions, commitments, observations — and gives you answers with sources.

### 11. Interview Tracking
Candidates with CV notes, structured scorecards, AI-analysed interview transcripts (strengths, concerns, recommendation), and pipeline status. Lightweight but sufficient to replace the Excel sheet.

## What it is NOT

- Not a project management tool — Jira owns the work breakdown
- Not a calendar app — it consumes calendar context, it doesn't manage it
- Not a communication tool — it doesn't send messages or replace Slack/email
- Not a note-taking app — notes are a means of input, not the product

## Success criteria

The app succeeds if:

1. **You open it every morning.** The daily briefing is valuable enough to become your first tab. If it isn't, nothing else matters.
2. **You never say "I forgot" in a meeting.** Commitments, action items, and context are surfaced before you need them, not after.
3. **1:1 prep takes 30 seconds, not 10 minutes.** You open the person's profile and everything you need is there.
4. **Initiative status is always current.** You can answer "where are we on Project X?" without opening 4 different tools.
5. **Quarterly reviews write themselves.** You've accumulated evidence all quarter through normal usage, not a dedicated effort.
6. **Delegation has accountability.** You know what you've assigned, to whom, and whether it's done — without chasing people manually.
7. **Data entry is near-zero.** Capture is raw and fast, AI handles the organizing. The daily close-out is under 2 minutes. If you skip a day, nothing breaks.
8. **After 30 days of use, you can't imagine going back.** The compound value of accumulated context makes the app more useful every week.
