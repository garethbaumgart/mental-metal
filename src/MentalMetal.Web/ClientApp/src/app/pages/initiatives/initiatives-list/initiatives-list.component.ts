import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { InitiativesService } from '../../../shared/services/initiatives.service';
import { Initiative, InitiativeStatus } from '../../../shared/models/initiative.model';
import { CreateInitiativeDialogComponent } from '../create-initiative-dialog/create-initiative-dialog.component';

@Component({
  selector: 'app-initiatives-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, SelectModule, TableModule, TagModule, CreateInitiativeDialogComponent],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">Initiatives</h1>
        <p-button label="New Initiative" icon="pi pi-plus" (onClick)="showCreateDialog.set(true)" />
      </div>

      <div class="flex items-center gap-4">
        <p-select
          [options]="statusFilterOptions"
          [ngModel]="selectedStatus()"
          (ngModelChange)="selectedStatus.set($event); onStatusFilterChange()"
          placeholder="All Statuses"
          [showClear]="true"
          class="w-48"
        />
      </div>

      @if (loading()) {
        <div class="flex justify-center p-8">
          <i class="pi pi-spinner pi-spin text-2xl"></i>
        </div>
      } @else if (initiatives().length === 0) {
        <div class="flex flex-col items-center gap-4 p-12">
          <i class="pi pi-flag text-4xl text-muted-color"></i>
          <p class="text-muted-color">No initiatives found. Create your first initiative to get started.</p>
        </div>
      } @else {
        <p-table
          [value]="initiatives()"
          [rows]="20"
          [paginator]="initiatives().length > 20"
          [rowHover]="true"
          styleClass="p-datatable-sm"
        >
          <ng-template #header>
            <tr>
              <th>Title</th>
              <th>Status</th>
              <th>Summary</th>
            </tr>
          </ng-template>
          <ng-template #body let-initiative>
            <tr class="cursor-pointer" (click)="onRowClick(initiative)">
              <td class="font-medium">{{ initiative.title }}</td>
              <td>
                <p-tag [value]="formatStatus(initiative.status)" [severity]="statusSeverity(initiative.status)" />
              </td>
              <td class="text-sm text-muted-color truncate" style="max-width: 300px">{{ initiative.autoSummary || '\u2014' }}</td>
            </tr>
          </ng-template>
        </p-table>
      }

      <app-create-initiative-dialog
        [(visible)]="showCreateDialog"
        (created)="onInitiativeCreated($event)"
      />
    </div>
  `,
})
export class InitiativesListComponent implements OnInit {
  private readonly initiativesService = inject(InitiativesService);
  private readonly router = inject(Router);

  readonly initiatives = signal<Initiative[]>([]);
  readonly loading = signal(true);
  readonly showCreateDialog = signal(false);
  readonly selectedStatus = signal<InitiativeStatus | null>(null);

  protected readonly statusFilterOptions = [
    { label: 'Active', value: 'Active' as InitiativeStatus },
    { label: 'On Hold', value: 'OnHold' as InitiativeStatus },
    { label: 'Completed', value: 'Completed' as InitiativeStatus },
    { label: 'Cancelled', value: 'Cancelled' as InitiativeStatus },
  ];

  ngOnInit(): void {
    this.loadInitiatives();
  }

  protected onStatusFilterChange(): void {
    this.loadInitiatives();
  }

  protected onRowClick(initiative: Initiative): void {
    this.router.navigate(['/initiatives', initiative.id]);
  }

  protected onInitiativeCreated(_initiative: Initiative): void {
    this.loadInitiatives();
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

  private loadInitiatives(): void {
    this.loading.set(true);
    this.initiativesService.list(this.selectedStatus() ?? undefined).subscribe({
      next: (initiatives) => {
        this.initiatives.set(initiatives);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
