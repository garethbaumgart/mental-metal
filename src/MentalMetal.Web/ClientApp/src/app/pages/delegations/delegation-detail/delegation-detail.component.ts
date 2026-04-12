import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { DialogModule } from 'primeng/dialog';
import { MessageService } from 'primeng/api';
import { DelegationsService } from '../../../shared/services/delegations.service';
import { PeopleService } from '../../../shared/services/people.service';
import { InitiativesService } from '../../../shared/services/initiatives.service';
import { Delegation, DelegationPriority, DelegationStatus } from '../../../shared/models/delegation.model';
import { DelegationDialogComponent } from '../delegation-dialog/delegation-dialog.component';

@Component({
  selector: 'app-delegation-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, FormsModule, ButtonModule, TagModule, ToastModule, InputTextModule, TextareaModule, DialogModule, DelegationDialogComponent],
  providers: [MessageService],
  styles: [`
    .detail-row {
      border-color: var(--p-surface-200);
    }
  `],
  template: `
    <p-toast />

    @if (loading()) {
      <div class="flex justify-center p-8">
        <i class="pi pi-spinner pi-spin text-2xl"></i>
      </div>
    } @else if (delegation()) {
      <div class="max-w-2xl mx-auto flex flex-col gap-8">
        <!-- Header -->
        <div class="flex items-center gap-4">
          <p-button icon="pi pi-arrow-left" [text]="true" (onClick)="goBack()" />
          <h1 class="text-2xl font-bold flex-1">{{ delegation()!.description }}</h1>
          <p-tag [value]="formatStatus(delegation()!.status)" [severity]="statusSeverity(delegation()!.status)" />
          <p-tag [value]="delegation()!.priority" [severity]="prioritySeverity(delegation()!.priority)" />
        </div>

        <!-- Actions -->
        <div class="flex gap-2 flex-wrap">
          @switch (delegation()!.status) {
            @case ('Assigned') {
              <p-button label="Start" icon="pi pi-play" severity="info" (onClick)="onStart()" />
              <p-button label="Complete" icon="pi pi-check" severity="success" (onClick)="onComplete()" />
              <p-button label="Block" icon="pi pi-ban" severity="warn" [outlined]="true" (onClick)="showBlockDialog.set(true)" />
            }
            @case ('InProgress') {
              <p-button label="Complete" icon="pi pi-check" severity="success" (onClick)="onComplete()" />
              <p-button label="Block" icon="pi pi-ban" severity="warn" [outlined]="true" (onClick)="showBlockDialog.set(true)" />
            }
            @case ('Blocked') {
              <p-button label="Unblock" icon="pi pi-lock-open" severity="info" (onClick)="onUnblock()" />
              <p-button label="Complete" icon="pi pi-check" severity="success" (onClick)="onComplete()" />
            }
          }
          @if (delegation()!.status !== 'Completed') {
            <p-button label="Follow Up" icon="pi pi-phone" [outlined]="true" (onClick)="showFollowUpDialog.set(true)" />
          }
          <p-button label="Edit" icon="pi pi-pencil" [outlined]="true" (onClick)="openEditDialog()" />
        </div>

        <!-- Details -->
        <section class="flex flex-col gap-4">
          <h2 class="text-xl font-semibold">Details</h2>
          <div class="flex flex-col gap-3">
            <div class="flex gap-4 py-2 border-b detail-row">
              <span class="text-sm font-medium text-muted-color w-36">Delegated to</span>
              <span class="text-sm">{{ personName() }}</span>
            </div>
            <div class="flex gap-4 py-2 border-b detail-row">
              <span class="text-sm font-medium text-muted-color w-36">Priority</span>
              <span class="text-sm">{{ delegation()!.priority }}</span>
            </div>
            <div class="flex gap-4 py-2 border-b detail-row">
              <span class="text-sm font-medium text-muted-color w-36">Due Date</span>
              <span class="text-sm">{{ delegation()!.dueDate || 'Not set' }}</span>
            </div>
            @if (delegation()!.initiativeId) {
              <div class="flex gap-4 py-2 border-b detail-row">
                <span class="text-sm font-medium text-muted-color w-36">Initiative</span>
                <span class="text-sm">{{ initiativeName() }}</span>
              </div>
            }
            @if (delegation()!.notes) {
              <div class="flex gap-4 py-2 border-b detail-row">
                <span class="text-sm font-medium text-muted-color w-36">Notes</span>
                <span class="text-sm whitespace-pre-wrap">{{ delegation()!.notes }}</span>
              </div>
            }
            @if (delegation()!.lastFollowedUpAt) {
              <div class="flex gap-4 py-2 border-b detail-row">
                <span class="text-sm font-medium text-muted-color w-36">Last Follow-up</span>
                <span class="text-sm">{{ delegation()!.lastFollowedUpAt | date:'medium' }}</span>
              </div>
            }
            @if (delegation()!.completedAt) {
              <div class="flex gap-4 py-2 border-b detail-row">
                <span class="text-sm font-medium text-muted-color w-36">Completed At</span>
                <span class="text-sm">{{ delegation()!.completedAt | date:'medium' }}</span>
              </div>
            }
            <div class="flex gap-4 py-2 border-b detail-row">
              <span class="text-sm font-medium text-muted-color w-36">Created</span>
              <span class="text-sm">{{ delegation()!.createdAt | date:'medium' }}</span>
            </div>
            <div class="flex gap-4 py-2">
              <span class="text-sm font-medium text-muted-color w-36">Updated</span>
              <span class="text-sm">{{ delegation()!.updatedAt | date:'medium' }}</span>
            </div>
          </div>
        </section>
      </div>

      <app-delegation-dialog
        [(visible)]="showEditDialog"
        [editDelegation]="delegation()!"
        (updated)="onUpdated($event)"
      />

      <!-- Block reason dialog -->
      <p-dialog
        header="Block Delegation"
        [visible]="showBlockDialog()"
        (visibleChange)="showBlockDialog.set($event)"
        [modal]="true"
        [style]="{ width: '28rem' }"
      >
        <div class="flex flex-col gap-4 pt-4">
          <label for="blockReason" class="text-sm font-medium text-muted-color">Reason *</label>
          <textarea pTextarea id="blockReason" [(ngModel)]="blockReason" [rows]="3" class="w-full" placeholder="Why is this blocked?"></textarea>
        </div>
        <ng-template #footer>
          <div class="flex justify-end gap-2">
            <p-button label="Cancel" severity="secondary" (onClick)="showBlockDialog.set(false)" />
            <p-button label="Block" icon="pi pi-ban" severity="warn" (onClick)="onBlock()" [disabled]="!blockReason.trim()" />
          </div>
        </ng-template>
      </p-dialog>

      <!-- Follow-up dialog -->
      <p-dialog
        header="Record Follow-up"
        [visible]="showFollowUpDialog()"
        (visibleChange)="showFollowUpDialog.set($event)"
        [modal]="true"
        [style]="{ width: '28rem' }"
      >
        <div class="flex flex-col gap-4 pt-4">
          <label for="followUpNotes" class="text-sm font-medium text-muted-color">Notes (optional)</label>
          <textarea pTextarea id="followUpNotes" [(ngModel)]="followUpNotes" [rows]="3" class="w-full" placeholder="Follow-up notes..."></textarea>
        </div>
        <ng-template #footer>
          <div class="flex justify-end gap-2">
            <p-button label="Cancel" severity="secondary" (onClick)="showFollowUpDialog.set(false)" />
            <p-button label="Record" icon="pi pi-phone" (onClick)="onFollowUp()" />
          </div>
        </ng-template>
      </p-dialog>
    }
  `,
})
export class DelegationDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly delegationsService = inject(DelegationsService);
  private readonly peopleService = inject(PeopleService);
  private readonly initiativesService = inject(InitiativesService);
  private readonly messageService = inject(MessageService);

  readonly delegation = signal<Delegation | null>(null);
  readonly loading = signal(true);
  readonly showEditDialog = signal(false);
  readonly showBlockDialog = signal(false);
  readonly showFollowUpDialog = signal(false);
  readonly personName = signal('Loading...');
  readonly initiativeName = signal('');

  protected blockReason = '';
  protected followUpNotes = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadDelegation(id);
    } else {
      this.loading.set(false);
      this.router.navigate(['/delegations']);
    }
  }

  protected goBack(): void {
    this.router.navigate(['/delegations']);
  }

  protected openEditDialog(): void {
    this.showEditDialog.set(true);
  }

  protected onUpdated(updated: Delegation): void {
    this.delegation.set(updated);
    this.messageService.add({ severity: 'success', summary: 'Delegation updated' });
  }

  protected onStart(): void {
    const d = this.delegation();
    if (!d) return;
    this.delegationsService.start(d.id).subscribe({
      next: (updated) => {
        this.delegation.set(updated);
        this.messageService.add({ severity: 'success', summary: 'Delegation started' });
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to start' }),
    });
  }

  protected onComplete(): void {
    const d = this.delegation();
    if (!d) return;
    this.delegationsService.complete(d.id).subscribe({
      next: (updated) => {
        this.delegation.set(updated);
        this.messageService.add({ severity: 'success', summary: 'Delegation completed' });
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to complete' }),
    });
  }

  protected onBlock(): void {
    const d = this.delegation();
    if (!d || !this.blockReason.trim()) return;
    this.delegationsService.block(d.id, { reason: this.blockReason.trim() }).subscribe({
      next: (updated) => {
        this.delegation.set(updated);
        this.showBlockDialog.set(false);
        this.blockReason = '';
        this.messageService.add({ severity: 'success', summary: 'Delegation blocked' });
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to block' }),
    });
  }

  protected onUnblock(): void {
    const d = this.delegation();
    if (!d) return;
    this.delegationsService.unblock(d.id).subscribe({
      next: (updated) => {
        this.delegation.set(updated);
        this.messageService.add({ severity: 'success', summary: 'Delegation unblocked' });
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to unblock' }),
    });
  }

  protected onFollowUp(): void {
    const d = this.delegation();
    if (!d) return;
    this.delegationsService.followUp(d.id, {
      notes: this.followUpNotes.trim() || undefined,
    }).subscribe({
      next: (updated) => {
        this.delegation.set(updated);
        this.showFollowUpDialog.set(false);
        this.followUpNotes = '';
        this.messageService.add({ severity: 'success', summary: 'Follow-up recorded' });
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to record follow-up' }),
    });
  }

  protected formatStatus(status: DelegationStatus): string {
    switch (status) {
      case 'InProgress': return 'In Progress';
      default: return status;
    }
  }

  protected statusSeverity(status: DelegationStatus): 'info' | 'success' | 'warn' | 'secondary' {
    switch (status) {
      case 'Assigned': return 'info';
      case 'InProgress': return 'warn';
      case 'Completed': return 'success';
      case 'Blocked': return 'secondary';
    }
  }

  protected prioritySeverity(priority: DelegationPriority): 'info' | 'success' | 'warn' | 'danger' {
    switch (priority) {
      case 'Low': return 'info';
      case 'Medium': return 'success';
      case 'High': return 'warn';
      case 'Urgent': return 'danger';
    }
  }

  private loadDelegation(id: string): void {
    this.loading.set(true);
    this.delegationsService.get(id).subscribe({
      next: (delegation) => {
        this.delegation.set(delegation);
        this.loadPersonName(delegation.delegatePersonId);
        if (delegation.initiativeId) {
          this.loadInitiativeName(delegation.initiativeId);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.router.navigate(['/delegations']);
      },
    });
  }

  private loadPersonName(personId: string): void {
    this.peopleService.get(personId).subscribe({
      next: (person) => this.personName.set(person.name),
      error: () => this.personName.set('Unknown'),
    });
  }

  private loadInitiativeName(initiativeId: string): void {
    this.initiativesService.get(initiativeId).subscribe({
      next: (initiative) => this.initiativeName.set(initiative.title),
      error: () => this.initiativeName.set('Unknown'),
    });
  }
}
