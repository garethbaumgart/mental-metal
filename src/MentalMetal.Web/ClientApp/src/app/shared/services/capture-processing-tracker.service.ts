import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, interval, Subscription } from 'rxjs';
import { switchMap, filter } from 'rxjs/operators';
import { CapturesService } from './captures.service';
import { Capture } from '../models/capture.model';

export interface CaptureCompletion {
  capture: Capture;
  status: 'Processed' | 'Failed';
}

/**
 * Global singleton that tracks in-flight capture IDs and polls for their completion.
 * Emits on `completions$` when a tracked capture transitions to Processed or Failed.
 * Used by the app shell to show toast notifications from any page.
 */
@Injectable({ providedIn: 'root' })
export class CaptureProcessingTrackerService {
  private readonly capturesService = inject(CapturesService);

  private readonly trackedIds = new Set<string>();
  private readonly completionsSubject = new Subject<CaptureCompletion>();
  readonly completions$ = this.completionsSubject.asObservable();

  private pollSubscription: Subscription | null = null;

  /** Add a capture ID to the tracking set and start polling if not already running. */
  track(captureId: string): void {
    this.trackedIds.add(captureId);
    this.ensurePolling();
  }

  /** Check if any captures are currently being tracked. */
  get hasTracked(): boolean {
    return this.trackedIds.size > 0;
  }

  private ensurePolling(): void {
    if (this.pollSubscription) return;

    this.pollSubscription = interval(5000)
      .pipe(
        filter(() => this.trackedIds.size > 0),
        switchMap(() => this.capturesService.list()),
      )
      .subscribe({
        next: (captures) => {
          const captureMap = new Map(captures.map((c) => [c.id, c]));

          for (const id of [...this.trackedIds]) {
            const capture = captureMap.get(id);
            if (!capture) continue;

            if (capture.processingStatus === 'Processed') {
              this.trackedIds.delete(id);
              this.completionsSubject.next({ capture, status: 'Processed' });
            } else if (capture.processingStatus === 'Failed') {
              this.trackedIds.delete(id);
              this.completionsSubject.next({ capture, status: 'Failed' });
            }
          }

          if (this.trackedIds.size === 0) {
            this.stopPolling();
          }
        },
        error: () => {
          // Polling failure is non-fatal — will retry on next interval
        },
      });
  }

  private stopPolling(): void {
    this.pollSubscription?.unsubscribe();
    this.pollSubscription = null;
  }
}
