import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { CloseOutQueueItem } from './daily-close-out.models';

export type TriageAction = 'confirm' | 'discard' | 'reassign' | 'quick-discard' | 'process';

@Component({
  selector: 'app-triage-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, TagModule],
  template: `
    <div class="flex flex-col gap-3 p-4 rounded-md border bg-surface-0">
      <div class="flex items-start justify-between gap-3">
        <div class="flex flex-col gap-1 min-w-0">
          @if (capture().title) {
            <h3 class="text-base font-semibold truncate">{{ capture().title }}</h3>
          }
          <div class="flex items-center gap-2 flex-wrap">
            <p-tag [value]="displayedStatus()" [severity]="statusSeverity()" />
            @if (capture().extractionStatus !== 'None') {
              <p-tag [value]="capture().extractionStatus" severity="secondary" />
            }
            <span class="text-xs text-muted-color">{{ capture().captureType }}</span>
          </div>
        </div>
      </div>

      <p class="text-sm whitespace-pre-wrap">{{ capture().rawContent }}</p>

      @if (capture().aiExtraction; as ext) {
        <div class="flex flex-col gap-2 p-3 rounded-md bg-surface-50">
          <span class="text-xs font-semibold uppercase text-muted-color">AI summary</span>
          <p class="text-sm">{{ ext.summary }}</p>
        </div>
      }

      @if (capture().failureReason; as reason) {
        <div class="p-3 rounded-md bg-surface-50 text-sm">
          <span class="font-semibold">Failed:</span> {{ reason }}
        </div>
      }

      <div class="flex items-center gap-2 flex-wrap pt-2">
        @if (canConfirmDiscard()) {
          <p-button
            label="Confirm"
            icon="pi pi-check"
            size="small"
            severity="success"
            (onClick)="action.emit('confirm')"
          />
          <p-button
            label="Discard extraction"
            icon="pi pi-times"
            size="small"
            severity="secondary"
            (onClick)="action.emit('discard')"
          />
        }
        @if (canProcess()) {
          <p-button
            label="Process"
            icon="pi pi-sparkles"
            size="small"
            severity="primary"
            [loading]="processing()"
            [disabled]="processing()"
            (onClick)="action.emit('process')"
          />
        }
        <p-button
          label="Reassign"
          icon="pi pi-sync"
          size="small"
          [text]="true"
          (onClick)="action.emit('reassign')"
        />
        <p-button
          label="Quick discard"
          icon="pi pi-inbox"
          size="small"
          severity="danger"
          [text]="true"
          (onClick)="action.emit('quick-discard')"
        />
      </div>
    </div>
  `,
})
export class TriageCardComponent {
  readonly capture = input.required<CloseOutQueueItem>();
  /**
   * True while a bulk "Process all raw" is running and this card is in
   * flight — lets the parent disable the per-row Process button and
   * show a spinner without changing the underlying data model.
   */
  readonly processing = input<boolean>(false);
  readonly action = output<TriageAction>();

  protected canConfirmDiscard(): boolean {
    const c = this.capture();
    return c.processingStatus === 'Processed' && !c.extractionResolved;
  }

  protected canProcess(): boolean {
    return this.capture().processingStatus === 'Raw' && !this.processing();
  }

  /**
   * Prefer "Processing" while a mutation is in flight from either the
   * per-row button or the bulk runner — the server-side status is still
   * `Raw` until it writes back the transition, so derive an optimistic
   * label here so the badge reflects what the user just clicked.
   */
  protected displayedStatus(): string {
    if (this.processing() && this.capture().processingStatus === 'Raw') {
      return 'Processing';
    }
    return this.capture().processingStatus;
  }

  protected statusSeverity(): 'info' | 'warn' | 'danger' | 'success' | 'secondary' {
    if (this.processing() && this.capture().processingStatus === 'Raw') {
      return 'warn';
    }
    switch (this.capture().processingStatus) {
      case 'Raw':
        return 'info';
      case 'Processing':
        return 'warn';
      case 'Processed':
        return 'success';
      case 'Failed':
        return 'danger';
      default:
        return 'secondary';
    }
  }
}
