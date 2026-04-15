import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { MyQueueService } from '../../features/my-queue/my-queue.service';
import { QueueItem } from '../../features/my-queue/my-queue.models';

/**
 * Top 5 items from My Queue, in the service's existing priority order.
 * Subscribes to the shared `MyQueueService` signals so we inherit its
 * loading/error state without duplicating fetch logic.
 */
@Component({
  selector: 'app-top-of-queue-widget',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ButtonModule, TagModule],
  template: `
    <section
      class="flex flex-col gap-3 p-5 rounded-md bg-surface-50"
      aria-label="Top of queue"
    >
      <header class="flex items-center justify-between">
        <h2 class="text-lg font-semibold">Top of your queue</h2>
        <a
          routerLink="/my-queue"
          class="text-xs font-medium text-primary hover:underline"
        >View all</a>
      </header>

      @if (queue.loading()) {
        <div class="flex items-center gap-2 py-4 justify-center">
          <i class="pi pi-spinner pi-spin"></i>
          <span class="text-sm text-muted-color">Loading…</span>
        </div>
      } @else if (queue.error(); as msg) {
        <div class="flex flex-col items-start gap-2 py-3">
          <p class="text-sm text-muted-color">{{ msg }}</p>
          <p-button label="Retry" icon="pi pi-refresh" size="small" [text]="true" (onClick)="reload()" />
        </div>
      } @else if (top().length === 0) {
        <p class="text-sm text-muted-color py-2">Queue is empty. Nice work.</p>
      } @else {
        <ul class="flex flex-col gap-2">
          @for (item of top(); track item.id) {
            <li class="flex items-center gap-3 p-2 rounded bg-surface-0">
              <p-tag [value]="item.itemType" severity="secondary" />
              <span class="flex-1 text-sm">{{ item.title }}</span>
              @if (item.isOverdue) {
                <p-tag value="Overdue" severity="danger" />
              }
            </li>
          }
        </ul>
      }
    </section>
  `,
})
export class TopOfQueueWidgetComponent {
  protected readonly queue = inject(MyQueueService);

  protected readonly top = computed<QueueItem[]>(() => {
    const resp = this.queue.response();
    return resp ? resp.items.slice(0, 5) : [];
  });

  constructor() {
    // Only trigger a fetch if the queue service hasn't already loaded.
    effect(() => {
      if (!this.queue.response() && !this.queue.loading() && !this.queue.error()) {
        this.queue.load();
      }
    });
  }

  protected reload(): void {
    this.queue.load();
  }
}
