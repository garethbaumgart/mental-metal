import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { QueueItem } from './my-queue.models';

@Component({
  selector: 'app-my-queue-item',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, TagModule],
  template: `
    <article class="flex flex-col gap-3 p-4 rounded-md bg-surface-0 border border-surface-200">
      <header class="flex items-start justify-between gap-3 flex-wrap">
        <div class="flex items-center gap-2 min-w-0">
          @switch (item().itemType) {
            @case ('Commitment') {
              <p-tag value="Commitment" severity="warn" />
            }
            @case ('Delegation') {
              <p-tag value="Delegation" severity="info" />
            }
            @case ('Capture') {
              <p-tag value="Capture" severity="secondary" />
            }
          }
          @if (item().isOverdue) {
            <p-tag value="Overdue" severity="danger" />
          }
          <span class="text-sm font-medium truncate">{{ item().title }}</span>
        </div>
        <span class="text-xs text-muted-color">
          Priority {{ item().priorityScore }}
        </span>
      </header>

      <div class="flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-color">
        @if (item().personName) {
          <span><i class="pi pi-user mr-1"></i>{{ item().personName }}</span>
        }
        @if (item().initiativeName) {
          <span><i class="pi pi-flag mr-1"></i>{{ item().initiativeName }}</span>
        }
        @if (item().dueDate) {
          <span><i class="pi pi-calendar mr-1"></i>Due {{ item().dueDate }}</span>
        }
        @if (item().daysSinceCaptured !== null) {
          <span>
            <i class="pi pi-clock mr-1"></i>Captured {{ item().daysSinceCaptured }} day(s) ago
          </span>
        }
        <span>{{ item().status }}</span>
      </div>

      @if (item().suggestDelegate) {
        <div class="flex justify-end">
          <p-button
            label="Delegate this"
            icon="pi pi-share-alt"
            severity="secondary"
            size="small"
            [text]="true"
            (onClick)="delegate.emit(item())"
          />
        </div>
      }
    </article>
  `,
})
export class MyQueueItemComponent {
  readonly item = input.required<QueueItem>();
  readonly delegate = output<QueueItem>();
}
