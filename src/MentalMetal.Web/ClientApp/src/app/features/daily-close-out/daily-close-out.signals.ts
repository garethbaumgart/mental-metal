import { computed, inject, Injectable, signal } from '@angular/core';
import { DailyCloseOutService } from './daily-close-out.service';
import {
  CloseOutQueueCounts,
  CloseOutQueueItem,
  DailyCloseOutLog,
} from './daily-close-out.models';

@Injectable({ providedIn: 'root' })
export class DailyCloseOutStore {
  private readonly service = inject(DailyCloseOutService);

  private readonly _items = signal<CloseOutQueueItem[]>([]);
  private readonly _counts = signal<CloseOutQueueCounts>({
    total: 0,
    raw: 0,
    processing: 0,
    processed: 0,
    failed: 0,
  });
  private readonly _isLoading = signal<boolean>(false);
  private readonly _lastSummary = signal<DailyCloseOutLog | null>(null);

  readonly items = this._items.asReadonly();
  readonly counts = this._counts.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly lastSummary = this._lastSummary.asReadonly();
  readonly isEmpty = computed(() => this._items().length === 0);

  refreshQueue(): void {
    this._isLoading.set(true);
    this.service.getQueue().subscribe({
      next: (resp) => {
        this._items.set(resp.items);
        this._counts.set(resp.counts);
        this._isLoading.set(false);
      },
      error: () => this._isLoading.set(false),
    });
  }

  removeFromQueue(captureId: string): void {
    this._items.update((items) => items.filter((i) => i.id !== captureId));
  }

  replaceItem(updated: CloseOutQueueItem): void {
    this._items.update((items) =>
      items.map((i) => (i.id === updated.id ? updated : i)),
    );
  }

  setLastSummary(log: DailyCloseOutLog | null): void {
    this._lastSummary.set(log);
  }
}
