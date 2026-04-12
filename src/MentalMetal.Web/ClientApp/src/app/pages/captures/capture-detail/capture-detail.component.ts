import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { ChipModule } from 'primeng/chip';
import { ToastModule } from 'primeng/toast';
import { AutoCompleteModule, AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { MessageService } from 'primeng/api';
import { CapturesService } from '../../../shared/services/captures.service';
import { PeopleService } from '../../../shared/services/people.service';
import { InitiativesService } from '../../../shared/services/initiatives.service';
import { Capture, CaptureType, ProcessingStatus } from '../../../shared/models/capture.model';
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
    AutoCompleteModule,
  ],
  styles: [`
    .content-block {
      border-color: var(--p-surface-200);
      background-color: var(--p-surface-50);
    }
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
          <p-tag [value]="formatStatus(capture()!.processingStatus)" [severity]="statusSeverity(capture()!.processingStatus)" />
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

        <!-- Raw Content -->
        <section class="flex flex-col gap-4">
          <h2 class="text-xl font-semibold">Content</h2>
          <div class="p-4 rounded-md border content-block whitespace-pre-wrap text-sm">{{ capture()!.rawContent }}</div>
        </section>

        <!-- Metadata Section -->
        <section class="flex flex-col gap-4">
          <h2 class="text-xl font-semibold">Metadata</h2>
          <div class="flex items-end gap-4">
            <div class="flex flex-col gap-2 flex-1">
              <label for="title" class="text-sm font-medium text-muted-color">Title</label>
              <input pInputText id="title" [(ngModel)]="editTitle" class="w-full" />
            </div>
            <div class="flex flex-col gap-2 flex-1">
              <label for="source" class="text-sm font-medium text-muted-color">Source</label>
              <input pInputText id="source" [(ngModel)]="editSource" class="w-full" />
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
                [(ngModel)]="selectedPerson"
                [suggestions]="peopleSuggestions()"
                (completeMethod)="searchPeople($event)"
                field="name"
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
              [disabled]="!selectedPerson"
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
                [(ngModel)]="selectedInitiative"
                [suggestions]="initiativeSuggestions()"
                (completeMethod)="searchInitiatives($event)"
                field="title"
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
              [disabled]="!selectedInitiative"
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
  readonly linkedPeople = signal<Person[]>([]);
  readonly linkedInitiatives = signal<Initiative[]>([]);
  readonly peopleSuggestions = signal<Person[]>([]);
  readonly initiativeSuggestions = signal<Initiative[]>([]);

  protected editTitle = '';
  protected editSource = '';
  protected selectedPerson: Person | null = null;
  protected selectedInitiative: Initiative | null = null;

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
    return this.editTitle !== (c.title ?? '') || this.editSource !== (c.source ?? '');
  }

  protected saveMetadata(): void {
    const c = this.capture();
    if (!c) return;

    this.savingMetadata.set(true);
    this.capturesService.updateMetadata(c.id, {
      title: this.editTitle.trim() || null,
      source: this.editSource.trim() || null,
    }).subscribe({
      next: (updated) => {
        this.capture.set(updated);
        this.editTitle = updated.title ?? '';
        this.editSource = updated.source ?? '';
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
    if (!c || !this.selectedPerson) return;

    this.linkingPerson.set(true);
    const personToLink = this.selectedPerson;
    this.capturesService.linkPerson(c.id, personToLink.id).subscribe({
      next: (updated) => {
        this.capture.set(updated);
        this.linkedPeople.update((list) =>
          list.some((p) => p.id === personToLink.id) ? list : [...list, personToLink]
        );
        this.selectedPerson = null;
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
    if (!c || !this.selectedInitiative) return;

    this.linkingInitiative.set(true);
    const initiativeToLink = this.selectedInitiative;
    this.capturesService.linkInitiative(c.id, initiativeToLink.id).subscribe({
      next: (updated) => {
        this.capture.set(updated);
        this.linkedInitiatives.update((list) =>
          list.some((i) => i.id === initiativeToLink.id) ? list : [...list, initiativeToLink]
        );
        this.selectedInitiative = null;
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
    }
  }

  protected typeSeverity(type: CaptureType): 'info' | 'warn' | 'success' {
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

  private loadCapture(id: string): void {
    this.loading.set(true);
    this.capturesService.get(id).subscribe({
      next: (capture) => {
        this.capture.set(capture);
        this.editTitle = capture.title ?? '';
        this.editSource = capture.source ?? '';
        this.loadLinkedPeople(capture.linkedPersonIds);
        this.loadLinkedInitiatives(capture.linkedInitiativeIds);
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
