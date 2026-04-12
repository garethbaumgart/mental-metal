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
import { DelegationsService } from '../../../shared/services/delegations.service';
import { Delegation, DelegationPriority, DelegationStatus } from '../../../shared/models/delegation.model';
import { PeopleService } from '../../../shared/services/people.service';
import { Person } from '../../../shared/models/person.model';
import { DelegationDialogComponent } from '../delegation-dialog/delegation-dialog.component';

@Component({
  selector: 'app-delegations-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, SelectModule, TableModule, TagModule, ToastModule, TooltipModule, DelegationDialogComponent],
  providers: [MessageService],
  template: `
    <p-toast />
    <div class="flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">Delegations</h1>
        <p-button label="New Delegation" icon="pi pi-plus" (onClick)="showCreateDialog.set(true)" />
      </div>

      <div class="flex items-center gap-4 flex-wrap">
        <p-select
          [options]="statusFilterOptions"
          [ngModel]="selectedStatus()"
          (ngModelChange)="selectedStatus.set($event); onFilterChange()"
          placeholder="All Statuses"
          [showClear]="true"
          class="w-48"
        />
        <p-select
          [options]="priorityFilterOptions"
          [ngModel]="selectedPriority()"
          (ngModelChange)="selectedPriority.set($event); onFilterChange()"
          placeholder="All Priorities"
          [showClear]="true"
          class="w-48"
        />
      </div>

      @if (loading()) {
        <div class="flex justify-center p-8">
          <i class="pi pi-spinner pi-spin text-2xl"></i>
        </div>
      } @else if (delegations().length === 0) {
        <div class="flex flex-col items-center gap-4 p-12">
          <i class="pi pi-send text-4xl text-muted-color"></i>
          <p class="text-muted-color">No delegations found. Create your first delegation to start tracking assigned work.</p>
        </div>
      } @else {
        <p-table
          [value]="delegations()"
          [rows]="20"
          [paginator]="delegations().length > 20"
          [rowHover]="true"
          styleClass="p-datatable-sm"
        >
          <ng-template #header>
            <tr>
              <th>Description</th>
              <th>Delegate</th>
              <th>Status</th>
              <th>Priority</th>
              <th>Due Date</th>
              <th>Last Follow-up</th>
              <th>Actions</th>
            </tr>
          </ng-template>
          <ng-template #body let-delegation>
            <tr class="cursor-pointer" (click)="onRowClick(delegation)">
              <td class="font-medium">
                {{ delegation.description.length > 50 ? delegation.description.substring(0, 50) + '...' : delegation.description }}
              </td>
              <td class="text-muted-color text-sm">{{ personName(delegation.delegatePersonId) }}</td>
              <td>
                <p-tag [value]="formatStatus(delegation.status)" [severity]="statusSeverity(delegation.status)" />
              </td>
              <td>
                <p-tag [value]="delegation.priority" [severity]="prioritySeverity(delegation.priority)" />
              </td>
              <td class="text-muted-color text-sm">
                @if (delegation.dueDate) {
                  {{ delegation.dueDate }}
                } @else {
                  -
                }
              </td>
              <td class="text-muted-color text-sm">
                @if (delegation.lastFollowedUpAt) {
                  {{ formatDate(delegation.lastFollowedUpAt) }}
                } @else {
                  -
                }
              </td>
              <td>
                <div class="flex gap-1" (click)="$event.stopPropagation()">
                  @switch (delegation.status) {
                    @case ('Assigned') {
                      <p-button icon="pi pi-play" severity="info" [text]="true" size="small" pTooltip="Start" (onClick)="onStart(delegation)" />
                      <p-button icon="pi pi-check" severity="success" [text]="true" size="small" pTooltip="Complete" (onClick)="onComplete(delegation)" />
                    }
                    @case ('InProgress') {
                      <p-button icon="pi pi-check" severity="success" [text]="true" size="small" pTooltip="Complete" (onClick)="onComplete(delegation)" />
                      <p-button icon="pi pi-ban" severity="warn" [text]="true" size="small" pTooltip="Block" (onClick)="onBlock(delegation)" />
                    }
                    @case ('Blocked') {
                      <p-button icon="pi pi-lock-open" severity="info" [text]="true" size="small" pTooltip="Unblock" (onClick)="onUnblock(delegation)" />
                      <p-button icon="pi pi-check" severity="success" [text]="true" size="small" pTooltip="Complete" (onClick)="onComplete(delegation)" />
                    }
                  }
                  @if (delegation.status !== 'Completed') {
                    <p-button icon="pi pi-phone" severity="secondary" [text]="true" size="small" pTooltip="Follow Up" (onClick)="onFollowUp(delegation)" />
                  }
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      }

      <app-delegation-dialog
        [(visible)]="showCreateDialog"
        (created)="onDelegationCreated($event)"
      />
    </div>
  `,
})
export class DelegationsListComponent implements OnInit {
  private readonly delegationsService = inject(DelegationsService);
  private readonly peopleService = inject(PeopleService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  readonly delegations = signal<Delegation[]>([]);
  readonly loading = signal(true);
  readonly showCreateDialog = signal(false);
  private readonly peopleMap = signal<Map<string, string>>(new Map());

  readonly selectedStatus = signal<DelegationStatus | null>(null);
  readonly selectedPriority = signal<DelegationPriority | null>(null);

  protected readonly statusFilterOptions = [
    { label: 'Assigned', value: 'Assigned' as DelegationStatus },
    { label: 'In Progress', value: 'InProgress' as DelegationStatus },
    { label: 'Completed', value: 'Completed' as DelegationStatus },
    { label: 'Blocked', value: 'Blocked' as DelegationStatus },
  ];

  protected readonly priorityFilterOptions = [
    { label: 'Low', value: 'Low' as DelegationPriority },
    { label: 'Medium', value: 'Medium' as DelegationPriority },
    { label: 'High', value: 'High' as DelegationPriority },
    { label: 'Urgent', value: 'Urgent' as DelegationPriority },
  ];

  ngOnInit(): void {
    this.loadPeople();
    this.loadDelegations();
  }

  protected onFilterChange(): void {
    this.loadDelegations();
  }

  protected onRowClick(delegation: Delegation): void {
    this.router.navigate(['/delegations', delegation.id]);
  }

  protected onDelegationCreated(_delegation: Delegation): void {
    this.loadDelegations();
  }

  protected personName(personId: string): string {
    return this.peopleMap().get(personId) ?? 'Unknown';
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

  protected formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString();
  }

  protected onStart(delegation: Delegation): void {
    this.delegationsService.start(delegation.id).subscribe({
      next: () => this.loadDelegations(),
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to start delegation' }),
    });
  }

  protected onComplete(delegation: Delegation): void {
    this.delegationsService.complete(delegation.id).subscribe({
      next: () => this.loadDelegations(),
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to complete delegation' }),
    });
  }

  protected onBlock(delegation: Delegation): void {
    this.delegationsService.block(delegation.id, { reason: 'Blocked' }).subscribe({
      next: () => this.loadDelegations(),
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to block delegation' }),
    });
  }

  protected onUnblock(delegation: Delegation): void {
    this.delegationsService.unblock(delegation.id).subscribe({
      next: () => this.loadDelegations(),
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to unblock delegation' }),
    });
  }

  protected onFollowUp(delegation: Delegation): void {
    this.delegationsService.followUp(delegation.id).subscribe({
      next: () => {
        this.loadDelegations();
        this.messageService.add({ severity: 'success', summary: 'Follow-up recorded' });
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Failed to record follow-up' }),
    });
  }

  private loadDelegations(): void {
    this.loading.set(true);
    this.delegationsService.list(
      this.selectedStatus() ?? undefined,
      this.selectedPriority() ?? undefined,
    ).subscribe({
      next: (delegations) => {
        this.delegations.set(delegations);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to load delegations' });
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
