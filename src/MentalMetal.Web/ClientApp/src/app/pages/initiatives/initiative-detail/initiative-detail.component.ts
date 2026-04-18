import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { InitiativesService } from '../../../shared/services/initiatives.service';
import { Initiative, InitiativeStatus } from '../../../shared/models/initiative.model';

@Component({
  selector: 'app-initiative-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    DatePipe,
    ButtonModule,
    InputTextModule,
    TagModule,
    ToastModule,
    ConfirmDialogModule,
  ],
  providers: [MessageService, ConfirmationService],
  template: `
    <p-toast />
    <p-confirmDialog />

    @if (loading()) {
      <div class="flex justify-center p-8">
        <i class="pi pi-spinner pi-spin text-2xl"></i>
      </div>
    } @else if (initiative()) {
      <div class="max-w-2xl mx-auto flex flex-col gap-8">
        <!-- Header -->
        <div class="flex items-center gap-4">
          <p-button icon="pi pi-arrow-left" [text]="true" (onClick)="goBack()" />
          <h1 class="text-2xl font-bold flex-1">{{ initiative()!.title }}</h1>
          <p-tag
            [value]="formatStatus(initiative()!.status)"
            [severity]="statusSeverity(initiative()!.status)"
          />
        </div>

        <!-- Title Section -->
        <section class="flex flex-col gap-4">
          <h2 class="text-xl font-semibold">Title</h2>
          <div class="flex items-end gap-4">
            <div class="flex flex-col gap-2 flex-1">
              <label for="title" class="text-sm font-medium text-muted-color">Initiative Title</label>
              <input pInputText id="title" [(ngModel)]="title" class="w-full" />
            </div>
            <p-button
              label="Save"
              (onClick)="saveTitle()"
              [loading]="savingTitle()"
              [disabled]="!title.trim() || title.trim() === initiative()!.title"
            />
          </div>
        </section>

        <!-- Status Transitions -->
        @if (statusActions().length > 0) {
          <section class="flex flex-col gap-4">
            <h2 class="text-xl font-semibold">Status</h2>
            <div class="flex items-center gap-3">
              @for (action of statusActions(); track action.status) {
                <p-button
                  [label]="action.label"
                  [severity]="action.severity"
                  [outlined]="true"
                  (onClick)="changeStatus(action.status)"
                  [loading]="changingStatus()"
                />
              }
            </div>
          </section>
        }

        <!-- Auto Summary -->
        <section class="flex flex-col gap-4">
          <div class="flex items-center justify-between">
            <h2 class="text-xl font-semibold">Auto Summary</h2>
            <p-button
              label="Refresh"
              icon="pi pi-refresh"
              [outlined]="true"
              size="small"
              (onClick)="refreshSummary()"
              [loading]="refreshingSummary()"
            />
          </div>
          @if (initiative()!.autoSummary) {
            <p class="text-sm whitespace-pre-wrap">{{ initiative()!.autoSummary }}</p>
            @if (initiative()!.lastSummaryRefreshedAt) {
              <p class="text-xs text-muted-color">Last refreshed: {{ initiative()!.lastSummaryRefreshedAt | date:'medium' }}</p>
            }
          } @else {
            <p class="text-muted-color text-sm">No summary yet. Link captures to this initiative and refresh to generate one.</p>
          }
        </section>
      </div>
    }
  `,
})
export class InitiativeDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly initiativesService = inject(InitiativesService);
  private readonly messageService = inject(MessageService);

  readonly initiative = signal<Initiative | null>(null);
  readonly loading = signal(true);
  readonly savingTitle = signal(false);
  readonly changingStatus = signal(false);
  readonly refreshingSummary = signal(false);
  readonly statusActions = signal<StatusAction[]>([]);

  protected title = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadInitiative(id);
    }
  }

  protected goBack(): void {
    this.router.navigate(['/initiatives']);
  }

  protected saveTitle(): void {
    const i = this.initiative();
    if (!i || !this.title.trim()) return;

    this.savingTitle.set(true);
    this.initiativesService.updateTitle(i.id, { title: this.title.trim() }).subscribe({
      next: (updated) => {
        this.initiative.set(updated);
        this.title = updated.title;
        this.savingTitle.set(false);
        this.messageService.add({ severity: 'success', summary: 'Title updated' });
      },
      error: () => {
        this.savingTitle.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to update title' });
      },
    });
  }

  protected changeStatus(newStatus: InitiativeStatus): void {
    const i = this.initiative();
    if (!i) return;

    this.changingStatus.set(true);
    this.initiativesService.changeStatus(i.id, newStatus).subscribe({
      next: (updated) => {
        this.initiative.set(updated);
        this.updateStatusActions(updated.status);
        this.changingStatus.set(false);
        this.messageService.add({ severity: 'success', summary: 'Status changed' });
      },
      error: () => {
        this.changingStatus.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to change status' });
      },
    });
  }

  protected refreshSummary(): void {
    const i = this.initiative();
    if (!i) return;

    this.refreshingSummary.set(true);
    this.initiativesService.refreshSummary(i.id).subscribe({
      next: (updated) => {
        this.initiative.set(updated);
        this.refreshingSummary.set(false);
        this.messageService.add({ severity: 'success', summary: 'Summary refreshed' });
      },
      error: () => {
        this.refreshingSummary.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to refresh summary' });
      },
    });
  }

  protected formatStatus(status: InitiativeStatus): string {
    switch (status) {
      case 'Active': return 'Active';
      case 'OnHold': return 'On Hold';
      case 'Completed': return 'Completed';
      case 'Cancelled': return 'Cancelled';
    }
  }

  protected statusSeverity(status: InitiativeStatus): 'success' | 'warn' | 'info' | 'danger' {
    switch (status) {
      case 'Active': return 'success';
      case 'OnHold': return 'warn';
      case 'Completed': return 'info';
      case 'Cancelled': return 'danger';
    }
  }

  private loadInitiative(id: string): void {
    this.loading.set(true);
    this.initiativesService.get(id).subscribe({
      next: (initiative) => {
        this.initiative.set(initiative);
        this.title = initiative.title;
        this.updateStatusActions(initiative.status);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.router.navigate(['/initiatives']);
      },
    });
  }

  private updateStatusActions(status: InitiativeStatus): void {
    switch (status) {
      case 'Active':
        this.statusActions.set([
          { label: 'Put On Hold', status: 'OnHold', severity: 'warn' },
          { label: 'Complete', status: 'Completed', severity: 'info' },
          { label: 'Cancel', status: 'Cancelled', severity: 'danger' },
        ]);
        break;
      case 'OnHold':
        this.statusActions.set([
          { label: 'Resume', status: 'Active', severity: 'success' },
        ]);
        break;
      default:
        this.statusActions.set([]);
        break;
    }
  }
}

interface StatusAction {
  label: string;
  status: InitiativeStatus;
  severity: 'success' | 'warn' | 'info' | 'danger';
}
