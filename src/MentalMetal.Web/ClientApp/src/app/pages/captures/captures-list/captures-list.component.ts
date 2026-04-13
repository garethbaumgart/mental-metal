import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { CapturesService } from '../../../shared/services/captures.service';
import { Capture, CaptureType, ProcessingStatus } from '../../../shared/models/capture.model';
import { QuickCaptureDialogComponent } from '../quick-capture-dialog/quick-capture-dialog.component';

@Component({
  selector: 'app-captures-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DatePipe, ButtonModule, SelectModule, TableModule, TagModule, QuickCaptureDialogComponent],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">Captures</h1>
        <p-button label="New Capture" icon="pi pi-plus" (onClick)="showCreateDialog.set(true)" />
      </div>

      <div class="flex items-center gap-4">
        <p-select
          [options]="typeFilterOptions"
          [ngModel]="selectedType()"
          (ngModelChange)="selectedType.set($event); onFilterChange()"
          placeholder="All Types"
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
      </div>

      @if (loading()) {
        <div class="flex justify-center p-8">
          <i class="pi pi-spinner pi-spin text-2xl"></i>
        </div>
      } @else if (captures().length === 0) {
        <div class="flex flex-col items-center gap-4 p-12">
          <i class="pi pi-pencil text-4xl text-muted-color"></i>
          <p class="text-muted-color">No captures found. Create your first capture to get started.</p>
        </div>
      } @else {
        <p-table
          [value]="captures()"
          [rows]="20"
          [paginator]="captures().length > 20"
          [rowHover]="true"
          styleClass="p-datatable-sm"
        >
          <ng-template #header>
            <tr>
              <th>Title / Preview</th>
              <th>Type</th>
              <th>Status</th>
              <th>Captured</th>
            </tr>
          </ng-template>
          <ng-template #body let-capture>
            <tr class="cursor-pointer" (click)="onRowClick(capture)">
              <td class="font-medium">{{ capture.title || contentPreview(capture.rawContent) }}</td>
              <td>
                <p-tag [value]="formatType(capture.captureType)" [severity]="typeSeverity(capture.captureType)" />
              </td>
              <td>
                @if (capture.processingStatus === 'Processing') {
                  <p-tag severity="warn">
                    <i class="pi pi-spinner pi-spin mr-1"></i> Processing
                  </p-tag>
                } @else {
                  <p-tag [value]="formatStatus(capture.processingStatus)" [severity]="statusSeverity(capture.processingStatus)" />
                }
              </td>
              <td class="text-muted-color text-sm">{{ capture.capturedAt | date:'short' }}</td>
            </tr>
          </ng-template>
        </p-table>
      }

      <app-quick-capture-dialog
        [(visible)]="showCreateDialog"
        (created)="onCaptureCreated($event)"
      />
    </div>
  `,
})
export class CapturesListComponent implements OnInit {
  private readonly capturesService = inject(CapturesService);
  private readonly router = inject(Router);

  readonly captures = signal<Capture[]>([]);
  readonly loading = signal(true);
  readonly showCreateDialog = signal(false);
  readonly selectedType = signal<CaptureType | null>(null);
  readonly selectedStatus = signal<ProcessingStatus | null>(null);

  protected readonly typeFilterOptions = [
    { label: 'Quick Note', value: 'QuickNote' as CaptureType },
    { label: 'Transcript', value: 'Transcript' as CaptureType },
    { label: 'Meeting Notes', value: 'MeetingNotes' as CaptureType },
  ];

  protected readonly statusFilterOptions = [
    { label: 'Raw', value: 'Raw' as ProcessingStatus },
    { label: 'Processing', value: 'Processing' as ProcessingStatus },
    { label: 'Processed', value: 'Processed' as ProcessingStatus },
    { label: 'Failed', value: 'Failed' as ProcessingStatus },
  ];

  ngOnInit(): void {
    this.loadCaptures();
  }

  protected onFilterChange(): void {
    this.loadCaptures();
  }

  protected onRowClick(capture: Capture): void {
    this.router.navigate(['/capture', capture.id]);
  }

  protected onCaptureCreated(_capture: Capture): void {
    this.loadCaptures();
  }

  protected contentPreview(content: string): string {
    return content.length > 80 ? content.substring(0, 80) + '...' : content;
  }

  protected formatType(type: CaptureType): string {
    switch (type) {
      case 'QuickNote': return 'Quick Note';
      case 'Transcript': return 'Transcript';
      case 'MeetingNotes': return 'Meeting Notes';
    }
  }

  protected typeSeverity(type: CaptureType): 'success' | 'info' | 'warn' {
    switch (type) {
      case 'QuickNote': return 'info';
      case 'Transcript': return 'warn';
      case 'MeetingNotes': return 'success';
    }
  }

  protected formatStatus(status: ProcessingStatus): string {
    switch (status) {
      case 'Raw': return 'Raw';
      case 'Processing': return 'Processing';
      case 'Processed': return 'Processed';
      case 'Failed': return 'Failed';
    }
  }

  protected statusSeverity(status: ProcessingStatus): 'info' | 'warn' | 'success' | 'danger' {
    switch (status) {
      case 'Raw': return 'info';
      case 'Processing': return 'warn';
      case 'Processed': return 'success';
      case 'Failed': return 'danger';
    }
  }

  private loadCaptures(): void {
    this.loading.set(true);
    this.capturesService.list(this.selectedType() ?? undefined, this.selectedStatus() ?? undefined).subscribe({
      next: (captures) => {
        this.captures.set(captures);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
