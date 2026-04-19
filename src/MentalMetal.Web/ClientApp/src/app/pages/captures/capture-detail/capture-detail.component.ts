import { ChangeDetectionStrategy, Component, computed, effect, ElementRef, inject, OnInit, signal, viewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { PanelModule } from 'primeng/panel';
import { SelectModule } from 'primeng/select';
import { MessageService } from 'primeng/api';
import { CapturesService } from '../../../shared/services/captures.service';
import { PeopleService } from '../../../shared/services/people.service';
import { InitiativesService } from '../../../shared/services/initiatives.service';
import { Capture, CaptureTranscript, CaptureType, ProcessingStatus } from '../../../shared/models/capture.model';
import { Person } from '../../../shared/models/person.model';
import { Initiative } from '../../../shared/models/initiative.model';
import { TranscriptViewerComponent } from '../transcript-viewer/transcript-viewer.component';
import { SpeakerPickerComponent } from '../speaker-picker/speaker-picker.component';

@Component({
  selector: 'app-capture-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    DatePipe,
    ButtonModule,
    InputTextModule,
    TagModule,
    ToastModule,
    PanelModule,
    SelectModule,
    TranscriptViewerComponent,
    SpeakerPickerComponent,
  ],
  styles: [`
    .content-block {
      border-color: var(--p-content-border-color);
      background-color: var(--p-surface-50);
    }
    .extraction-section {
      border-color: var(--p-content-border-color);
      background-color: var(--p-surface-50);
    }
    .extraction-item {
      border-color: var(--p-surface-100);
    }
    .error-banner {
      border-color: var(--p-red-200);
      background-color: var(--p-red-50);
    }
    .error-icon { color: var(--p-red-500); }
    .error-title { color: var(--p-red-700); }
    .error-detail { color: var(--p-red-600); }
    .risk-icon { color: var(--p-yellow-500); }
  `],
  providers: [MessageService],
  template: `
    <p-toast />

    @if (loading()) {
      <div class="flex justify-center p-8">
        <i class="pi pi-spinner pi-spin text-2xl"></i>
      </div>
    } @else if (capture()) {
      <div class="max-w-2xl mx-auto flex flex-col gap-8">
        <!-- Header -->
        <div class="flex items-center gap-4">
          <p-button icon="pi pi-arrow-left" [text]="true" (onClick)="goBack()" />
          <h1 class="text-2xl font-bold flex-1">{{ capture()!.title || 'Untitled Capture' }}</h1>
          <p-tag [value]="formatType(capture()!.captureType)" [severity]="typeSeverity(capture()!.captureType)" />
          @if (capture()!.processingStatus === 'Processing') {
            <p-tag severity="warn">
              <i class="pi pi-spinner pi-spin mr-1"></i> Processing
            </p-tag>
          } @else {
            <p-tag [value]="formatStatus(capture()!.processingStatus)" [severity]="statusSeverity(capture()!.processingStatus)" />
          }
        </div>

        <!-- Timestamps -->
        <div class="flex items-center gap-4 text-sm text-muted-color">
          <span>Captured: {{ capture()!.capturedAt | date:'medium' }}</span>
          @if (capture()!.processedAt) {
            <span>Processed: {{ capture()!.processedAt | date:'medium' }}</span>
          }
          @if (capture()!.captureSource) {
            <span>Source: {{ capture()!.captureSource }}</span>
          }
        </div>

        <!-- Failed banner -->
        @if (capture()!.processingStatus === 'Failed') {
          <div class="flex items-center gap-4 p-4 rounded-md border error-banner">
            <i class="pi pi-exclamation-triangle error-icon"></i>
            <div class="flex-1">
              <p class="font-medium error-title">Processing Failed</p>
              @if (capture()!.failureReason) {
                <p class="text-sm error-detail">{{ capture()!.failureReason }}</p>
              }
            </div>
            <p-button
              label="Retry"
              icon="pi pi-refresh"
              severity="warn"
              (onClick)="retryProcessing()"
              [loading]="retrying()"
            />
          </div>
        }

        <!-- Raw Content -->
        <section class="flex flex-col gap-4">
          <h2 class="text-xl font-semibold">Content</h2>
          @if (highlightRange(); as range) {
            <div class="p-4 rounded-md border content-block whitespace-pre-wrap text-sm">{{ contentBefore() }}<mark #highlightMark class="source-highlight">{{ contentHighlighted() }}</mark>{{ contentAfter() }}</div>
          } @else {
            <div class="p-4 rounded-md border content-block whitespace-pre-wrap text-sm">{{ capture()!.rawContent }}</div>
          }
        </section>

        <!-- Transcript (audio captures only) -->
        @if (capture()!.captureType === 'AudioRecording') {
          <section class="flex flex-col gap-4">
            <div class="flex items-center justify-between">
              <h2 class="text-xl font-semibold">Transcript</h2>
            </div>
            <app-transcript-viewer
              [transcript]="transcript()"
              [highlightStart]="highlightStart()"
              [highlightEnd]="highlightEnd()"
              (linkRequested)="openSpeakerPicker($event)"
            />
            @if (pickerSpeakerLabel(); as label) {
              <app-speaker-picker [speakerLabel]="label" (linked)="onSpeakerLinked($event)" (cancelled)="pickerSpeakerLabel.set(null)" />
            }
          </section>
        }

        <!-- AI Extraction (auto-applied, read-only) -->
        @if (capture()!.processingStatus === 'Processed' && capture()!.aiExtraction) {
          <section class="flex flex-col gap-4">
            <h2 class="text-xl font-semibold">AI Extraction</h2>

            <!-- Summary -->
            <div class="p-4 rounded-md border extraction-section">
              <h3 class="font-semibold mb-2">Summary</h3>
              <p class="text-sm">{{ capture()!.aiExtraction!.summary }}</p>
            </div>

            <!-- Commitments -->
            @if (capture()!.aiExtraction!.commitments.length > 0) {
              <div class="p-4 rounded-md border extraction-section">
                <h3 class="font-semibold mb-2">Commitments ({{ capture()!.aiExtraction!.commitments.length }})</h3>
                @for (c of capture()!.aiExtraction!.commitments; track $index) {
                  <div class="p-3 mb-2 rounded border extraction-item flex items-start gap-3">
                    <p-tag [value]="c.direction === 'MineToThem' ? 'Mine' : 'Theirs'" [severity]="c.direction === 'MineToThem' ? 'warn' : 'info'" />
                    <p-tag [value]="c.confidence" [severity]="c.confidence === 'High' ? 'success' : c.confidence === 'Medium' ? 'warn' : 'secondary'" />
                    <div class="flex-1">
                      <p class="text-sm font-medium">{{ c.description }}</p>
                    </div>
                  </div>
                }
              </div>
            }

            <!-- Decisions -->
            @if (capture()!.aiExtraction!.decisions.length > 0) {
              <div class="p-4 rounded-md border extraction-section">
                <h3 class="font-semibold mb-2">Decisions ({{ capture()!.aiExtraction!.decisions.length }})</h3>
                @for (d of capture()!.aiExtraction!.decisions; track $index) {
                  <div class="p-2 mb-1 text-sm flex items-start gap-2">
                    <i class="pi pi-check-square text-muted-color mt-0.5"></i>
                    <span>{{ d }}</span>
                  </div>
                }
              </div>
            }

            <!-- Risks -->
            @if (capture()!.aiExtraction!.risks.length > 0) {
              <div class="p-4 rounded-md border extraction-section">
                <h3 class="font-semibold mb-2">Risks ({{ capture()!.aiExtraction!.risks.length }})</h3>
                @for (r of capture()!.aiExtraction!.risks; track $index) {
                  <div class="p-2 mb-1 text-sm flex items-start gap-2">
                    <i class="pi pi-exclamation-triangle risk-icon mt-0.5"></i>
                    <span>{{ r }}</span>
                  </div>
                }
              </div>
            }

            <!-- People Mentioned -->
            @if (capture()!.aiExtraction!.peopleMentioned.length > 0) {
              <div class="p-4 rounded-md border extraction-section">
                <h3 class="font-semibold mb-2">People Mentioned</h3>
                @for (p of capture()!.aiExtraction!.peopleMentioned; track $index) {
                  <div class="p-2 mb-1 text-sm flex items-center gap-2">
                    <span class="font-medium">{{ p.rawName }}</span>
                    @if (p.context) {
                      <span class="text-muted-color">— {{ p.context }}</span>
                    }
                    @if (p.personId) {
                      <p-tag value="Resolved" severity="success" />
                    } @else {
                      <p-tag value="Unresolved" severity="warn" />
                      @if (resolvingPersonName() === p.rawName) {
                        <p-select
                          [options]="people()"
                          optionLabel="name"
                          optionValue="id"
                          placeholder="Select person..."
                          [filter]="true"
                          filterBy="name"
                          (onChange)="onPersonSelected(p.rawName, $event.value)"
                          appendTo="body"
                          class="ml-2"
                          [style]="{ 'min-width': '200px' }"
                        />
                        <p-button icon="pi pi-times" [text]="true" size="small" (onClick)="resolvingPersonName.set(null)" />
                      } @else {
                        <p-button label="Resolve" icon="pi pi-link" size="small" [text]="true" (onClick)="startResolvePerson(p.rawName)" />
                      }
                    }
                  </div>
                }
              </div>
            }

            <!-- Initiative Tags -->
            @if (capture()!.aiExtraction!.initiativeTags.length > 0) {
              <div class="p-4 rounded-md border extraction-section">
                <h3 class="font-semibold mb-2">Initiative Tags</h3>
                @for (t of capture()!.aiExtraction!.initiativeTags; track $index) {
                  <div class="p-2 mb-1 text-sm flex items-center gap-2">
                    <span class="font-medium">{{ t.rawName }}</span>
                    @if (t.context) {
                      <span class="text-muted-color">— {{ t.context }}</span>
                    }
                    @if (t.initiativeId) {
                      <p-tag value="Linked" severity="success" />
                    } @else {
                      <p-tag value="Unlinked" severity="warn" />
                      @if (resolvingInitiativeName() === t.rawName) {
                        <p-select
                          [options]="initiatives()"
                          optionLabel="title"
                          optionValue="id"
                          placeholder="Select initiative..."
                          [filter]="true"
                          filterBy="title"
                          (onChange)="onInitiativeSelected(t.rawName, $event.value)"
                          appendTo="body"
                          class="ml-2"
                          [style]="{ 'min-width': '200px' }"
                        />
                        <p-button icon="pi pi-times" [text]="true" size="small" (onClick)="resolvingInitiativeName.set(null)" />
                      } @else {
                        <p-button label="Link" icon="pi pi-link" size="small" [text]="true" (onClick)="startResolveInitiative(t.rawName)" />
                      }
                    }
                  </div>
                }
              </div>
            }
          </section>
        }

        <!-- Metadata Section -->
        <section class="flex flex-col gap-4">
          <h2 class="text-xl font-semibold">Metadata</h2>
          <div class="flex items-end gap-4">
            <div class="flex flex-col gap-2 flex-1">
              <label for="title" class="text-sm font-medium text-muted-color">Title</label>
              <input pInputText id="title" [ngModel]="editTitle()" (ngModelChange)="editTitle.set($event)" class="w-full" />
            </div>
            <p-button
              label="Save"
              (onClick)="saveMetadata()"
              [loading]="savingMetadata()"
              [disabled]="!metadataChanged()"
            />
          </div>
        </section>
      </div>
    }
  `,
})
export class CaptureDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly capturesService = inject(CapturesService);
  private readonly peopleService = inject(PeopleService);
  private readonly initiativesService = inject(InitiativesService);
  private readonly messageService = inject(MessageService);

  readonly highlightMark = viewChild<ElementRef<HTMLElement>>('highlightMark');

  readonly capture = signal<Capture | null>(null);
  readonly loading = signal(true);

  readonly highlightStart = signal<number | null>(null);
  readonly highlightEnd = signal<number | null>(null);

  readonly highlightRange = computed(() => {
    const start = this.highlightStart();
    const end = this.highlightEnd();
    const raw = this.capture()?.rawContent;
    if (start == null || end == null || !raw || start >= end || start < 0) return null;
    const clampedEnd = Math.min(end, raw.length);
    const clampedStart = Math.min(start, clampedEnd);
    if (clampedStart >= clampedEnd) return null;
    return { start: clampedStart, end: clampedEnd };
  });

  readonly contentBefore = computed(() => {
    const range = this.highlightRange();
    const raw = this.capture()?.rawContent;
    if (!range || !raw) return '';
    return raw.substring(0, range.start);
  });

  readonly contentHighlighted = computed(() => {
    const range = this.highlightRange();
    const raw = this.capture()?.rawContent;
    if (!range || !raw) return '';
    return raw.substring(range.start, range.end);
  });

  readonly contentAfter = computed(() => {
    const range = this.highlightRange();
    const raw = this.capture()?.rawContent;
    if (!range || !raw) return '';
    return raw.substring(range.end);
  });
  readonly savingMetadata = signal(false);
  readonly retrying = signal(false);
  readonly transcript = signal<CaptureTranscript | null>(null);
  readonly pickerSpeakerLabel = signal<string | null>(null);

  readonly editTitle = signal('');

  // Resolve UI state
  readonly people = signal<Person[]>([]);
  readonly initiatives = signal<Initiative[]>([]);
  readonly resolvingPersonName = signal<string | null>(null);
  readonly resolvingInitiativeName = signal<string | null>(null);

  constructor() {
    effect(() => {
      const el = this.highlightMark()?.nativeElement;
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
    });
  }

  readonly hasUnresolvedMentions = computed(() => {
    const c = this.capture();
    return c?.aiExtraction?.peopleMentioned.some(p => !p.personId) ?? false;
  });

  readonly hasUnlinkedTags = computed(() => {
    const c = this.capture();
    return c?.aiExtraction?.initiativeTags.some(t => !t.initiativeId) ?? false;
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadCapture(id);
    }
    const qs = this.route.snapshot.queryParamMap;
    const hs = qs.get('highlightStart');
    const he = qs.get('highlightEnd');
    if (hs != null) { const n = parseInt(hs, 10); this.highlightStart.set(isNaN(n) ? null : n); }
    if (he != null) { const n = parseInt(he, 10); this.highlightEnd.set(isNaN(n) ? null : n); }
  }

  protected goBack(): void {
    this.router.navigate(['/capture']);
  }

  protected metadataChanged(): boolean {
    const c = this.capture();
    if (!c) return false;
    return this.editTitle() !== (c.title ?? '');
  }

  protected retryProcessing(): void {
    const c = this.capture();
    if (!c) return;

    this.retrying.set(true);
    this.capturesService.retry(c.id).subscribe({
      next: (updated) => {
        this.capture.set(updated);
        this.retrying.set(false);
        this.messageService.add({ severity: 'success', summary: 'Ready for reprocessing' });
      },
      error: () => {
        this.retrying.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to retry' });
      },
    });
  }

  protected saveMetadata(): void {
    const c = this.capture();
    if (!c) return;

    this.savingMetadata.set(true);
    this.capturesService.updateMetadata(c.id, {
      title: this.editTitle().trim() || null,
    }).subscribe({
      next: (updated) => {
        this.capture.set(updated);
        this.editTitle.set(updated.title ?? '');
        this.savingMetadata.set(false);
        this.messageService.add({ severity: 'success', summary: 'Metadata updated' });
      },
      error: () => {
        this.savingMetadata.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to update metadata' });
      },
    });
  }

  protected formatType(type: CaptureType): string {
    switch (type) {
      case 'QuickNote': return 'Quick Note';
      case 'Transcript': return 'Transcript';
      case 'MeetingNotes': return 'Meeting Notes';
      case 'AudioRecording': return 'Audio';
    }
  }

  protected typeSeverity(type: CaptureType): 'info' | 'warn' | 'success' {
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

  protected startResolvePerson(rawName: string): void {
    this.resolvingPersonName.set(rawName);
    if (this.people().length === 0) {
      this.peopleService.list().subscribe({
        next: (list) => this.people.set(list),
      });
    }
  }

  protected onPersonSelected(rawName: string, personId: string): void {
    const c = this.capture();
    if (!c || !personId) return;

    this.capturesService.resolvePersonMention(c.id, rawName, personId).subscribe({
      next: (updated) => {
        this.capture.set(updated);
        this.resolvingPersonName.set(null);
        this.messageService.add({ severity: 'success', summary: 'Person resolved' });
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Failed to resolve person' });
      },
    });
  }

  protected startResolveInitiative(rawName: string): void {
    this.resolvingInitiativeName.set(rawName);
    if (this.initiatives().length === 0) {
      this.initiativesService.list().subscribe({
        next: (list) => this.initiatives.set(list),
      });
    }
  }

  protected onInitiativeSelected(rawName: string, initiativeId: string): void {
    const c = this.capture();
    if (!c || !initiativeId) return;

    this.capturesService.resolveInitiativeTag(c.id, rawName, initiativeId).subscribe({
      next: (updated) => {
        this.capture.set(updated);
        this.resolvingInitiativeName.set(null);
        this.messageService.add({ severity: 'success', summary: 'Initiative linked' });
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Failed to link initiative' });
      },
    });
  }

  private loadCapture(id: string): void {
    this.loading.set(true);
    this.capturesService.get(id).subscribe({
      next: (capture) => {
        this.capture.set(capture);
        this.editTitle.set(capture.title ?? '');
        if (capture.captureType === 'AudioRecording') {
          this.loadTranscript(capture.id);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.router.navigate(['/capture']);
      },
    });
  }

  private loadTranscript(id: string): void {
    this.capturesService.getTranscript(id).subscribe({
      next: (t) => this.transcript.set(t),
      error: () => this.transcript.set(null),
    });
  }

  protected openSpeakerPicker(speakerLabel: string): void {
    this.pickerSpeakerLabel.set(speakerLabel);
  }

  protected onSpeakerLinked(event: { speakerLabel: string; personId: string }): void {
    const id = this.capture()?.id;
    if (!id) return;
    this.capturesService
      .updateSpeakers(id, { mappings: [{ speakerLabel: event.speakerLabel, personId: event.personId }] })
      .subscribe({
        next: () => {
          this.loadTranscript(id);
          this.pickerSpeakerLabel.set(null);
          this.messageService.add({ severity: 'success', summary: 'Speaker linked' });
        },
        error: (err) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Failed to link speaker',
            detail: err?.error?.errorCode ?? err?.error?.message ?? 'Unknown error',
          });
        },
      });
  }
}
