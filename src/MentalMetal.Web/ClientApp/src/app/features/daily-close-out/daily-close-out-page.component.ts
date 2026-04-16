import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
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
    RouterLink,
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
          <div class="flex items-center gap-2 flex-wrap">
            <p-button
              label="Process all raw"
              icon="pi pi-sparkles"
              severity="secondary"
              [outlined]="true"
              [disabled]="!hasRaw() || bulkProcessing()"
              [loading]="bulkProcessing()"
              (onClick)="processAllRaw()"
            />
            <p-button
              label="Close out the day"
              icon="pi pi-flag"
              [disabled]="store.isLoading() || bulkProcessing()"
              (onClick)="closeOutDay()"
            />
          </div>
        </div>
        @if (providerNotConfigured()) {
          <div class="flex flex-col items-start gap-1 pt-3 border-t">
            <p class="text-sm text-muted-color">
              Configure your AI provider to process captures.
            </p>
            <a
              routerLink="/settings"
              class="text-sm font-medium text-primary hover:underline"
            >Open settings</a>
          </div>
        }
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
            <app-triage-card
              [capture]="item"
              [processing]="processingIds().has(item.id)"
              (action)="onAction(item, $event)"
            />
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
  protected readonly bulkProcessing = signal(false);
  protected readonly providerNotConfigured = signal(false);
  /** capture IDs currently being processed (per-row or bulk). */
  protected readonly processingIds = signal<ReadonlySet<string>>(new Set());
  protected readonly hasRaw = computed(() =>
    this.store.items().some((c) => c.processingStatus === 'Raw'),
  );

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
      case 'process':
        this.processOne(item.id);
        break;
    }
  }

  private processOne(id: string): void {
    // A prior per-row or bulk attempt may have surfaced the
    // provider-not-configured block. Clear it before retrying so the
    // block doesn't remain on-screen after the user (presumably)
    // opened /settings and fixed their key.
    this.providerNotConfigured.set(false);
    this.markProcessing(id, true);
    this.capturesService.process(id).subscribe({
      next: () => {
        this.markProcessing(id, false);
        this.store.refreshQueue();
      },
      error: (err) => {
        this.markProcessing(id, false);
        const code = (err?.error as { code?: string } | null)?.code;
        if (err?.status === 409 && code === 'ai_provider_not_configured') {
          this.providerNotConfigured.set(true);
          return;
        }
        this.messageService.add({ severity: 'error', summary: 'Failed to process' });
      },
    });
  }

  private markProcessing(id: string, inFlight: boolean): void {
    this.processingIds.update((set) => {
      const next = new Set(set);
      if (inFlight) next.add(id);
      else next.delete(id);
      return next;
    });
  }

  async processAllRaw(): Promise<void> {
    if (this.bulkProcessing() || !this.hasRaw()) return;
    this.bulkProcessing.set(true);
    this.providerNotConfigured.set(false);

    const rawIds = this.store.items()
      .filter((c) => c.processingStatus === 'Raw')
      .map((c) => c.id);
    // Mark all as in-flight so each card shows a spinner alongside its Process button.
    this.processingIds.update((set) => {
      const next = new Set(set);
      for (const id of rawIds) next.add(id);
      return next;
    });

    try {
      const result = await this.service.processAllRaw(
        rawIds,
        3,
        // Refresh the queue after each item so the card's status badge
        // and the progress counts (Raw/Processing/Processed/Failed)
        // flip live during a long batch — not just at the end.
        (id) => {
          this.markProcessing(id, false);
          this.store.refreshQueue();
        },
      );
      if (result.providerNotConfigured) {
        this.providerNotConfigured.set(true);
      } else {
        this.messageService.add({
          severity: result.failed > 0 ? 'warn' : 'success',
          summary: `Processed ${result.succeeded} of ${result.attempted} · ${result.failed} failed`,
        });
      }
    } finally {
      this.processingIds.update(() => new Set());
      this.bulkProcessing.set(false);
      this.store.refreshQueue();
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
