import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { CommitmentsService } from '../../../shared/services/commitments.service';
import { PeopleService } from '../../../shared/services/people.service';
import { InitiativesService } from '../../../shared/services/initiatives.service';
import { Commitment, CommitmentDirection, CommitmentStatus } from '../../../shared/models/commitment.model';
import { CommitmentDialogComponent } from '../commitment-dialog/commitment-dialog.component';

@Component({
  selector: 'app-commitment-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, ButtonModule, TagModule, ToastModule, CommitmentDialogComponent],
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
    } @else if (commitment()) {
      <div class="max-w-2xl mx-auto flex flex-col gap-8">
        <!-- Header -->
        <div class="flex items-center gap-4">
          <p-button icon="pi pi-arrow-left" [text]="true" (onClick)="goBack()" />
          <h1 class="text-2xl font-bold flex-1">{{ commitment()!.description }}</h1>
          <p-tag [value]="formatDirection(commitment()!.direction)" [severity]="directionSeverity(commitment()!.direction)" />
          <p-tag [value]="formatStatus(commitment()!.status)" [severity]="statusSeverity(commitment()!.status)" />
          @if (commitment()!.isOverdue) {
            <p-tag value="Overdue" severity="danger" />
          }
        </div>

        <!-- Actions -->
        <div class="flex gap-2">
          @if (commitment()!.status === 'Open') {
            <p-button label="Complete" icon="pi pi-check" severity="success" (onClick)="onComplete()" />
            <p-button label="Cancel" icon="pi pi-times" severity="danger" [outlined]="true" (onClick)="onCancel()" />
          } @else {
            <p-button label="Reopen" icon="pi pi-replay" severity="info" (onClick)="onReopen()" />
          }
          <p-button label="Edit" icon="pi pi-pencil" [outlined]="true" (onClick)="openEditDialog()" />
        </div>

        <!-- Details -->
        <section class="flex flex-col gap-4">
          <h2 class="text-xl font-semibold">Details</h2>
          <div class="flex flex-col gap-3">
            <div class="flex gap-4 py-2 border-b detail-row">
              <span class="text-sm font-medium text-muted-color w-32">Person</span>
              <span class="text-sm">{{ personName() }}</span>
            </div>
            <div class="flex gap-4 py-2 border-b detail-row">
              <span class="text-sm font-medium text-muted-color w-32">Direction</span>
              <span class="text-sm">{{ formatDirection(commitment()!.direction) }}</span>
            </div>
            <div class="flex gap-4 py-2 border-b detail-row">
              <span class="text-sm font-medium text-muted-color w-32">Due Date</span>
              <span class="text-sm">{{ commitment()!.dueDate || 'Not set' }}</span>
            </div>
            @if (commitment()!.initiativeId) {
              <div class="flex gap-4 py-2 border-b detail-row">
                <span class="text-sm font-medium text-muted-color w-32">Initiative</span>
                <span class="text-sm">{{ initiativeName() }}</span>
              </div>
            }
            @if (commitment()!.notes) {
              <div class="flex gap-4 py-2 border-b detail-row">
                <span class="text-sm font-medium text-muted-color w-32">Notes</span>
                <span class="text-sm whitespace-pre-wrap">{{ commitment()!.notes }}</span>
              </div>
            }
            @if (commitment()!.completedAt) {
              <div class="flex gap-4 py-2 border-b detail-row">
                <span class="text-sm font-medium text-muted-color w-32">Completed At</span>
                <span class="text-sm">{{ commitment()!.completedAt | date:'medium' }}</span>
              </div>
            }
            <div class="flex gap-4 py-2 border-b detail-row">
              <span class="text-sm font-medium text-muted-color w-32">Created</span>
              <span class="text-sm">{{ commitment()!.createdAt | date:'medium' }}</span>
            </div>
            <div class="flex gap-4 py-2">
              <span class="text-sm font-medium text-muted-color w-32">Updated</span>
              <span class="text-sm">{{ commitment()!.updatedAt | date:'medium' }}</span>
            </div>
          </div>
        </section>
      </div>

      <app-commitment-dialog
        [(visible)]="showEditDialog"
        [editCommitment]="commitment()!"
        (updated)="onUpdated($event)"
      />
    }
  `,
})
export class CommitmentDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly commitmentsService = inject(CommitmentsService);
  private readonly peopleService = inject(PeopleService);
  private readonly initiativesService = inject(InitiativesService);
  private readonly messageService = inject(MessageService);

  readonly commitment = signal<Commitment | null>(null);
  readonly loading = signal(true);
  readonly showEditDialog = signal(false);
  readonly personName = signal('Loading...');
  readonly initiativeName = signal('');

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadCommitment(id);
    }
  }

  protected goBack(): void {
    this.router.navigate(['/commitments']);
  }

  protected openEditDialog(): void {
    this.showEditDialog.set(true);
  }

  protected onUpdated(updated: Commitment): void {
    this.commitment.set(updated);
    this.messageService.add({ severity: 'success', summary: 'Commitment updated' });
  }

  protected onComplete(): void {
    const c = this.commitment();
    if (!c) return;
    this.commitmentsService.complete(c.id).subscribe({
      next: (updated) => {
        this.commitment.set(updated);
        this.messageService.add({ severity: 'success', summary: 'Commitment completed' });
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to complete' }),
    });
  }

  protected onCancel(): void {
    const c = this.commitment();
    if (!c) return;
    this.commitmentsService.cancel(c.id).subscribe({
      next: (updated) => {
        this.commitment.set(updated);
        this.messageService.add({ severity: 'success', summary: 'Commitment cancelled' });
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to cancel' }),
    });
  }

  protected onReopen(): void {
    const c = this.commitment();
    if (!c) return;
    this.commitmentsService.reopen(c.id).subscribe({
      next: (updated) => {
        this.commitment.set(updated);
        this.messageService.add({ severity: 'success', summary: 'Commitment reopened' });
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to reopen' }),
    });
  }

  protected formatDirection(direction: CommitmentDirection): string {
    switch (direction) {
      case 'MineToThem': return 'I owe them';
      case 'TheirsToMe': return 'They owe me';
    }
  }

  protected directionSeverity(direction: CommitmentDirection): 'info' | 'warn' {
    switch (direction) {
      case 'MineToThem': return 'warn';
      case 'TheirsToMe': return 'info';
    }
  }

  protected formatStatus(status: CommitmentStatus): string {
    return status;
  }

  protected statusSeverity(status: CommitmentStatus): 'info' | 'success' | 'secondary' {
    switch (status) {
      case 'Open': return 'info';
      case 'Completed': return 'success';
      case 'Cancelled': return 'secondary';
    }
  }

  private loadCommitment(id: string): void {
    this.loading.set(true);
    this.commitmentsService.get(id).subscribe({
      next: (commitment) => {
        this.commitment.set(commitment);
        this.loadPersonName(commitment.personId);
        if (commitment.initiativeId) {
          this.loadInitiativeName(commitment.initiativeId);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.router.navigate(['/commitments']);
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
