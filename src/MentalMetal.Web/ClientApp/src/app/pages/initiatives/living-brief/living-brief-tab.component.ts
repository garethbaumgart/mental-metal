import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { InitiativeBriefService } from '../../../shared/services/initiative-brief.service';
import {
  LivingBrief,
  PendingBriefUpdate,
  RiskSeverity,
} from '../../../shared/models/living-brief.model';

@Component({
  selector: 'app-living-brief-tab',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    TagModule,
    DialogModule,
    SelectModule,
    ToastModule,
  ],
  providers: [MessageService],
  template: `
    <p-toast />

    @if (loading()) {
      <div class="flex justify-center p-8"><i class="pi pi-spinner pi-spin text-2xl"></i></div>
    } @else if (brief()) {
      <div class="flex flex-col gap-6">

        <!-- Pending Updates Panel -->
        @if (pendingUpdates().length > 0) {
          <section class="border rounded p-4 flex flex-col gap-3" style="border-color: var(--p-surface-200)">
            <div class="flex items-center justify-between">
              <h3 class="text-lg font-semibold">
                Pending AI Updates
                <span class="ml-2 text-sm text-muted-color">({{ pendingUpdates().length }})</span>
              </h3>
              <p-button label="Refresh now" icon="pi pi-refresh" size="small" [loading]="refreshing()" (onClick)="refreshNow()" />
            </div>

            @for (update of pendingUpdates(); track update.id) {
              <div class="border rounded p-3 flex flex-col gap-2" style="border-color: var(--p-surface-200)">
                <div class="flex items-center gap-2">
                  <p-tag [value]="update.status" [severity]="statusSeverity(update.status)" />
                  @if (update.isStale) {
                    <p-tag value="Stale" severity="warn" />
                  }
                  <span class="text-xs text-muted-color ml-auto">{{ update.createdAt | date:'short' }}</span>
                </div>

                @if (update.status === 'Failed') {
                  <p class="text-sm" style="color: var(--p-red-500)">{{ friendlyFailure(update.failureReason) }}</p>
                } @else {
                  @if (update.proposal.proposedSummary) {
                    <div>
                      <div class="text-xs uppercase text-muted-color">Summary</div>
                      <p class="text-sm">{{ update.proposal.proposedSummary }}</p>
                    </div>
                  }
                  @if (update.proposal.newDecisions.length > 0) {
                    <div>
                      <div class="text-xs uppercase text-muted-color">New Decisions ({{ update.proposal.newDecisions.length }})</div>
                      <ul class="text-sm list-disc list-inside">
                        @for (d of update.proposal.newDecisions; track d.description) { <li>{{ d.description }}</li> }
                      </ul>
                    </div>
                  }
                  @if (update.proposal.newRisks.length > 0) {
                    <div>
                      <div class="text-xs uppercase text-muted-color">New Risks ({{ update.proposal.newRisks.length }})</div>
                      <ul class="text-sm list-disc list-inside">
                        @for (r of update.proposal.newRisks; track r.description) { <li>[{{ r.severity }}] {{ r.description }}</li> }
                      </ul>
                    </div>
                  }
                  @if (update.proposal.rationale) {
                    <p class="text-xs italic text-muted-color">{{ update.proposal.rationale }}</p>
                  }

                  @if (update.status === 'Pending' || update.status === 'Edited') {
                    <div class="flex gap-2 mt-2">
                      <p-button label="Apply" size="small" [disabled]="update.isStale" (onClick)="applyUpdate(update.id)" />
                      <p-button label="Reject" severity="secondary" size="small" (onClick)="rejectUpdate(update.id)" />
                    </div>
                  }
                }
              </div>
            }
          </section>
        } @else {
          <div class="flex justify-end">
            <p-button label="Refresh now" icon="pi pi-refresh" size="small" [loading]="refreshing()" (onClick)="refreshNow()" />
          </div>
        }

        <!-- Summary -->
        <section class="flex flex-col gap-2">
          <div class="flex items-center justify-between">
            <h3 class="text-lg font-semibold">Summary</h3>
            <p-button icon="pi pi-pencil" [text]="true" (onClick)="editSummaryDialog.set(true)" />
          </div>
          <p class="text-sm whitespace-pre-wrap">{{ brief()!.summary || 'No summary yet.' }}</p>
          <div class="text-xs text-muted-color">
            v{{ brief()!.briefVersion }}
            @if (brief()!.summaryLastRefreshedAt) {
              · last updated {{ brief()!.summaryLastRefreshedAt | date:'short' }} ({{ brief()!.summarySource }})
            }
          </div>
        </section>

        <!-- Decisions -->
        <section class="flex flex-col gap-2">
          <div class="flex items-center justify-between">
            <h3 class="text-lg font-semibold">Decisions ({{ brief()!.keyDecisions.length }})</h3>
            <p-button icon="pi pi-plus" [text]="true" (onClick)="logDecisionDialog.set(true)" />
          </div>
          @if (brief()!.keyDecisions.length === 0) {
            <p class="text-sm text-muted-color">No decisions logged yet.</p>
          } @else {
            <ul class="flex flex-col gap-2">
              @for (d of brief()!.keyDecisions; track d.id) {
                <li class="text-sm border-l-2 pl-3" style="border-color: var(--p-surface-300)">
                  <div class="flex items-center gap-2">
                    <span>{{ d.description }}</span>
                    <p-tag [value]="d.source" severity="info" />
                  </div>
                  @if (d.rationale) { <div class="text-xs text-muted-color">{{ d.rationale }}</div> }
                </li>
              }
            </ul>
          }
        </section>

        <!-- Risks -->
        <section class="flex flex-col gap-2">
          <div class="flex items-center justify-between">
            <h3 class="text-lg font-semibold">Open Risks ({{ openRisks().length }})</h3>
            <p-button icon="pi pi-plus" [text]="true" (onClick)="raiseRiskDialog.set(true)" />
          </div>
          @if (openRisks().length === 0) {
            <p class="text-sm text-muted-color">No open risks.</p>
          } @else {
            <ul class="flex flex-col gap-2">
              @for (r of openRisks(); track r.id) {
                <li class="text-sm flex items-center gap-2">
                  <p-tag [value]="r.severity" [severity]="riskSeverityStyle(r.severity)" />
                  <span class="flex-1">{{ r.description }}</span>
                  <p-button label="Resolve" size="small" [text]="true" (onClick)="resolveRisk(r.id)" />
                </li>
              }
            </ul>
          }
          @if (resolvedRisks().length > 0) {
            <details class="text-sm">
              <summary class="cursor-pointer text-muted-color">Resolved risks ({{ resolvedRisks().length }})</summary>
              <ul class="mt-2 flex flex-col gap-1">
                @for (r of resolvedRisks(); track r.id) {
                  <li class="text-muted-color">{{ r.description }}</li>
                }
              </ul>
            </details>
          }
        </section>

        <!-- Requirements History -->
        <section class="flex flex-col gap-2">
          <div class="flex items-center justify-between">
            <h3 class="text-lg font-semibold">Requirements</h3>
            <p-button icon="pi pi-plus" [text]="true" (onClick)="reqDialog.set(true)" />
          </div>
          @if (latestRequirements()) {
            <p class="text-sm whitespace-pre-wrap">{{ latestRequirements()!.content }}</p>
            @if (brief()!.requirementsHistory.length > 1) {
              <details class="text-sm">
                <summary class="cursor-pointer text-muted-color">History ({{ brief()!.requirementsHistory.length - 1 }})</summary>
                <ul class="mt-2 flex flex-col gap-2">
                  @for (s of olderRequirements(); track s.id) {
                    <li class="text-xs text-muted-color whitespace-pre-wrap">{{ s.capturedAt | date:'short' }}: {{ s.content }}</li>
                  }
                </ul>
              </details>
            }
          } @else {
            <p class="text-sm text-muted-color">No requirements snapshot yet.</p>
          }
        </section>

        <!-- Design Direction -->
        <section class="flex flex-col gap-2">
          <div class="flex items-center justify-between">
            <h3 class="text-lg font-semibold">Design Direction</h3>
            <p-button icon="pi pi-plus" [text]="true" (onClick)="designDialog.set(true)" />
          </div>
          @if (latestDesign()) {
            <p class="text-sm whitespace-pre-wrap">{{ latestDesign()!.content }}</p>
            @if (brief()!.designDirectionHistory.length > 1) {
              <details class="text-sm">
                <summary class="cursor-pointer text-muted-color">History ({{ brief()!.designDirectionHistory.length - 1 }})</summary>
                <ul class="mt-2 flex flex-col gap-2">
                  @for (s of olderDesign(); track s.id) {
                    <li class="text-xs text-muted-color whitespace-pre-wrap">{{ s.capturedAt | date:'short' }}: {{ s.content }}</li>
                  }
                </ul>
              </details>
            }
          } @else {
            <p class="text-sm text-muted-color">No design direction snapshot yet.</p>
          }
        </section>
      </div>
    }

    <!-- Dialogs -->
    <p-dialog header="Edit Summary" [(visible)]="editSummaryDialogValue" [modal]="true" [style]="{ width: '32rem' }">
      <textarea pInputTextarea [ngModel]="newSummary()" (ngModelChange)="newSummary.set($event)" rows="6" class="w-full"></textarea>
      <div class="flex justify-end gap-2 mt-3">
        <p-button label="Cancel" severity="secondary" (onClick)="editSummaryDialog.set(false)" />
        <p-button label="Save" (onClick)="saveSummary()" />
      </div>
    </p-dialog>

    <p-dialog header="Log Decision" [(visible)]="logDecisionDialogValue" [modal]="true" [style]="{ width: '32rem' }">
      <div class="flex flex-col gap-3">
        <input pInputText [ngModel]="newDecisionDescription()" (ngModelChange)="newDecisionDescription.set($event)" placeholder="Decision" />
        <textarea pInputTextarea [ngModel]="newDecisionRationale()" (ngModelChange)="newDecisionRationale.set($event)" placeholder="Rationale (optional)" rows="3"></textarea>
      </div>
      <div class="flex justify-end gap-2 mt-3">
        <p-button label="Cancel" severity="secondary" (onClick)="logDecisionDialog.set(false)" />
        <p-button label="Log" (onClick)="logDecision()" [disabled]="!newDecisionDescription().trim()" />
      </div>
    </p-dialog>

    <p-dialog header="Raise Risk" [(visible)]="raiseRiskDialogValue" [modal]="true" [style]="{ width: '32rem' }">
      <div class="flex flex-col gap-3">
        <input pInputText [ngModel]="newRiskDescription()" (ngModelChange)="newRiskDescription.set($event)" placeholder="Risk description" />
        <p-select [options]="severityOptions" [ngModel]="newRiskSeverity()" (ngModelChange)="newRiskSeverity.set($event)" placeholder="Severity" />
      </div>
      <div class="flex justify-end gap-2 mt-3">
        <p-button label="Cancel" severity="secondary" (onClick)="raiseRiskDialog.set(false)" />
        <p-button label="Raise" (onClick)="raiseRisk()" [disabled]="!newRiskDescription().trim()" />
      </div>
    </p-dialog>

    <p-dialog header="Snapshot Requirements" [(visible)]="reqDialogValue" [modal]="true" [style]="{ width: '36rem' }">
      <textarea pInputTextarea [ngModel]="newReqContent()" (ngModelChange)="newReqContent.set($event)" rows="8" class="w-full" placeholder="Full requirements text"></textarea>
      <div class="flex justify-end gap-2 mt-3">
        <p-button label="Cancel" severity="secondary" (onClick)="reqDialog.set(false)" />
        <p-button label="Save Snapshot" (onClick)="snapshotReq()" [disabled]="!newReqContent().trim()" />
      </div>
    </p-dialog>

    <p-dialog header="Snapshot Design Direction" [(visible)]="designDialogValue" [modal]="true" [style]="{ width: '36rem' }">
      <textarea pInputTextarea [ngModel]="newDesignContent()" (ngModelChange)="newDesignContent.set($event)" rows="8" class="w-full" placeholder="Full design direction text"></textarea>
      <div class="flex justify-end gap-2 mt-3">
        <p-button label="Cancel" severity="secondary" (onClick)="designDialog.set(false)" />
        <p-button label="Save Snapshot" (onClick)="snapshotDesign()" [disabled]="!newDesignContent().trim()" />
      </div>
    </p-dialog>
  `,
})
export class LivingBriefTabComponent {
  readonly initiativeId = input.required<string>();

  private readonly briefService = inject(InitiativeBriefService);
  private readonly toast = inject(MessageService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly refreshing = signal(false);
  readonly brief = signal<LivingBrief | null>(null);
  readonly pendingUpdates = signal<PendingBriefUpdate[]>([]);

  readonly openRisks = computed(() => this.brief()?.risks.filter(r => r.status === 'Open') ?? []);
  readonly resolvedRisks = computed(() => this.brief()?.risks.filter(r => r.status === 'Resolved') ?? []);
  readonly latestRequirements = computed(() => {
    const h = this.brief()?.requirementsHistory ?? [];
    return h.length > 0 ? h[h.length - 1] : null;
  });
  readonly olderRequirements = computed(() => {
    const h = this.brief()?.requirementsHistory ?? [];
    return h.slice(0, -1).reverse();
  });
  readonly latestDesign = computed(() => {
    const h = this.brief()?.designDirectionHistory ?? [];
    return h.length > 0 ? h[h.length - 1] : null;
  });
  readonly olderDesign = computed(() => {
    const h = this.brief()?.designDirectionHistory ?? [];
    return h.slice(0, -1).reverse();
  });

  readonly editSummaryDialog = signal(false);
  readonly logDecisionDialog = signal(false);
  readonly raiseRiskDialog = signal(false);
  readonly reqDialog = signal(false);
  readonly designDialog = signal(false);

  // Two-way bound mirrors for [(visible)] (PrimeNG p-dialog needs writable property)
  get editSummaryDialogValue() { return this.editSummaryDialog(); }
  set editSummaryDialogValue(v: boolean) { this.editSummaryDialog.set(v); }
  get logDecisionDialogValue() { return this.logDecisionDialog(); }
  set logDecisionDialogValue(v: boolean) { this.logDecisionDialog.set(v); }
  get raiseRiskDialogValue() { return this.raiseRiskDialog(); }
  set raiseRiskDialogValue(v: boolean) { this.raiseRiskDialog.set(v); }
  get reqDialogValue() { return this.reqDialog(); }
  set reqDialogValue(v: boolean) { this.reqDialog.set(v); }
  get designDialogValue() { return this.designDialog(); }
  set designDialogValue(v: boolean) { this.designDialog.set(v); }

  // Form state must be signal-backed to drive zoneless change detection (templates read these).
  readonly newSummary = signal('');
  readonly newDecisionDescription = signal('');
  readonly newDecisionRationale = signal('');
  readonly newRiskDescription = signal('');
  readonly newRiskSeverity = signal<RiskSeverity>('Medium');
  readonly newReqContent = signal('');
  readonly newDesignContent = signal('');

  readonly severityOptions = ['Low', 'Medium', 'High', 'Critical'];

  constructor() {
    effect(() => {
      const id = this.initiativeId();
      if (id) this.loadAll(id);
    });
    effect(() => {
      const b = this.brief();
      if (b) this.newSummary.set(b.summary ?? '');
    });
  }

  private loadAll(id: string) {
    this.loading.set(true);
    this.briefService.get(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: b => { this.brief.set(b); this.loading.set(false); },
      error: () => {
        this.loading.set(false);
        this.toast.add({ severity: 'error', summary: 'Failed to load brief', detail: 'Please try again.' });
      },
    });
    this.reloadPending();
  }

  private reloadPending() {
    this.briefService.listPending(this.initiativeId()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: list => this.pendingUpdates.set(list),
      error: () => {
        this.toast.add({ severity: 'error', summary: 'Failed to load pending updates' });
      },
    });
  }

  refreshNow() {
    this.refreshing.set(true);
    this.briefService.refresh(this.initiativeId()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.refreshing.set(false);
        this.toast.add({ severity: 'success', summary: 'Refresh requested' });
        // Refresh runs synchronously server-side; with livingBriefAutoApply=true the
        // initiative may already reflect the new version. Reload the brief itself so
        // the UI isn't showing stale summary/decisions/risks next to the new pending row.
        this.loadAll(this.initiativeId());
      },
      error: () => {
        this.refreshing.set(false);
        this.toast.add({ severity: 'error', summary: 'Refresh failed' });
      },
    });
  }

  saveSummary() {
    this.briefService.updateSummary(this.initiativeId(), { summary: this.newSummary() })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: b => { this.brief.set(b); this.editSummaryDialog.set(false); },
        error: () => this.toast.add({ severity: 'error', summary: 'Failed to save summary' }),
      });
  }

  logDecision() {
    this.briefService.logDecision(this.initiativeId(), {
      description: this.newDecisionDescription(),
      rationale: this.newDecisionRationale() || null,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: b => {
        this.brief.set(b);
        this.logDecisionDialog.set(false);
        this.newDecisionDescription.set('');
        this.newDecisionRationale.set('');
      },
      error: () => this.toast.add({ severity: 'error', summary: 'Failed to log decision' }),
    });
  }

  raiseRisk() {
    this.briefService.raiseRisk(this.initiativeId(), {
      description: this.newRiskDescription(),
      severity: this.newRiskSeverity(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: b => {
        this.brief.set(b);
        this.raiseRiskDialog.set(false);
        this.newRiskDescription.set('');
      },
      error: () => this.toast.add({ severity: 'error', summary: 'Failed to raise risk' }),
    });
  }

  resolveRisk(riskId: string) {
    this.briefService.resolveRisk(this.initiativeId(), riskId, { resolutionNote: null })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: b => this.brief.set(b),
        error: () => this.toast.add({ severity: 'error', summary: 'Failed to resolve risk' }),
      });
  }

  snapshotReq() {
    this.briefService.snapshotRequirements(this.initiativeId(), { content: this.newReqContent() })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: b => { this.brief.set(b); this.reqDialog.set(false); this.newReqContent.set(''); },
        error: () => this.toast.add({ severity: 'error', summary: 'Failed to save requirements snapshot' }),
      });
  }

  snapshotDesign() {
    this.briefService.snapshotDesignDirection(this.initiativeId(), { content: this.newDesignContent() })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: b => { this.brief.set(b); this.designDialog.set(false); this.newDesignContent.set(''); },
        error: () => this.toast.add({ severity: 'error', summary: 'Failed to save design direction snapshot' }),
      });
  }

  applyUpdate(updateId: string) {
    this.briefService.applyPending(this.initiativeId(), updateId)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: b => {
          this.brief.set(b);
          this.toast.add({ severity: 'success', summary: 'Update applied' });
          this.reloadPending();
        },
        error: err => {
          const msg = err?.status === 409 ? 'This proposal is stale — refresh to regenerate.' : 'Apply failed';
          this.toast.add({ severity: 'warn', summary: msg });
          this.reloadPending();
        },
      });
  }

  rejectUpdate(updateId: string) {
    this.briefService.rejectPending(this.initiativeId(), updateId, { reason: null })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => { this.toast.add({ severity: 'info', summary: 'Update rejected' }); this.reloadPending(); },
        error: err => {
          const msg = err?.status === 409
            ? 'This update is already in a terminal state.'
            : 'Failed to reject update';
          this.toast.add({ severity: 'warn', summary: msg });
          this.reloadPending();
        },
      });
  }

  statusSeverity(s: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (s) {
      case 'Pending': return 'info';
      case 'Edited': return 'warn';
      case 'Applied': return 'success';
      case 'Rejected': return 'secondary';
      case 'Failed': return 'danger';
      default: return 'secondary';
    }
  }

  riskSeverityStyle(s: RiskSeverity): 'success' | 'info' | 'warn' | 'danger' {
    switch (s) {
      case 'Low': return 'info';
      case 'Medium': return 'warn';
      case 'High': return 'danger';
      case 'Critical': return 'danger';
    }
  }

  friendlyFailure(reason: string | null | undefined): string {
    if (!reason) return 'Unknown error';
    if (reason.toLowerCase().includes('daily ai limit')) return 'Daily AI limit reached. Add your own AI provider key for unlimited access.';
    if (reason.toLowerCase().includes('ai provider')) return 'AI provider error. Try again in a moment.';
    return reason;
  }
}
