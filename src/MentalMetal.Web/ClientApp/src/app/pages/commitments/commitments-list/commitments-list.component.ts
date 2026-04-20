import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AvatarModule } from 'primeng/avatar';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { CommitmentsService } from '../../../shared/services/commitments.service';
import {
  Commitment,
  CommitmentDirection,
  CommitmentStatus,
} from '../../../shared/models/commitment.model';
import { PeopleService } from '../../../shared/services/people.service';
import { Person } from '../../../shared/models/person.model';

interface PersonGroup {
  personId: string;
  personName: string;
  initials: string;
  commitments: Commitment[];
  overdueCount: number;
  totalCount: number;
}

@Component({
  selector: 'app-commitments-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, AvatarModule, ButtonModule, SelectModule, TagModule, ToastModule, TooltipModule],
  providers: [MessageService],
  styles: `
    .commitment-row {
      border-color: var(--p-content-border-color);
    }
    .commitment-row:hover {
      background: var(--p-content-hover-background);
    }
    .overdue-row {
      background: color-mix(in srgb, var(--p-red-50) 50%, transparent);
      border-left: 3px solid var(--p-red-500);
    }
    .overdue-row:hover {
      background: var(--p-red-50);
    }
    .closed-row {
      opacity: 0.6;
    }
    .closed-description {
      text-decoration: line-through;
    }
    .direction-tag {
      font-size: 0.7rem;
    }
    .overdue-date {
      color: var(--p-red-500);
    }
  `,
  template: `
    <p-toast />
    <div class="flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">Commitments</h1>
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
          @if (hasActiveFilters()) {
            <p class="text-muted-color">No commitments match the current filters. Try adjusting or clearing the filters above.</p>
          } @else {
            <p class="text-muted-color">No commitments found. Commitments are created automatically from transcript extraction.</p>
          }
        </div>
      } @else {
        <div class="flex flex-col gap-6">
          @for (group of personGroups(); track group.personId) {
            <div>
              <div class="flex items-center gap-3 mb-3">
                <p-avatar
                  [label]="group.initials"
                  shape="circle"
                  [style]="{ 'background-color': avatarColor(group.personId), color: 'var(--p-surface-0)' }"
                />
                <div>
                  <div class="font-semibold">{{ group.personName }}</div>
                  <div class="text-xs text-muted-color">
                    {{ group.totalCount }} commitment{{ group.totalCount !== 1 ? 's' : '' }}@if (group.overdueCount > 0) {
                      <span> · {{ group.overdueCount }} overdue</span>
                    }
                  </div>
                </div>
              </div>
              <div class="flex flex-col gap-2 ml-11">
                @for (commitment of group.commitments; track commitment.id) {
                  <div
                    role="button"
                    tabindex="0"
                    class="flex items-center gap-3 py-2 px-3 rounded-lg cursor-pointer commitment-row"
                    [class.overdue-row]="commitment.isOverdue"
                    [class.closed-row]="commitment.status !== 'Open'"
                    (click)="onRowClick(commitment)"
                    (keydown.enter)="onRowKeydown($event, commitment)"
                    (keydown.space)="onRowKeydown($event, commitment)"
                  >
                    <p-tag
                      class="direction-tag"
                      [value]="formatDirection(commitment.direction)"
                      [severity]="directionSeverity(commitment.direction)"
                    />
                    <span
                      class="text-sm flex-1"
                      [class.closed-description]="commitment.status !== 'Open'"
                    >{{ commitment.description }}</span>
                    @if (commitment.isOverdue) {
                      <span class="text-xs font-medium overdue-date">{{ commitment.dueDate }}</span>
                    } @else if (commitment.dueDate) {
                      <span class="text-xs text-muted-color">{{ commitment.dueDate }}</span>
                    } @else {
                      <span class="text-xs text-muted-color">No date</span>
                    }
                    <div class="flex gap-1" (click)="$event.stopPropagation()">
                      @if (commitment.status === 'Open') {
                        <p-button
                          icon="pi pi-check"
                          severity="success"
                          [text]="true"
                          size="small"
                          pTooltip="Complete"
                          ariaLabel="Complete"
                          (onClick)="onComplete(commitment)"
                        />
                        <p-button
                          icon="pi pi-eye-slash"
                          severity="secondary"
                          [text]="true"
                          size="small"
                          pTooltip="Dismiss"
                          ariaLabel="Dismiss"
                          (onClick)="onDismiss(commitment)"
                        />
                      } @else {
                        <p-button
                          icon="pi pi-replay"
                          severity="info"
                          [text]="true"
                          size="small"
                          pTooltip="Reopen"
                          ariaLabel="Reopen"
                          (onClick)="onReopen(commitment)"
                        />
                      }
                    </div>
                  </div>
                }
              </div>
            </div>
          }
        </div>
      }
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
  private readonly peopleMap = signal<Map<string, string>>(new Map());

  readonly selectedDirection = signal<CommitmentDirection | null>(null);
  readonly selectedStatus = signal<CommitmentStatus | null>('Open');
  readonly selectedOverdue = signal<boolean | null>(null);

  readonly hasActiveFilters = computed(() =>
    this.selectedDirection() !== null || this.selectedStatus() !== null || this.selectedOverdue() !== null,
  );

  readonly personGroups = computed<PersonGroup[]>(() => {
    const commitments = this.commitments();
    const people = this.peopleMap();

    const groupMap = new Map<string, Commitment[]>();
    for (const c of commitments) {
      const existing = groupMap.get(c.personId);
      if (existing) {
        existing.push(c);
      } else {
        groupMap.set(c.personId, [c]);
      }
    }

    const groups: PersonGroup[] = [];
    for (const [personId, personCommitments] of groupMap) {
      const name = people.get(personId) ?? 'Unknown';
      groups.push({
        personId,
        personName: name,
        initials: this.getInitials(name),
        commitments: personCommitments,
        overdueCount: personCommitments.filter((c) => c.isOverdue).length,
        totalCount: personCommitments.length,
      });
    }

    // Sort: groups with overdue items first, then by name
    groups.sort((a, b) => {
      if (a.overdueCount > 0 && b.overdueCount === 0) return -1;
      if (a.overdueCount === 0 && b.overdueCount > 0) return 1;
      return a.personName.localeCompare(b.personName);
    });

    return groups;
  });

  protected readonly directionFilterOptions = [
    { label: 'I owe them', value: 'MineToThem' as CommitmentDirection },
    { label: 'They owe me', value: 'TheirsToMe' as CommitmentDirection },
  ];

  protected readonly statusFilterOptions = [
    { label: 'Open', value: 'Open' as CommitmentStatus },
    { label: 'Completed', value: 'Completed' as CommitmentStatus },
    { label: 'Cancelled', value: 'Cancelled' as CommitmentStatus },
    { label: 'Dismissed', value: 'Dismissed' as CommitmentStatus },
  ];

  protected readonly overdueFilterOptions = [{ label: 'Overdue only', value: true }];

  private readonly avatarColors = [
    'var(--p-indigo-500)', 'var(--p-green-700)', 'var(--p-orange-800)', 'var(--p-purple-700)', 'var(--p-teal-700)',
    'var(--p-red-800)', 'var(--p-blue-700)', 'var(--p-stone-700)', 'var(--p-indigo-800)', 'var(--p-cyan-700)',
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

  protected onRowKeydown(event: Event, commitment: Commitment): void {
    if (event.target !== event.currentTarget) return;
    event.preventDefault();
    this.onRowClick(commitment);
  }

  protected formatDirection(direction: CommitmentDirection): string {
    switch (direction) {
      case 'MineToThem':
        return 'I owe';
      case 'TheirsToMe':
        return 'They owe';
    }
  }

  protected directionSeverity(direction: CommitmentDirection): 'info' | 'warn' {
    switch (direction) {
      case 'MineToThem':
        return 'warn';
      case 'TheirsToMe':
        return 'info';
    }
  }

  protected avatarColor(personId: string): string {
    let hash = 0;
    for (let i = 0; i < personId.length; i++) {
      hash = (hash * 31 + personId.charCodeAt(i)) | 0;
    }
    return this.avatarColors[Math.abs(hash) % this.avatarColors.length];
  }

  protected onComplete(commitment: Commitment): void {
    this.commitmentsService.complete(commitment.id).subscribe({
      next: () => this.loadCommitments(),
      error: () =>
        this.messageService.add({ severity: 'error', summary: 'Failed to complete commitment' }),
    });
  }

  protected onDismiss(commitment: Commitment): void {
    this.commitmentsService.dismiss(commitment.id).subscribe({
      next: () => this.loadCommitments(),
      error: () =>
        this.messageService.add({ severity: 'error', summary: 'Failed to dismiss commitment' }),
    });
  }

  protected onReopen(commitment: Commitment): void {
    this.commitmentsService.reopen(commitment.id).subscribe({
      next: () => this.loadCommitments(),
      error: () =>
        this.messageService.add({ severity: 'error', summary: 'Failed to reopen commitment' }),
    });
  }

  private getInitials(name: string): string {
    const trimmed = name?.trim();
    if (!trimmed) return '?';
    const parts = trimmed.split(/\s+/);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return trimmed.substring(0, 2).toUpperCase();
  }

  private loadCommitments(): void {
    this.loading.set(true);
    this.commitmentsService
      .list(
        this.selectedDirection() ?? undefined,
        this.selectedStatus() ?? undefined,
        undefined,
        undefined,
        this.selectedOverdue() ?? undefined,
      )
      .subscribe({
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
        people.forEach((p) => map.set(p.id, p.name));
        this.peopleMap.set(map);
      },
      error: () =>
        this.messageService.add({ severity: 'error', summary: 'Failed to load people' }),
    });
  }
}
