import { Injectable, signal, computed, OnDestroy } from '@angular/core';

export type RecordingState = 'idle' | 'recording' | 'paused';
export type AudioCaptureMode = 'microphone' | 'both';

/**
 * Audio recorder service ported from Praxis-note and adapted to Mental Metal conventions.
 * Captures microphone (and optionally tab audio) using Web Audio API.
 * Produces raw PCM Int16 chunks for streaming to Deepgram via AudioWorklet.
 *
 * Two modes:
 * - 'microphone': mic only (for voice notes, in-person meetings)
 * - 'both': stereo — left channel = mic, right channel = tab audio (for online meetings)
 *
 * Uses signals for all state (zoneless-safe).
 */
@Injectable({ providedIn: 'root' })
export class AudioRecorderService implements OnDestroy {
  private mediaRecorder: MediaRecorder | null = null;
  private micStream: MediaStream | null = null;
  private systemStream: MediaStream | null = null;
  private mixedStream: MediaStream | null = null;
  private audioContext: AudioContext | null = null;
  private mixingContext: AudioContext | null = null;
  private analyserNode: AnalyserNode | null = null;
  private chunks: Blob[] = [];
  private timerInterval: ReturnType<typeof setInterval> | null = null;
  private levelAnimationId: number | null = null;
  private isStarting = false;
  private startToken = 0;
  private recoveryAttempts = 0;
  private static readonly MAX_RECOVERY_ATTEMPTS = 5;
  private static readonly RECOVERY_ATTEMPT_WINDOW_MS = 60_000;
  private lastRecoveryTime = 0;
  private isRecovering = false;
  private contextKeepAliveInterval: ReturnType<typeof setInterval> | null = null;
  private pcmWorkletNode: AudioWorkletNode | null = null;
  private pcmBuffer: Int16Array[] = [];
  private pcmFlushInterval: ReturnType<typeof setInterval> | null = null;
  private mixingDestination: MediaStreamAudioDestinationNode | null = null;

  readonly state = signal<RecordingState>('idle');
  readonly elapsedSeconds = signal(0);
  readonly error = signal<string | null>(null);
  readonly audioLevels = signal<number[]>(new Array(16).fill(0));
  readonly captureMode = signal<AudioCaptureMode>('microphone');
  readonly channelCount = signal(1);
  readonly sampleRate = signal(48000);

  readonly onPcmChunk = signal<((data: ArrayBuffer) => void) | null>(null);

  readonly isRecording = computed(() => this.state() === 'recording');
  readonly isActive = computed(() => this.state() !== 'idle');
  readonly hasSystemAudio = computed(() => this.captureMode() === 'both');

  readonly formattedTime = computed(() => {
    const total = this.elapsedSeconds();
    const minutes = Math.floor(total / 60);
    const seconds = total % 60;
    return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
  });

  ngOnDestroy(): void {
    this.discard();
  }

  /** Start recording with microphone only. */
  async start(): Promise<void> {
    return this.startRecording('microphone');
  }

  /** Start recording with mic + tab audio (stereo). */
  async startWithSystemAudio(): Promise<void> {
    return this.startRecording('both');
  }

  private async startRecording(mode: AudioCaptureMode): Promise<void> {
    if (this.state() !== 'idle' || this.isStarting) return;
    this.isStarting = true;
    const token = ++this.startToken;

    this.error.set(null);
    this.captureMode.set(mode);
    this.recoveryAttempts = 0;
    this.lastRecoveryTime = 0;

    try {
      this.micStream = await this.getMicrophoneStream();
      if (token !== this.startToken) {
        this.releaseStream(this.micStream);
        this.micStream = null;
        this.isStarting = false;
        return;
      }

      if (mode === 'both') {
        try {
          this.systemStream = await this.getSystemAudioStream();
          if (token !== this.startToken) {
            this.releaseStream(this.micStream);
            this.releaseStream(this.systemStream);
            this.micStream = null;
            this.systemStream = null;
            this.isStarting = false;
            return;
          }
        } catch {
          // User cancelled tab sharing — fall back to mic-only
          this.captureMode.set('microphone');
        }
      }

      const recordingStream = this.createRecordingStream();
      if (!recordingStream) {
        throw new Error('No audio stream available');
      }

      this.audioContext = new AudioContext();
      if (this.audioContext.state === 'suspended') {
        await this.audioContext.resume();
      }
      const source = this.audioContext.createMediaStreamSource(recordingStream);
      this.analyserNode = this.audioContext.createAnalyser();
      this.analyserNode.fftSize = 64;
      source.connect(this.analyserNode);

      this.chunks = [];
      this.initMediaRecorder(recordingStream);
      this.monitorAudioTracks();

      // Start PCM capture for streaming transcription
      await this.startPcmCapture(recordingStream);

      this.mediaRecorder!.start(1000);
      this.state.set('recording');
      this.elapsedSeconds.set(0);
      this.startTimer();
      this.startLevelMetering();
      this.startContextKeepAlive();
    } catch (err) {
      this.cleanup();
      if (err instanceof DOMException && err.name === 'NotAllowedError') {
        this.error.set('Microphone access denied. Please allow microphone permissions and try again.');
      } else if (err instanceof Error) {
        this.error.set(err.message);
      } else {
        this.error.set('Failed to start recording. Please check your audio settings.');
      }
    } finally {
      this.isStarting = false;
    }
  }

  private initMediaRecorder(stream: MediaStream): void {
    const mimeType = this.getSupportedMimeType();
    this.mediaRecorder = new MediaRecorder(
      stream,
      mimeType ? { mimeType } : undefined,
    );

    this.mediaRecorder.ondataavailable = (e) => {
      if (e.data.size > 0) {
        this.chunks.push(e.data);
      }
    };

    this.mediaRecorder.onerror = () => {
      this.attemptRecovery('MediaRecorder error');
    };
  }

  private monitorAudioTracks(): void {
    if (this.systemStream) {
      const videoTrack = this.systemStream.getVideoTracks()[0];
      if (videoTrack) {
        videoTrack.onended = () => {
          this.captureMode.set('microphone');
        };
      }
    }

    if (this.micStream) {
      for (const track of this.micStream.getAudioTracks()) {
        track.onended = () => {
          if (this.state() !== 'idle') {
            this.attemptRecovery('microphone track ended');
          }
        };
      }
    }

    if (this.systemStream) {
      for (const track of this.systemStream.getAudioTracks()) {
        track.onended = () => {
          if (this.state() !== 'idle') {
            this.captureMode.set('microphone');
            this.attemptRecovery('system audio track ended');
          }
        };
      }
    }
  }

  private attemptRecovery(reason: string): void {
    if (this.isRecovering || this.state() === 'idle') return;
    this.isRecovering = true;

    const now = Date.now();
    if (now - this.lastRecoveryTime > AudioRecorderService.RECOVERY_ATTEMPT_WINDOW_MS) {
      this.recoveryAttempts = 0;
    }

    this.recoveryAttempts++;
    this.lastRecoveryTime = now;

    if (this.recoveryAttempts > AudioRecorderService.MAX_RECOVERY_ATTEMPTS) {
      this.error.set('Recording failed repeatedly and could not recover. Please start a new recording.');
      this.isRecovering = false;
      this.cleanup();
      return;
    }

    this.recoverRecording().then((recovered) => {
      this.isRecovering = false;
      if (!recovered) {
        setTimeout(() => {
          if (this.state() !== 'idle') {
            this.attemptRecovery('retry after failed recovery');
          }
        }, 2000);
      }
    }).catch(() => {
      this.isRecovering = false;
      this.error.set('Recording encountered an unexpected error and could not recover.');
      this.cleanup();
    });
  }

  private async recoverRecording(): Promise<boolean> {
    if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
      try {
        this.mediaRecorder.onerror = null;
        this.mediaRecorder.ondataavailable = null;
        this.mediaRecorder.stop();
      } catch {
        // Already stopped
      }
    }
    this.mediaRecorder = null;

    const micAlive = this.micStream?.getAudioTracks().some((t) => t.readyState === 'live') ?? false;
    if (!micAlive) {
      this.releaseStream(this.micStream);
      this.micStream = null;
      try {
        this.micStream = await this.getMicrophoneStream();
      } catch {
        return false;
      }
      if (this.state() === 'idle') {
        this.releaseStream(this.micStream);
        this.micStream = null;
        return false;
      }
    }

    const systemAlive = this.systemStream?.getAudioTracks().some((t) => t.readyState === 'live') ?? false;
    if (!systemAlive && this.systemStream) {
      this.releaseStream(this.systemStream);
      this.systemStream = null;
      this.captureMode.set('microphone');
    }

    this.stopPcmCapture();
    if (this.mixingContext) {
      try { this.mixingContext.close(); } catch { /* ignore */ }
      this.mixingContext = null;
    }
    this.mixedStream = null;
    this.mixingDestination = null;

    const recordingStream = this.createRecordingStream();
    if (!recordingStream) return false;

    this.stopLevelMetering();
    if (this.audioContext) {
      try { this.audioContext.close(); } catch { /* ignore */ }
    }
    this.audioContext = new AudioContext();
    if (this.audioContext.state === 'suspended') {
      await this.audioContext.resume();
    }

    if (this.state() === 'idle') return false;

    const source = this.audioContext.createMediaStreamSource(recordingStream);
    this.analyserNode = this.audioContext.createAnalyser();
    this.analyserNode.fftSize = 64;
    source.connect(this.analyserNode);

    this.initMediaRecorder(recordingStream);
    this.monitorAudioTracks();

    await this.startPcmCapture(recordingStream);

    try {
      this.mediaRecorder!.start(1000);
    } catch {
      return false;
    }

    this.state.set('recording');
    this.startLevelMetering();
    this.error.set(null);
    return true;
  }

  private async getMicrophoneStream(): Promise<MediaStream> {
    try {
      return await navigator.mediaDevices.getUserMedia({ audio: true });
    } catch (err) {
      if (err instanceof DOMException && err.name === 'NotAllowedError') {
        throw new DOMException(
          'Microphone access denied. Please allow microphone permissions and try again.',
          'NotAllowedError',
        );
      }
      throw new DOMException(
        'Could not access microphone. Please check your audio settings.',
        err instanceof DOMException ? err.name : 'NotReadableError',
      );
    }
  }

  private async getSystemAudioStream(): Promise<MediaStream> {
    const stream = await navigator.mediaDevices.getDisplayMedia({
      video: true,
      audio: {
        echoCancellation: false,
        noiseSuppression: false,
        autoGainControl: false,
      } as MediaTrackConstraints,
    });

    const audioTracks = stream.getAudioTracks();
    if (audioTracks.length === 0) {
      this.releaseStream(stream);
      throw new Error('No audio captured. Please share a browser tab and enable "Share audio".');
    }

    return stream;
  }

  private createRecordingStream(): MediaStream | null {
    if (!this.systemStream) {
      this.mixedStream = this.micStream;
      this.channelCount.set(1);
      return this.micStream;
    }

    if (!this.micStream) return null;

    // Create stereo stream: channel 0 = mic, channel 1 = system audio
    this.mixingContext = new AudioContext();
    if (this.mixingContext.state === 'suspended') {
      this.mixingContext.resume().catch(() => {});
    }

    const micSource = this.mixingContext.createMediaStreamSource(this.micStream);
    const systemSource = this.mixingContext.createMediaStreamSource(this.systemStream);
    const merger = this.mixingContext.createChannelMerger(2);

    const micGain = this.mixingContext.createGain();
    const systemGain = this.mixingContext.createGain();
    micGain.gain.value = 1.0;
    systemGain.gain.value = 1.0;

    micSource.connect(micGain);
    systemSource.connect(systemGain);
    micGain.connect(merger, 0, 0);
    systemGain.connect(merger, 0, 1);

    this.mixingDestination = this.mixingContext.createMediaStreamDestination();
    merger.connect(this.mixingDestination);

    this.mixedStream = this.mixingDestination.stream;
    this.channelCount.set(2);
    return this.mixingDestination.stream;
  }

  private async startPcmCapture(stream: MediaStream): Promise<void> {
    // For mic-only mode, create a separate AudioContext for PCM capture
    // For stereo mode, use the mixing context
    const ctx = this.mixingContext ?? new AudioContext();
    if (!this.mixingContext) {
      // Store as mixing context so cleanup handles it
      this.mixingContext = ctx;
    }

    this.sampleRate.set(ctx.sampleRate);

    try {
      await ctx.audioWorklet.addModule('audio-pcm-processor.js');
    } catch {
      this.error.set('Audio capture module unavailable. Recording will continue without live transcription.');
      return;
    }

    this.pcmWorkletNode = new AudioWorkletNode(ctx, 'pcm-processor', {
      channelCount: this.channelCount(),
      channelCountMode: 'explicit',
    });

    const workletSource = ctx.createMediaStreamSource(stream);
    workletSource.connect(this.pcmWorkletNode);

    this.pcmBuffer = [];

    this.pcmWorkletNode.port.onmessage = (event: MessageEvent) => {
      const channels = event.data.channels as Float32Array[];
      if (channels && channels.length > 0 && channels[0]?.length > 0) {
        const interleaved = this.float32ToInt16Interleaved(channels);
        this.pcmBuffer.push(new Int16Array(interleaved));
      }
    };

    this.startPcmFlushInterval();
  }

  private startPcmFlushInterval(): void {
    if (this.pcmFlushInterval !== null) return;
    this.pcmFlushInterval = setInterval(() => {
      if (this.pcmBuffer.length === 0) return;

      const chunks = this.pcmBuffer;
      this.pcmBuffer = [];

      let totalLength = 0;
      for (const chunk of chunks) {
        totalLength += chunk.length;
      }

      const merged = new Int16Array(totalLength);
      let offset = 0;
      for (const chunk of chunks) {
        merged.set(chunk, offset);
        offset += chunk.length;
      }

      this.onPcmChunk()?.(merged.buffer);
    }, 250);
  }

  private float32ToInt16Interleaved(channels: Float32Array[]): ArrayBuffer {
    if (!channels || channels.length === 0 || !channels[0] || channels[0].length === 0) {
      return new ArrayBuffer(0);
    }
    const channelCount = channels.length;
    const sampleCount = channels[0].length;
    const buffer = new ArrayBuffer(sampleCount * channelCount * 2);
    const view = new DataView(buffer);

    let offset = 0;
    for (let i = 0; i < sampleCount; i++) {
      for (let ch = 0; ch < channelCount; ch++) {
        const sample = Math.max(-1, Math.min(1, channels[ch][i]));
        const int16 = sample < 0 ? sample * 0x8000 : sample * 0x7fff;
        view.setInt16(offset, int16, true);
        offset += 2;
      }
    }

    return buffer;
  }

  private stopPcmCapture(): void {
    if (this.pcmFlushInterval !== null) {
      clearInterval(this.pcmFlushInterval);
      this.pcmFlushInterval = null;
    }
    if (this.pcmWorkletNode) {
      this.pcmWorkletNode.port.onmessage = null;
      this.pcmWorkletNode.disconnect();
      this.pcmWorkletNode = null;
    }
    this.pcmBuffer = [];
  }

  private releaseStream(stream: MediaStream | null): void {
    if (stream) {
      for (const track of stream.getTracks()) {
        track.onended = null;
        track.stop();
      }
    }
  }

  stop(): Promise<void> {
    return new Promise((resolve) => {
      if (!this.mediaRecorder || this.mediaRecorder.state === 'inactive') {
        this.cleanup();
        resolve();
        return;
      }

      this.mediaRecorder.onstop = () => {
        this.cleanup();
        resolve();
      };

      this.mediaRecorder.stop();
    });
  }

  discard(): void {
    this.startToken++;
    if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
      this.mediaRecorder.onstop = () => {};
      this.mediaRecorder.stop();
    }
    this.cleanup();
  }

  static isSystemAudioSupported(): boolean {
    return (
      typeof navigator !== 'undefined' &&
      typeof navigator.mediaDevices !== 'undefined' &&
      typeof navigator.mediaDevices.getDisplayMedia === 'function'
    );
  }

  private getSupportedMimeType(): string | undefined {
    const types = [
      'audio/webm;codecs=opus',
      'audio/webm',
      'audio/mp4',
      'audio/ogg;codecs=opus',
    ];
    for (const type of types) {
      if (MediaRecorder.isTypeSupported(type)) {
        return type;
      }
    }
    return undefined;
  }

  private startTimer(): void {
    this.stopTimer();
    this.timerInterval = setInterval(() => {
      this.elapsedSeconds.update((s) => s + 1);
    }, 1000);
  }

  private stopTimer(): void {
    if (this.timerInterval !== null) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
  }

  private startLevelMetering(): void {
    if (!this.analyserNode) return;

    const bufferLength = this.analyserNode.frequencyBinCount;
    const dataArray = new Uint8Array(bufferLength);
    const barCount = 16;

    const update = () => {
      if (!this.analyserNode || this.state() === 'idle') return;

      this.analyserNode.getByteFrequencyData(dataArray);

      const levels: number[] = [];
      const binsPerBar = Math.floor(bufferLength / barCount);

      for (let i = 0; i < barCount; i++) {
        let sum = 0;
        for (let j = 0; j < binsPerBar; j++) {
          sum += dataArray[i * binsPerBar + j];
        }
        levels.push(sum / (binsPerBar * 255));
      }

      this.audioLevels.set(levels);
      this.levelAnimationId = requestAnimationFrame(update);
    };

    this.levelAnimationId = requestAnimationFrame(update);
  }

  private stopLevelMetering(): void {
    if (this.levelAnimationId !== null) {
      cancelAnimationFrame(this.levelAnimationId);
      this.levelAnimationId = null;
    }
  }

  private startContextKeepAlive(): void {
    this.stopContextKeepAlive();
    this.contextKeepAliveInterval = setInterval(() => {
      if (this.state() === 'idle') return;
      if (this.audioContext?.state === 'suspended') {
        this.audioContext.resume().catch(() => {});
      }
      if (this.mixingContext?.state === 'suspended') {
        this.mixingContext.resume().catch(() => {});
      }
    }, 5000);
  }

  private stopContextKeepAlive(): void {
    if (this.contextKeepAliveInterval !== null) {
      clearInterval(this.contextKeepAliveInterval);
      this.contextKeepAliveInterval = null;
    }
  }

  private cleanup(): void {
    this.stopTimer();
    this.stopLevelMetering();
    this.stopContextKeepAlive();
    this.stopPcmCapture();

    this.releaseStream(this.micStream);
    this.releaseStream(this.systemStream);

    this.micStream = null;
    this.systemStream = null;
    this.mixedStream = null;
    this.mixingDestination = null;

    if (this.audioContext) {
      this.audioContext.close();
      this.audioContext = null;
    }

    if (this.mixingContext) {
      this.mixingContext.close();
      this.mixingContext = null;
    }

    this.analyserNode = null;
    this.mediaRecorder = null;
    this.chunks = [];
    this.state.set('idle');
    this.elapsedSeconds.set(0);
    this.audioLevels.set(new Array(16).fill(0));
    this.captureMode.set('microphone');
    this.channelCount.set(1);
    this.sampleRate.set(48000);
    this.recoveryAttempts = 0;
    this.isRecovering = false;
  }
}
