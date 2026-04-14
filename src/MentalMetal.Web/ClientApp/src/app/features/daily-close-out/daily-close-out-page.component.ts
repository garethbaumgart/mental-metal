import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { ProgressBarModule } from 'primeng/progressbar';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { CapturesService } from '../../shared/services/captures.service';
import { DailyCloseOutService } from './daily-close-out.service';
import { DailyCloseOutStore } from './daily-close-out.signals';
import { TriageCardComponent, TriageAction } from './triage-card.component';
import { ReassignDialogComponent } from './reassign-dialog.component';
import { CloseOutQueueItem, ReassignCaptureRequest } from './daily-close-out.models';

@Component({
  selector: 'app-daily-close-out-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ButtonModule,
    ProgressBarModule,
    ToastModule,
    TriageCardComponent,
    ReassignDialogComponent,
  ],
  providers: [MessageService],
  template: `
    <p-toast />
    <div class="flex flex-col gap-6 max-w-3xl mx-auto">
      <header class="flex flex-col gap-2">
        <h1 class="text-2xl font-bold">Daily close-out</h1>
        <p class="text-sm text-muted-color">
          Triage captures that still need your attention, then close out the day.
        </p>
      </header>

      <section class="flex flex-col gap-3 p-4 rounded-md bg-surface-50">
        <div class="flex items-center justify-between gap-3 flex-wrap">
          <div class="flex flex-col gap-1">
            <span class="text-sm font-semibold">Progress</span>
            <span class="text-xs text-muted-color">
              {{ store.counts().total }} pending ·
              Raw {{ store.counts().raw }} · Processing {{ store.counts().processing }} ·
              Processed {{ store.counts().processed }} · Failed {{ store.counts().failed }}
            </span>
          </div>
          <p-button
            label="Close out the day"
            icon="pi pi-flag"
            [disabled]="store.isLoading()"
            (onClick)="closeOutDay()"
          />
        </div>
      </section>

      @if (store.isLoading()) {
        <div class="flex justify-center p-8">
          <i class="pi pi-spinner pi-spin text-2xl"></i>
        </div>
      } @else if (store.isEmpty()) {
        <div class="flex flex-col items-center gap-4 p-12 bg-surface-50 rounded-md">
          <i class="pi pi-check-circle text-4xl text-primary"></i>
          <p class="text-sm text-muted-color">Inbox zero — nothing left to triage.</p>
        </div>
      } @else {
        <div class="flex flex-col gap-3">
          @for (item of store.items(); track item.id) {
            <app-triage-card [capture]="item" (action)="onAction(item, $event)" />
          }
        </div>
      }

      @if (store.lastSummary(); as summary) {
        <section class="flex flex-col gap-2 p-4 rounded-md bg-surface-50">
          <span class="text-sm font-semibold">Latest close-out</span>
          <span class="text-xs text-muted-color">
            {{ summary.date }} · Confirmed {{ summary.confirmedCount }} ·
            Discarded {{ summary.discardedCount }} · Remaining {{ summary.remainingCount }}
          </span>
        </section>
      }

      <app-reassign-dialog
        [visible]="reassignVisible()"
        [capture]="reassignTarget()"
        (visibleChange)="reassignVisible.set($event)"
        (applied)="onReassignApplied($event)"
      />
    </div>
  `,
})
export class DailyCloseOutPageComponent implements OnInit {
  protected readonly store = inject(DailyCloseOutStore);
  private readonly service = inject(DailyCloseOutService);
  private readonly capturesService = inject(CapturesService);
  private readonly messageService = inject(MessageService);

  protected readonly reassignVisible = signal(false);
  protected readonly reassignTarget = signal<CloseOutQueueItem | null>(null);

  ngOnInit(): void {
    this.store.refreshQueue();
  }

  onAction(item: CloseOutQueueItem, action: TriageAction): void {
    switch (action) {
      case 'confirm':
        this.capturesService.confirmExtraction(item.id).subscribe({
          next: () => {
            this.store.removeFromQueue(item.id);
            this.messageService.add({ severity: 'success', summary: 'Extraction confirmed' });
          },
          error: () =>
            this.messageService.add({ severity: 'error', summary: 'Failed to confirm' }),
        });
        break;
      case 'discard':
        this.capturesService.discardExtraction(item.id).subscribe({
          next: () => {
            this.store.removeFromQueue(item.id);
            this.messageService.add({ severity: 'success', summary: 'Extraction discarded' });
          },
          error: () =>
            this.messageService.add({ severity: 'error', summary: 'Failed to discard' }),
        });
        break;
      case 'reassign':
        this.reassignTarget.set(item);
        this.reassignVisible.set(true);
        break;
      case 'quick-discard':
        this.service.quickDiscard(item.id).subscribe({
          next: () => {
            this.store.removeFromQueue(item.id);
            this.messageService.add({ severity: 'success', summary: 'Removed from queue' });
          },
          error: () =>
            this.messageService.add({ severity: 'error', summary: 'Failed to discard' }),
        });
        break;
    }
  }

  onReassignApplied(request: ReassignCaptureRequest): void {
    const target = this.reassignTarget();
    if (!target) return;

    this.service.reassign(target.id, request).subscribe({
      next: (updated) => {
        this.store.replaceItem(updated);
        this.reassignVisible.set(false);
        this.messageService.add({ severity: 'success', summary: 'Capture reassigned' });
      },
      error: () =>
        this.messageService.add({ severity: 'error', summary: 'Failed to reassign' }),
    });
  }

  closeOutDay(): void {
    this.service.closeOutDay({}).subscribe({
      next: (summary) => {
        this.store.setLastSummary(summary);
        this.messageService.add({
          severity: 'success',
          summary: 'Day closed out',
          detail: `Confirmed ${summary.confirmedCount}, discarded ${summary.discardedCount}, remaining ${summary.remainingCount}.`,
        });
      },
      error: () =>
        this.messageService.add({ severity: 'error', summary: 'Failed to close out the day' }),
    });
  }
}
