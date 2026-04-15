import { Injectable, signal } from '@angular/core';
import { Subject } from 'rxjs';
import { Capture } from '../models/capture.model';

/**
 * Shared open/close state and broadcast channel for the global Quick Capture
 * dialog. The dialog is mounted once at the authenticated shell level, so
 * the sidebar/FAB/keyboard shortcut and the /capture list all toggle the
 * same instance via this service rather than owning local visibility state.
 *
 * `captureCreated$` lets listeners (e.g. the `/capture` list) react to
 * captures authored from anywhere in the app, without the dialog caring
 * who is listening.
 */
@Injectable({ providedIn: 'root' })
export class QuickCaptureUiService {
  readonly visible = signal(false);

  private readonly captureCreatedSubject = new Subject<Capture>();
  readonly captureCreated$ = this.captureCreatedSubject.asObservable();

  /** Open the dialog. No-op if already visible. */
  open(): void {
    if (!this.visible()) this.visible.set(true);
  }

  /** Close the dialog. */
  close(): void {
    this.visible.set(false);
  }

  /** Dialog callback: broadcast the new capture to listeners. */
  notifyCreated(capture: Capture): void {
    this.captureCreatedSubject.next(capture);
  }
}
