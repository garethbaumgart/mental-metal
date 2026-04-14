import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { ChipModule } from 'primeng/chip';
import { ToastModule } from 'primeng/toast';
import { PanelModule } from 'primeng/panel';
import { DividerModule } from 'primeng/divider';
import { AutoCompleteModule, AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { MessageService } from 'primeng/api';
import { CapturesService } from '../../../shared/services/captures.service';
import { PeopleService } from '../../../shared/services/people.service';
import { InitiativesService } from '../../../shared/services/initiatives.service';
import { Capture, CaptureTranscript, CaptureType, ProcessingStatus } from '../../../shared/models/capture.model';
import { TranscriptViewerComponent } from '../transcript-viewer/transcript-viewer.component';
import { SpeakerPickerComponent } from '../speaker-picker/speaker-picker.component';
import { Person } from '../../../shared/models/person.model';
import { Initiative } from '../../../shared/models/initiative.model';

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
    ChipModule,
    ToastModule,
    PanelModule,
    DividerModule,
    AutoCompleteModule,
    TranscriptViewerComponent,
    SpeakerPickerComponent,
  ],
  styles: [`
    .content-block {
      border-color: var(--p-surface-200);
      background-color: var(--p-surface-50);
    }
    .extraction-section {
      border-color: var(--p-surface-200);
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
    .success-banner {
      background-color: var(--p-green-50);
      border-color: var(--p-green-200);
    }
    .success-icon { color: var(--p-green-600); }
    .success-text { color: var(--p-green-700); }
    .warn-banner {
      background-color: var(--p-yellow-50);
      border-color: var(--p-yellow-200);
    }
    .warn-icon { color: var(--p-yellow-600); }
    .warn-text { color: var(--p-yellow-700); }
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
          @if (capture()!.source) {
            <span>Source: {{ capture()!.source }}</span>
          }
        </div>

        <!-- Action Buttons -->
        @if (capture()!.processingStatus === 'Raw') {
          <div>
            <p-button
              label="Process with AI"
              icon="pi pi-sparkles"
              (onClick)="processCapture()"
              [loading]="processing()"
            />
          </div>
        }
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
          <div class="p-4 rounded-md border content-block whitespace-pre-wrap text-sm">{{ capture()!.rawContent }}</div>
        </section>

        <!-- Transcript (audio captures only) -->
        @if (capture()!.captureType === 'AudioRecording') {
          <section class="flex flex-col gap-4">
            <div class="flex items-center justify-between">
              <h2 class="text-xl font-semibold">Transcript</h2>
              @if (transcript()?.transcriptionStatus === 'Failed') {
                <p-button label="Retry transcription" icon="pi pi-refresh" severity="warn" (onClick)="retryTranscription()" [loading]="retryingTranscription()" />
              }
            </div>
            <app-transcript-viewer [transcript]="transcript()" (linkRequested)="openSpeakerPicker($event)" />
            @if (pickerSpeakerLabel(); as label) {
              <app-speaker-picker [speakerLabel]="label" (linked)="onSpeakerLinked($event)" (cancelled)="pickerSpeakerLabel.set(null)" />
            }
          </section>
        }

        <!-- AI Extraction Review Panel -->
        @if (capture()!.processingStatus === 'Processed' && capture()!.aiExtraction) {
          <section class="flex flex-col gap-4">
            <div class="flex items-center justify-between">
              <h2 class="text-xl font-semibold">AI Extraction</h2>
              <p-tag
                [value]="'Confidence: ' + (capture()!.aiExtraction!.confidenceScore * 100).toFixed(0) + '%'"
                severity="info"
              />
            </div>

            @if (capture()!.extractionStatus === 'Confirmed') {
              <div class="flex items-center gap-2 p-3 rounded-md border success-banner">
                <i class="pi pi-check-circle success-icon"></i>
                <span class="success-text font-medium">Entities created</span>
              </div>
            } @else if (capture()!.extractionStatus === 'Discarded') {
              <div class="flex items-center gap-2 p-3 rounded-md border warn-banner">
                <i class="pi pi-info-circle warn-icon"></i>
                <span class="warn-text font-medium">Extraction discarded</span>
              </div>
            } @else {
              <div class="flex gap-2">
                <p-button
                  label="Confirm & Create"
                  icon="pi pi-check"
                  (onClick)="confirmExtraction()"
                  [loading]="confirming()"
                />
                <p-button
                  label="Discard"
                  icon="pi pi-times"
                  severity="secondary"
                  [outlined]="true"
                  (onClick)="discardExtraction()"
                  [loading]="discarding()"
                />
              </div>
            }

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
                    <p-tag [value]="c.direction === 'MineToThem' ? 'Mine → Them' : 'Theirs → Me'" [severity]="c.direction === 'MineToThem' ? 'warn' : 'info'" />
                    <div class="flex-1">
                      <p class="text-sm font-medium">{{ c.description }}</p>
                      <div class="flex gap-3 mt-1 text-xs text-muted-color">
                        @if (c.personHint) {
                          <span><i class="pi pi-user mr-1"></i>{{ c.personHint }}</span>
                        }
                        @if (c.dueDate) {
                          <span><i class="pi pi-calendar mr-1"></i>{{ c.dueDate }}</span>
                        }
                      </div>
                    </div>
                  </div>
                }
              </div>
            }

            <!-- Delegations -->
            @if (capture()!.aiExtraction!.delegations.length > 0) {
              <div class="p-4 rounded-md border extraction-section">
                <h3 class="font-semibold mb-2">Delegations ({{ capture()!.aiExtraction!.delegations.length }})</h3>
                @for (d of capture()!.aiExtraction!.delegations; track $index) {
                  <div class="p-3 mb-2 rounded border extraction-item">
                    <p class="text-sm font-medium">{{ d.description }}</p>
                    <div class="flex gap-3 mt-1 text-xs text-muted-color">
                      @if (d.personHint) {
                        <span><i class="pi pi-user mr-1"></i>{{ d.personHint }}</span>
                      }
                      @if (d.dueDate) {
                        <span><i class="pi pi-calendar mr-1"></i>{{ d.dueDate }}</span>
                      }
                    </div>
                  </div>
                }
              </div>
            }

            <!-- Observations -->
            @if (capture()!.aiExtraction!.observations.length > 0) {
              <div class="p-4 rounded-md border extraction-section">
                <h3 class="font-semibold mb-2">Observations ({{ capture()!.aiExtraction!.observations.length }})</h3>
                @for (o of capture()!.aiExtraction!.observations; track $index) {
                  <div class="p-3 mb-2 rounded border extraction-item flex items-start gap-3">
                    @if (o.tag) {
                      <p-tag [value]="o.tag" severity="secondary" />
                    }
                    <div class="flex-1">
                      <p class="text-sm font-medium">{{ o.description }}</p>
                      @if (o.personHint) {
                        <span class="text-xs text-muted-color"><i class="pi pi-user mr-1"></i>{{ o.personHint }}</span>
                      }
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
            @if (capture()!.aiExtraction!.risksIdentified.length > 0) {
              <div class="p-4 rounded-md border extraction-section">
                <h3 class="font-semibold mb-2">Risks ({{ capture()!.aiExtraction!.risksIdentified.length }})</h3>
                @for (r of capture()!.aiExtraction!.risksIdentified; track $index) {
                  <div class="p-2 mb-1 text-sm flex items-start gap-2">
                    <i class="pi pi-exclamation-triangle risk-icon mt-0.5"></i>
                    <span>{{ r }}</span>
                  </div>
                }
              </div>
            }

            <!-- Suggested Links -->
            @if (capture()!.aiExtraction!.suggestedPersonLinks.length > 0 || capture()!.aiExtraction!.suggestedInitiativeLinks.length > 0) {
              <div class="p-4 rounded-md border extraction-section">
                <h3 class="font-semibold mb-2">Suggested Links</h3>
                @if (capture()!.aiExtraction!.suggestedPersonLinks.length > 0) {
                  <div class="flex flex-wrap gap-2 mb-2">
                    <span class="text-sm text-muted-color mr-2">People:</span>
                    @for (p of capture()!.aiExtraction!.suggestedPersonLinks; track $index) {
                      <p-tag [value]="p" severity="info" />
                    }
                  </div>
                }
                @if (capture()!.aiExtraction!.suggestedInitiativeLinks.length > 0) {
                  <div class="flex flex-wrap gap-2">
                    <span class="text-sm text-muted-color mr-2">Initiatives:</span>
                    @for (i of capture()!.aiExtraction!.suggestedInitiativeLinks; track $index) {
                      <p-tag [value]="i" severity="success" />
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
            <div class="flex flex-col gap-2 flex-1">
              <label for="source" class="text-sm font-medium text-muted-color">Source</label>
              <input pInputText id="source" [ngModel]="editSource()" (ngModelChange)="editSource.set($event)" class="w-full" />
            </div>
            <p-button
              label="Save"
              (onClick)="saveMetadata()"
              [loading]="savingMetadata()"
              [disabled]="!metadataChanged()"
            />
          </div>
        </section>

        <!-- Linked People Section -->
        <section class="flex flex-col gap-4">
          <h2 class="text-xl font-semibold">Linked People</h2>

          @if (linkedPeople().length === 0 && capture()!.linkedPersonIds.length === 0) {
            <p class="text-muted-color text-sm">No linked people.</p>
          } @else {
            <div class="flex flex-wrap gap-2">
              @for (person of linkedPeople(); track person.id) {
                <p-chip [label]="person.name" [removable]="true" (onRemove)="unlinkPerson(person.id)" />
              }
            </div>
          }

          <div class="flex items-end gap-4">
            <div class="flex flex-col gap-2 flex-1">
              <label for="linkPerson" class="text-sm font-medium text-muted-color">Add Person</label>
              <p-autoComplete
                id="linkPerson"
                [ngModel]="selectedPerson()"
                (ngModelChange)="selectedPerson.set($event)"
                [suggestions]="peopleSuggestions()"
                (completeMethod)="searchPeople($event)"
                field="name"
                [forceSelection]="true"
                placeholder="Search people..."
                class="w-full"
              />
            </div>
            <p-button
              label="Link"
              icon="pi pi-link"
              [outlined]="true"
              (onClick)="linkPerson()"
              [loading]="linkingPerson()"
              [disabled]="!selectedPerson()"
            />
          </div>
        </section>

        <!-- Linked Initiatives Section -->
        <section class="flex flex-col gap-4">
          <h2 class="text-xl font-semibold">Linked Initiatives</h2>

          @if (linkedInitiatives().length === 0 && capture()!.linkedInitiativeIds.length === 0) {
            <p class="text-muted-color text-sm">No linked initiatives.</p>
          } @else {
            <div class="flex flex-wrap gap-2">
              @for (initiative of linkedInitiatives(); track initiative.id) {
                <p-chip [label]="initiative.title" [removable]="true" (onRemove)="unlinkInitiative(initiative.id)" />
              }
            </div>
          }

          <div class="flex items-end gap-4">
            <div class="flex flex-col gap-2 flex-1">
              <label for="linkInitiative" class="text-sm font-medium text-muted-color">Add Initiative</label>
              <p-autoComplete
                id="linkInitiative"
                [ngModel]="selectedInitiative()"
                (ngModelChange)="selectedInitiative.set($event)"
                [suggestions]="initiativeSuggestions()"
                (completeMethod)="searchInitiatives($event)"
                field="title"
                [forceSelection]="true"
                placeholder="Search initiatives..."
                class="w-full"
              />
            </div>
            <p-button
              label="Link"
              icon="pi pi-link"
              [outlined]="true"
              (onClick)="linkInitiative()"
              [loading]="linkingInitiative()"
              [disabled]="!selectedInitiative()"
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

  readonly capture = signal<Capture | null>(null);
  readonly loading = signal(true);
  readonly savingMetadata = signal(false);
  readonly linkingPerson = signal(false);
  readonly linkingInitiative = signal(false);
  readonly processing = signal(false);
  readonly retrying = signal(false);
  readonly confirming = signal(false);
  readonly discarding = signal(false);
  readonly linkedPeople = signal<Person[]>([]);
  readonly linkedInitiatives = signal<Initiative[]>([]);
  readonly peopleSuggestions = signal<Person[]>([]);
  readonly initiativeSuggestions = signal<Initiative[]>([]);

  readonly editTitle = signal('');
  readonly editSource = signal('');
  readonly selectedPerson = signal<Person | null>(null);
  readonly selectedInitiative = signal<Initiative | null>(null);
  readonly transcript = signal<CaptureTranscript | null>(null);
  readonly retryingTranscription = signal(false);
  readonly pickerSpeakerLabel = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadCapture(id);
    }
  }

  protected goBack(): void {
    this.router.navigate(['/capture']);
  }

  protected metadataChanged(): boolean {
    const c = this.capture();
    if (!c) return false;
    return this.editTitle() !== (c.title ?? '') || this.editSource() !== (c.source ?? '');
  }

  protected processCapture(): void {
    const c = this.capture();
    if (!c) return;

    this.processing.set(true);
    this.capturesService.process(c.id).subscribe({
      next: (updated) => {
        this.capture.set(updated);
        this.processing.set(false);
        if (updated.processingStatus === 'Processed') {
          this.messageService.add({ severity: 'success', summary: 'Processing complete' });
        } else if (updated.processingStatus === 'Failed') {
          this.messageService.add({ severity: 'error', summary: 'Processing failed', detail: updated.failureReason ?? undefined });
        } else {
          this.messageService.add({ severity: 'info', summary: 'Processing started' });
        }
      },
      error: () => {
        this.processing.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to start processing' });
      },
    });
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

  protected confirmExtraction(): void {
    const c = this.capture();
    if (!c) return;

    this.confirming.set(true);
    this.capturesService.confirmExtraction(c.id).subscribe({
      next: (result) => {
        this.capture.set(result.capture);
        this.confirming.set(false);
        this.loadLinkedPeople(result.capture.linkedPersonIds);
        this.loadLinkedInitiatives(result.capture.linkedInitiativeIds);
        if (result.warnings.length > 0) {
          for (const warning of result.warnings) {
            this.messageService.add({ severity: 'warn', summary: 'Skipped item', detail: warning, life: 8000 });
          }
        }
        this.messageService.add({ severity: 'success', summary: 'Entities created from extraction' });
      },
      error: () => {
        this.confirming.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to confirm extraction' });
      },
    });
  }

  protected discardExtraction(): void {
    const c = this.capture();
    if (!c) return;

    this.discarding.set(true);
    this.capturesService.discardExtraction(c.id).subscribe({
      next: (updated) => {
        this.capture.set(updated);
        this.discarding.set(false);
        this.messageService.add({ severity: 'info', summary: 'Extraction discarded' });
      },
      error: () => {
        this.discarding.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to discard extraction' });
      },
    });
  }

  protected saveMetadata(): void {
    const c = this.capture();
    if (!c) return;

    this.savingMetadata.set(true);
    this.capturesService.updateMetadata(c.id, {
      title: this.editTitle().trim() || null,
      source: this.editSource().trim() || null,
    }).subscribe({
      next: (updated) => {
        this.capture.set(updated);
        this.editTitle.set(updated.title ?? '');
        this.editSource.set(updated.source ?? '');
        this.savingMetadata.set(false);
        this.messageService.add({ severity: 'success', summary: 'Metadata updated' });
      },
      error: () => {
        this.savingMetadata.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to update metadata' });
      },
    });
  }

  protected searchPeople(event: AutoCompleteCompleteEvent): void {
    const c = this.capture();
    if (!c) return;

    this.peopleService.list().subscribe({
      next: (people) => {
        const linkedIds = new Set(c.linkedPersonIds);
        this.peopleSuggestions.set(
          people.filter((p) => !linkedIds.has(p.id) && p.name.toLowerCase().includes(event.query.toLowerCase()))
        );
      },
    });
  }

  protected linkPerson(): void {
    const c = this.capture();
    if (!c || !this.selectedPerson()) return;

    this.linkingPerson.set(true);
    const personToLink = this.selectedPerson()!;
    this.capturesService.linkPerson(c.id, personToLink.id).subscribe({
      next: (updated) => {
        this.capture.set(updated);
        this.linkedPeople.update((list) =>
          list.some((p) => p.id === personToLink.id) ? list : [...list, personToLink]
        );
        this.selectedPerson.set(null);
        this.linkingPerson.set(false);
        this.messageService.add({ severity: 'success', summary: 'Person linked' });
      },
      error: () => {
        this.linkingPerson.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to link person' });
      },
    });
  }

  protected unlinkPerson(personId: string): void {
    const c = this.capture();
    if (!c) return;

    this.capturesService.unlinkPerson(c.id, personId).subscribe({
      next: (updated) => {
        this.capture.set(updated);
        this.linkedPeople.update((list) => list.filter((p) => p.id !== personId));
        this.messageService.add({ severity: 'success', summary: 'Person unlinked' });
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Failed to unlink person' });
      },
    });
  }

  protected searchInitiatives(event: AutoCompleteCompleteEvent): void {
    const c = this.capture();
    if (!c) return;

    this.initiativesService.list().subscribe({
      next: (initiatives) => {
        const linkedIds = new Set(c.linkedInitiativeIds);
        this.initiativeSuggestions.set(
          initiatives.filter((i) => !linkedIds.has(i.id) && i.title.toLowerCase().includes(event.query.toLowerCase()))
        );
      },
    });
  }

  protected linkInitiative(): void {
    const c = this.capture();
    if (!c || !this.selectedInitiative()) return;

    this.linkingInitiative.set(true);
    const initiativeToLink = this.selectedInitiative()!;
    this.capturesService.linkInitiative(c.id, initiativeToLink.id).subscribe({
      next: (updated) => {
        this.capture.set(updated);
        this.linkedInitiatives.update((list) =>
          list.some((i) => i.id === initiativeToLink.id) ? list : [...list, initiativeToLink]
        );
        this.selectedInitiative.set(null);
        this.linkingInitiative.set(false);
        this.messageService.add({ severity: 'success', summary: 'Initiative linked' });
      },
      error: () => {
        this.linkingInitiative.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to link initiative' });
      },
    });
  }

  protected unlinkInitiative(initiativeId: string): void {
    const c = this.capture();
    if (!c) return;

    this.capturesService.unlinkInitiative(c.id, initiativeId).subscribe({
      next: (updated) => {
        this.capture.set(updated);
        this.linkedInitiatives.update((list) => list.filter((i) => i.id !== initiativeId));
        this.messageService.add({ severity: 'success', summary: 'Initiative unlinked' });
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Failed to unlink initiative' });
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

  private loadCapture(id: string): void {
    this.loading.set(true);
    this.capturesService.get(id).subscribe({
      next: (capture) => {
        this.capture.set(capture);
        this.editTitle.set(capture.title ?? '');
        this.editSource.set(capture.source ?? '');
        this.loadLinkedPeople(capture.linkedPersonIds);
        this.loadLinkedInitiatives(capture.linkedInitiativeIds);
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

  private loadLinkedPeople(personIds: string[]): void {
    if (personIds.length === 0) {
      this.linkedPeople.set([]);
      return;
    }
    this.peopleService.list().subscribe({
      next: (people) => {
        const idSet = new Set(personIds);
        this.linkedPeople.set(people.filter((p) => idSet.has(p.id)));
      },
    });
  }

  private loadTranscript(id: string): void {
    this.capturesService.getTranscript(id).subscribe({
      next: (t) => this.transcript.set(t),
      error: () => this.transcript.set(null),
    });
  }

  protected retryTranscription(): void {
    const id = this.capture()?.id;
    if (!id) return;
    this.retryingTranscription.set(true);
    this.capturesService.retryTranscription(id).subscribe({
      next: (capture) => {
        this.capture.set(capture);
        this.loadTranscript(id);
        this.retryingTranscription.set(false);
        this.messageService.add({ severity: 'success', summary: 'Transcription retried' });
      },
      error: (err) => {
        this.retryingTranscription.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Retry failed',
          detail: err?.error?.errorCode ?? err?.error?.message ?? 'Unknown error',
        });
      },
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

  private loadLinkedInitiatives(initiativeIds: string[]): void {
    if (initiativeIds.length === 0) {
      this.linkedInitiatives.set([]);
      return;
    }
    this.initiativesService.list().subscribe({
      next: (initiatives) => {
        const idSet = new Set(initiativeIds);
        this.linkedInitiatives.set(initiatives.filter((i) => idSet.has(i.id)));
      },
    });
  }
}
