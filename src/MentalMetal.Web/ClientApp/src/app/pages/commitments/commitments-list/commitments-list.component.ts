import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { CommitmentsService } from '../../../shared/services/commitments.service';
import { Commitment, CommitmentDirection, CommitmentStatus } from '../../../shared/models/commitment.model';
import { PeopleService } from '../../../shared/services/people.service';
import { Person } from '../../../shared/models/person.model';
import { CommitmentDialogComponent } from '../commitment-dialog/commitment-dialog.component';

@Component({
  selector: 'app-commitments-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, SelectModule, TableModule, TagModule, ToastModule, TooltipModule, CommitmentDialogComponent],
  providers: [MessageService],
  template: `
    <p-toast />
    <div class="flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">Commitments</h1>
        <p-button label="New Commitment" icon="pi pi-plus" (onClick)="showCreateDialog.set(true)" />
      </div>

      <div class="flex items-center gap-4 flex-wrap">
        <p-select
          [options]="directionFilterOptions"
          [ngModel]="selectedDirection()"
          (ngModelChange)="selectedDirection.set($event); onFilterChange()"
          placeholder="All Directions"
          [showClear]="true"
          class="w-48"
        />
        <p-select
          [options]="statusFilterOptions"
          [ngModel]="selectedStatus()"
          (ngModelChange)="selectedStatus.set($event); onFilterChange()"
          placeholder="All Statuses"
          [showClear]="true"
          class="w-48"
        />
        <p-select
          [options]="overdueFilterOptions"
          [ngModel]="selectedOverdue()"
          (ngModelChange)="selectedOverdue.set($event); onFilterChange()"
          placeholder="All"
          [showClear]="true"
          class="w-48"
        />
      </div>

      @if (loading()) {
        <div class="flex justify-center p-8">
          <i class="pi pi-spinner pi-spin text-2xl"></i>
        </div>
      } @else if (commitments().length === 0) {
        <div class="flex flex-col items-center gap-4 p-12">
          <i class="pi pi-check-square text-4xl text-muted-color"></i>
          <p class="text-muted-color">No commitments found. Create your first commitment to start tracking promises.</p>
        </div>
      } @else {
        <p-table
          [value]="commitments()"
          [rows]="20"
          [paginator]="commitments().length > 20"
          [rowHover]="true"
          styleClass="p-datatable-sm"
        >
          <ng-template #header>
            <tr>
              <th>Direction</th>
              <th>Description</th>
              <th>Person</th>
              <th>Status</th>
              <th>Due Date</th>
              <th>Actions</th>
            </tr>
          </ng-template>
          <ng-template #body let-commitment>
            <tr class="cursor-pointer" (click)="onRowClick(commitment)">
              <td>
                <p-tag [value]="formatDirection(commitment.direction)" [severity]="directionSeverity(commitment.direction)" />
              </td>
              <td class="font-medium">
                {{ commitment.description.length > 60 ? commitment.description.substring(0, 60) + '...' : commitment.description }}
              </td>
              <td class="text-muted-color text-sm">{{ personName(commitment.personId) }}</td>
              <td>
                <p-tag [value]="formatStatus(commitment.status)" [severity]="statusSeverity(commitment.status)" />
                @if (commitment.isOverdue) {
                  <p-tag value="Overdue" severity="danger" class="ml-1" />
                }
              </td>
              <td class="text-muted-color text-sm">
                @if (commitment.dueDate) {
                  {{ commitment.dueDate }}
                } @else {
                  -
                }
              </td>
              <td>
                <div class="flex gap-1" (click)="$event.stopPropagation()">
                  @if (commitment.status === 'Open') {
                    <p-button icon="pi pi-check" severity="success" [text]="true" size="small" pTooltip="Complete" (onClick)="onComplete(commitment)" />
                    <p-button icon="pi pi-times" severity="danger" [text]="true" size="small" pTooltip="Cancel" (onClick)="onCancel(commitment)" />
                  } @else {
                    <p-button icon="pi pi-replay" severity="info" [text]="true" size="small" pTooltip="Reopen" (onClick)="onReopen(commitment)" />
                  }
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      }

      <app-commitment-dialog
        [(visible)]="showCreateDialog"
        (created)="onCommitmentCreated($event)"
      />
    </div>
  `,
})
export class CommitmentsListComponent implements OnInit {
  private readonly commitmentsService = inject(CommitmentsService);
  private readonly peopleService = inject(PeopleService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  readonly commitments = signal<Commitment[]>([]);
  readonly loading = signal(true);
  readonly showCreateDialog = signal(false);
  private readonly peopleMap = signal<Map<string, string>>(new Map());

  readonly selectedDirection = signal<CommitmentDirection | null>(null);
  readonly selectedStatus = signal<CommitmentStatus | null>(null);
  readonly selectedOverdue = signal<boolean | null>(null);

  protected readonly directionFilterOptions = [
    { label: 'I owe them', value: 'MineToThem' as CommitmentDirection },
    { label: 'They owe me', value: 'TheirsToMe' as CommitmentDirection },
  ];

  protected readonly statusFilterOptions = [
    { label: 'Open', value: 'Open' as CommitmentStatus },
    { label: 'Completed', value: 'Completed' as CommitmentStatus },
    { label: 'Cancelled', value: 'Cancelled' as CommitmentStatus },
  ];

  protected readonly overdueFilterOptions = [
    { label: 'Overdue only', value: true },
  ];

  ngOnInit(): void {
    this.loadPeople();
    this.loadCommitments();
  }

  protected onFilterChange(): void {
    this.loadCommitments();
  }

  protected onRowClick(commitment: Commitment): void {
    this.router.navigate(['/commitments', commitment.id]);
  }

  protected onCommitmentCreated(_commitment: Commitment): void {
    this.loadCommitments();
  }

  protected personName(personId: string): string {
    return this.peopleMap().get(personId) ?? 'Unknown';
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

  protected onComplete(commitment: Commitment): void {
    this.commitmentsService.complete(commitment.id).subscribe({
      next: () => this.loadCommitments(),
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to complete commitment' }),
    });
  }

  protected onCancel(commitment: Commitment): void {
    this.commitmentsService.cancel(commitment.id).subscribe({
      next: () => this.loadCommitments(),
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to cancel commitment' }),
    });
  }

  protected onReopen(commitment: Commitment): void {
    this.commitmentsService.reopen(commitment.id).subscribe({
      next: () => this.loadCommitments(),
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to reopen commitment' }),
    });
  }

  private loadCommitments(): void {
    this.loading.set(true);
    this.commitmentsService.list(
      this.selectedDirection() ?? undefined,
      this.selectedStatus() ?? undefined,
      undefined,
      undefined,
      this.selectedOverdue() ?? undefined,
    ).subscribe({
      next: (commitments) => {
        this.commitments.set(commitments);
        this.loading.set(false);
      },
      error: () => {
        this.commitments.set([]);
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to load commitments' });
      },
    });
  }

  private loadPeople(): void {
    this.peopleService.list(undefined, true).subscribe({
      next: (people: Person[]) => {
        const map = new Map<string, string>();
        people.forEach(p => map.set(p.id, p.name));
        this.peopleMap.set(map);
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to load people' }),
    });
  }
}
