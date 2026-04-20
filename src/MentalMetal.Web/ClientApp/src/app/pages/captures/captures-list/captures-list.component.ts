import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { CapturesService } from '../../../shared/services/captures.service';
import { CaptureProcessingTrackerService } from '../../../shared/services/capture-processing-tracker.service';
import { QuickCaptureUiService } from '../../../shared/services/quick-capture-ui.service';
import { Capture, CaptureType, ProcessingStatus } from '../../../shared/models/capture.model';
import { CaptureRecorderComponent } from '../audio-recorder/capture-recorder.component';
import { RecordingPanelComponent } from '../recording-panel/recording-panel.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

interface FileUpload {
  file: File;
  status: 'pending' | 'uploading' | 'done' | 'failed';
  error: string | null;
}

@Component({
  selector: 'app-captures-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DatePipe, ButtonModule, SelectModule, TableModule, TagModule, CaptureRecorderComponent, RecordingPanelComponent],
  styles: [`
    .drop-zone {
      border: 2px dashed var(--p-content-border-color);
      transition: border-color 0.2s, background-color 0.2s;
    }
    .drop-zone-active {
      border-color: var(--p-primary-color);
      background-color: var(--p-primary-50);
    }
    .processing-queue {
      background-color: var(--p-primary-50);
      border: 1px solid var(--p-content-border-color);
      border-radius: 8px;
      padding: 14px;
    }
    :host-context(.dark) .processing-queue {
      background-color: var(--p-primary-950);
    }
  `],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">Captures</h1>
        <p-button label="New Capture" icon="pi pi-plus" (onClick)="quickCapture.open()" />
      </div>

      <!-- Drag-Drop Upload Zone -->
      <div
        class="drop-zone rounded-lg p-8 flex flex-col items-center gap-3 cursor-pointer"
        [class.drop-zone-active]="dragOver()"
        (dragover)="onDragOver($event)"
        (dragleave)="onDragLeave($event)"
        (drop)="onDrop($event)"
        (click)="fileInput.click()"
        role="button"
        tabindex="0"
        (keydown.enter)="fileInput.click()"
        (keydown.space)="$event.preventDefault(); fileInput.click()"
        aria-label="Upload files"
      >
        <i class="pi pi-cloud-upload text-3xl text-muted-color"></i>
        <p class="text-sm text-muted-color">
          Drag &amp; drop .docx, .txt, or .html files here, or click to browse
        </p>
        <input
          #fileInput
          type="file"
          class="hidden"
          multiple
          accept=".docx,.txt,.html,.htm"
          (change)="onFilesSelected($event)"
        />
      </div>

      <!-- Upload Progress -->
      @if (uploads().length > 0) {
        <div class="flex flex-col gap-2 p-4 rounded bg-[var(--p-content-hover-background)]">
          <h3 class="text-sm font-semibold">Uploading files</h3>
          @for (u of uploads(); track $index) {
            <div class="flex items-center gap-3">
              <span class="text-sm flex-1 truncate">{{ u.file.name }}</span>
              @if (u.status === 'uploading') {
                <i class="pi pi-spinner pi-spin text-sm"></i>
                <span class="text-xs text-muted-color">Uploading...</span>
              } @else if (u.status === 'done') {
                <i class="pi pi-check text-sm" style="color: var(--p-green-500)"></i>
                <span class="text-xs text-muted-color">Done</span>
              } @else if (u.status === 'failed') {
                <i class="pi pi-times text-sm" style="color: var(--p-red-500)"></i>
                <span class="text-xs text-muted-color">{{ u.error ?? 'Failed' }}</span>
                <p-button
                  icon="pi pi-refresh"
                  size="small"
                  [text]="true"
                  [rounded]="true"
                  ariaLabel="Retry upload"
                  (onClick)="retryUpload(u); $event.stopPropagation()"
                />
              }
            </div>
          }
        </div>
      }

      <app-recording-panel (saved)="onCaptureCreated($event)" />
      <app-capture-recorder (uploaded)="onCaptureCreated($event)" />

      <!-- Currently Processing Queue (Option 5) -->
      @if (processingCaptures().length > 0) {
        <div class="processing-queue" aria-label="Currently processing">
          <div class="text-xs font-semibold uppercase text-muted-color mb-3">Currently Processing</div>
          @for (c of processingCaptures(); track c.id) {
            <div class="flex items-center gap-3 py-2">
              <i class="pi pi-spinner pi-spin text-sm" style="color: var(--p-primary-color)"></i>
              <span class="flex-1 text-sm font-medium truncate">
                {{ c.title || contentPreview(c.rawContent) }}
              </span>
              <span class="text-xs text-muted-color">
                {{ c.processingStatus === 'Raw' ? 'Queued...' : 'Extracting...' }}
              </span>
            </div>
          }
        </div>
      }

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
      } @else if (completedCaptures().length > 0) {
        <p-table
          [value]="completedCaptures()"
          [rows]="20"
          [paginator]="completedCaptures().length > 20"
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
                <p-tag [value]="formatStatus(capture.processingStatus)" [severity]="statusSeverity(capture.processingStatus)" />
              </td>
              <td class="text-muted-color text-sm">{{ capture.capturedAt | date:'short' }}</td>
            </tr>
          </ng-template>
        </p-table>
      }

    </div>
  `,
})
export class CapturesListComponent implements OnInit, OnDestroy {
  private readonly capturesService = inject(CapturesService);
  private readonly processingTracker = inject(CaptureProcessingTrackerService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly quickCapture = inject(QuickCaptureUiService);

  readonly captures = signal<Capture[]>([]);
  readonly loading = signal(true);
  readonly selectedType = signal<CaptureType | null>(null);
  readonly selectedStatus = signal<ProcessingStatus | null>(null);
  readonly dragOver = signal(false);
  readonly uploads = signal<FileUpload[]>([]);

  /** Captures currently being processed (Raw or Processing status). */
  protected readonly processingCaptures = computed(() =>
    this.captures().filter((c) =>
      c.processingStatus === 'Raw' || c.processingStatus === 'Processing',
    ),
  );

  /** Captures that are done processing (Processed or Failed). */
  protected readonly completedCaptures = computed(() =>
    this.captures().filter((c) =>
      c.processingStatus !== 'Raw' && c.processingStatus !== 'Processing',
    ),
  );

  private readonly acceptedExtensions = ['.docx', '.txt', '.html', '.htm'];
  private pendingUploads = 0;
  private pollTimer: ReturnType<typeof setInterval> | null = null;
  private cleanPollCount = 0;
  private pollInFlight = false;

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
    this.quickCapture.captureCreated$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((capture) => {
      // Optimistic queue entry: immediately add the new capture to the list
      this.captures.update((list) => [capture, ...list]);
      this.startPolling();
    });
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  private startPolling(): void {
    if (this.pollTimer) return;
    this.cleanPollCount = 0;
    this.pollTimer = setInterval(() => this.pollForUpdates(), 3000);
  }

  private stopPolling(): void {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }

  private pollForUpdates(): void {
    if (this.pollInFlight) return; // Skip if previous request still pending
    this.pollInFlight = true;
    // Poll unfiltered to detect processing items even if current filters hide them
    this.capturesService.list().subscribe({
      next: (allCaptures) => {
        this.pollInFlight = false;
        const hasProcessing = allCaptures.some(
          (c) => c.processingStatus === 'Raw' || c.processingStatus === 'Processing',
        );

        // Apply current filters for display
        const filtered = allCaptures.filter((c) => {
          if (this.selectedType() && c.captureType !== this.selectedType()) return false;
          if (this.selectedStatus() && c.processingStatus !== this.selectedStatus()) return false;
          return true;
        });
        this.captures.set(filtered);

        if (!hasProcessing) {
          this.cleanPollCount++;
          if (this.cleanPollCount >= 2) {
            this.stopPolling();
          }
        } else {
          this.cleanPollCount = 0;
        }
      },
      error: () => {
        this.pollInFlight = false;
      },
    });
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragOver.set(true);
  }

  protected onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragOver.set(false);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragOver.set(false);
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFiles(Array.from(files));
    }
  }

  protected onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFiles(Array.from(input.files));
      input.value = '';
    }
  }

  protected retryUpload(upload: FileUpload): void {
    this.pendingUploads++;
    this.uploadFile(upload);
  }

  private handleFiles(files: File[]): void {
    const validFiles = files.filter((f) =>
      this.acceptedExtensions.some((ext) => f.name.toLowerCase().endsWith(ext)),
    );
    if (validFiles.length === 0) return;

    const newUploads: FileUpload[] = validFiles.map((file) => ({
      file,
      status: 'pending' as const,
      error: null,
    }));

    this.uploads.update((current) => [...current, ...newUploads]);
    this.pendingUploads += newUploads.length;

    for (const upload of newUploads) {
      this.uploadFile(upload);
    }
  }

  private uploadFile(upload: FileUpload): void {
    this.uploads.update((list) =>
      list.map((u) =>
        u.file === upload.file ? { ...u, status: 'uploading' as const, error: null } : u,
      ),
    );

    this.capturesService.importFile(upload.file).subscribe({
      next: (result) => {
        this.uploads.update((list) =>
          list.map((u) =>
            u.file === upload.file ? { ...u, status: 'done' as const } : u,
          ),
        );
        // Track the uploaded capture for background processing
        this.processingTracker.track(result.id);
        this.onUploadSettled();
      },
      error: (err: unknown) => {
        const message = (err as { error?: { message?: string } })?.error?.message
          ?? (err as { message?: string })?.message
          ?? 'Upload failed';
        this.uploads.update((list) =>
          list.map((u) =>
            u.file === upload.file
              ? { ...u, status: 'failed' as const, error: message }
              : u,
          ),
        );
        this.onUploadSettled();
      },
    });
  }

  private onUploadSettled(): void {
    this.pendingUploads--;
    if (this.pendingUploads <= 0) {
      this.pendingUploads = 0;
      this.loadCaptures();
      this.startPolling();
    }
  }

  protected onFilterChange(): void {
    this.loadCaptures();
  }

  protected onRowClick(capture: Capture): void {
    this.router.navigate(['/capture', capture.id]);
  }

  protected onCaptureCreated(capture: Capture): void {
    this.processingTracker.track(capture.id);
    this.captures.update((list) => [capture, ...list]);
    this.startPolling();
  }

  protected contentPreview(content: string): string {
    return content.length > 80 ? content.substring(0, 80) + '...' : content;
  }

  protected formatType(type: CaptureType): string {
    switch (type) {
      case 'QuickNote': return 'Quick Note';
      case 'Transcript': return 'Transcript';
      case 'MeetingNotes': return 'Meeting Notes';
      case 'AudioRecording': return 'Audio';
    }
  }

  protected typeSeverity(type: CaptureType): 'success' | 'info' | 'warn' {
    switch (type) {
      case 'QuickNote': return 'info';
      case 'Transcript': return 'warn';
      case 'MeetingNotes': return 'success';
      case 'AudioRecording': return 'warn';
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

        // Start polling if there are processing items
        if (captures.some((c) => c.processingStatus === 'Raw' || c.processingStatus === 'Processing')) {
          this.startPolling();
        }
      },
      error: () => this.loading.set(false),
    });
  }
}
