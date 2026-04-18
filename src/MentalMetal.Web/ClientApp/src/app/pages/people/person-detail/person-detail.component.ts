import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { PeopleService } from '../../../shared/services/people.service';
import { Person, PersonType, PipelineStatus } from '../../../shared/models/person.model';
import { CommitmentsService } from '../../../shared/services/commitments.service';
import { Commitment } from '../../../shared/models/commitment.model';

@Component({
  selector: 'app-person-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    DatePipe,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    SelectModule,
    TagModule,
    ToastModule,
    ConfirmDialogModule,
  ],
  providers: [MessageService, ConfirmationService],
  template: `
    <p-toast />
    <p-confirmDialog />

    @if (loading()) {
      <div class="flex justify-center p-8">
        <i class="pi pi-spinner pi-spin text-2xl"></i>
      </div>
    } @else if (person()) {
      <div class="max-w-2xl mx-auto flex flex-col gap-8">
        <!-- Header -->
        <div class="flex items-center gap-4 flex-wrap">
          <p-button icon="pi pi-arrow-left" [text]="true" (onClick)="goBack()" />
          <h1 class="text-2xl font-bold flex-1">{{ person()!.name }}</h1>
          <p-tag [value]="formatType(person()!.type)" [severity]="typeSeverity(person()!.type)" />
        </div>

        <section class="flex flex-col gap-3">
          <h2 class="text-xl font-semibold">Open Commitments</h2>
          @if (openCommitments().length === 0) {
            <p class="text-muted-color text-sm">No open commitments.</p>
          } @else {
            <ul class="flex flex-col gap-2">
              @for (c of openCommitments(); track c.id) {
                <li class="flex items-center gap-2 p-3 rounded bg-surface-50">
                  <p-tag [value]="c.direction === 'MineToThem' ? 'Mine → Them' : 'Theirs → Me'" severity="secondary" />
                  <span class="flex-1 text-sm">{{ c.description }}</span>
                  @if (c.dueDate) {
                    <span class="text-xs text-muted-color">Due {{ c.dueDate | date: 'mediumDate' }}</span>
                  }
                </li>
              }
            </ul>
          }
        </section>

        <!-- Profile & Details (read-only by default; expand to edit) -->
        <section class="flex flex-col gap-4 border-t pt-6">
          <div class="flex items-center justify-between">
            <h2 class="text-xl font-semibold" id="profile-details-heading">Profile &amp; details</h2>
            <p-button
              [label]="profileEditOpen() ? 'Close' : 'Edit'"
              [icon]="profileEditOpen() ? 'pi pi-times' : 'pi pi-pencil'"
              severity="secondary"
              [text]="true"
              size="small"
              [attr.aria-expanded]="profileEditOpen()"
              aria-controls="profile-edit-panel"
              (onClick)="toggleProfileEdit()"
            />
          </div>

          <!-- Read-only summary (always visible) -->
          <dl class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
            <div>
              <dt class="text-xs text-muted-color">Name</dt>
              <dd>{{ person()!.name }}</dd>
            </div>
            <div>
              <dt class="text-xs text-muted-color">Email</dt>
              <dd>{{ person()!.email || '—' }}</dd>
            </div>
            <div>
              <dt class="text-xs text-muted-color">Role</dt>
              <dd>{{ person()!.role || '—' }}</dd>
            </div>
            <div>
              <dt class="text-xs text-muted-color">Team</dt>
              <dd>{{ person()!.team || '—' }}</dd>
            </div>
            @if (person()!.notes) {
              <div class="sm:col-span-2">
                <dt class="text-xs text-muted-color">Notes</dt>
                <dd class="whitespace-pre-wrap">{{ person()!.notes }}</dd>
              </div>
            }
          </dl>

          @if (profileEditOpen()) {
            <!-- Editable Profile -->
            <div
              class="flex flex-col gap-4 pt-4 border-t"
              id="profile-edit-panel"
              role="region"
              aria-labelledby="profile-details-heading"
            >
              <h3 class="text-base font-semibold">Edit profile</h3>

              <div class="flex flex-col gap-2">
                <label for="name" class="text-sm font-medium text-muted-color">Name</label>
                <input pInputText id="name" [(ngModel)]="name" class="w-full" />
              </div>

              <div class="flex flex-col gap-2">
                <label for="email" class="text-sm font-medium text-muted-color">Email</label>
                <input pInputText id="email" [(ngModel)]="email" class="w-full" />
              </div>

              <div class="flex flex-col gap-2">
                <label for="role" class="text-sm font-medium text-muted-color">Role</label>
                <input pInputText id="role" [(ngModel)]="role" class="w-full" />
              </div>

              <div class="flex flex-col gap-2">
                <label for="team" class="text-sm font-medium text-muted-color">Team</label>
                <input pInputText id="team" [(ngModel)]="team" class="w-full" />
              </div>

              <div class="flex flex-col gap-2">
                <label for="notes" class="text-sm font-medium text-muted-color">Notes</label>
                <textarea pTextarea id="notes" [(ngModel)]="notes" [rows]="4" class="w-full"></textarea>
              </div>

              <p-button label="Save Profile" (onClick)="saveProfile()" [loading]="savingProfile()" />
            </div>

            <!-- Type Change -->
            <div class="flex flex-col gap-4 pt-4 border-t">
              <h3 class="text-base font-semibold">Change type</h3>
              <div class="flex items-end gap-4">
                <div class="flex flex-col gap-2 flex-1">
                  <label for="newType" class="text-sm font-medium text-muted-color">New Type</label>
                  <p-select
                    id="newType"
                    [options]="typeOptions"
                    [(ngModel)]="newType"
                    placeholder="Select new type"
                    class="w-full"
                  />
                </div>
                <p-button
                  label="Change"
                  severity="warn"
                  (onClick)="changeType()"
                  [loading]="changingType()"
                  [disabled]="!newType || newType === person()!.type"
                />
              </div>
            </div>

            @if (person()!.type === 'DirectReport') {
              <div class="flex flex-col gap-4 pt-4 border-t">
                <h3 class="text-base font-semibold">Career details</h3>

                <div class="flex flex-col gap-2">
                  <label for="level" class="text-sm font-medium text-muted-color">Level</label>
                  <input pInputText id="level" [(ngModel)]="careerLevel" class="w-full" />
                </div>

                <div class="flex flex-col gap-2">
                  <label for="aspirations" class="text-sm font-medium text-muted-color">Aspirations</label>
                  <textarea pTextarea id="aspirations" [(ngModel)]="careerAspirations" [rows]="3" class="w-full"></textarea>
                </div>

                <div class="flex flex-col gap-2">
                  <label for="growthAreas" class="text-sm font-medium text-muted-color">Growth Areas</label>
                  <textarea pTextarea id="growthAreas" [(ngModel)]="careerGrowthAreas" [rows]="3" class="w-full"></textarea>
                </div>

                <p-button label="Save Career Details" (onClick)="saveCareerDetails()" [loading]="savingCareer()" />
              </div>
            }

            @if (person()!.type === 'Candidate') {
              <div class="flex flex-col gap-4 pt-4 border-t">
                <h3 class="text-base font-semibold">Candidate details</h3>

                <div class="flex items-center gap-3">
                  <span class="text-sm font-medium text-muted-color">Pipeline Status:</span>
                  <p-tag
                    [value]="person()!.candidateDetails?.pipelineStatus ?? 'New'"
                    [severity]="pipelineSeverity(person()!.candidateDetails?.pipelineStatus ?? 'New')"
                  />
                </div>

                <div class="flex items-end gap-4">
                  <div class="flex flex-col gap-2 flex-1">
                    <label for="pipelineStatus" class="text-sm font-medium text-muted-color">Advance to</label>
                    <p-select
                      id="pipelineStatus"
                      [options]="pipelineOptions"
                      [(ngModel)]="newPipelineStatus"
                      placeholder="Select status"
                      class="w-full"
                    />
                  </div>
                  <p-button
                    label="Advance"
                    (onClick)="advancePipeline()"
                    [loading]="advancingPipeline()"
                    [disabled]="!newPipelineStatus"
                  />
                </div>

                <div class="flex flex-col gap-2">
                  <label for="cvNotes" class="text-sm font-medium text-muted-color">CV Notes</label>
                  <textarea pTextarea id="cvNotes" [(ngModel)]="cvNotes" [rows]="3" class="w-full"></textarea>
                </div>

                <div class="flex flex-col gap-2">
                  <label for="sourceChannel" class="text-sm font-medium text-muted-color">Source Channel</label>
                  <input pInputText id="sourceChannel" [(ngModel)]="sourceChannel" class="w-full" />
                </div>

                <p-button label="Save Candidate Details" (onClick)="saveCandidateDetails()" [loading]="savingCandidate()" />
              </div>
            }
          }
        </section>

        <!-- Archive -->
        <section class="flex flex-col gap-4 border-t pt-6">
          <p-button
            label="Archive Person"
            severity="danger"
            [outlined]="true"
            icon="pi pi-trash"
            (onClick)="confirmArchive()"
          />
        </section>
      </div>
    }
  `,
})
export class PersonDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly peopleService = inject(PeopleService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly commitmentsService = inject(CommitmentsService);

  readonly person = signal<Person | null>(null);
  readonly loading = signal(true);
  readonly savingProfile = signal(false);
  readonly savingCareer = signal(false);
  readonly savingCandidate = signal(false);
  readonly changingType = signal(false);
  readonly advancingPipeline = signal(false);

  readonly profileEditOpen = signal(false);

  readonly openCommitments = signal<Commitment[]>([]);

  // Profile fields
  protected name = '';
  protected email = '';
  protected role = '';
  protected team = '';
  protected notes = '';

  // Type change
  protected newType: PersonType | null = null;

  // Career details
  protected careerLevel = '';
  protected careerAspirations = '';
  protected careerGrowthAreas = '';

  // Candidate details
  protected newPipelineStatus: PipelineStatus | null = null;
  protected cvNotes = '';
  protected sourceChannel = '';

  protected readonly typeOptions = [
    { label: 'Direct Report', value: 'DirectReport' as PersonType },
    { label: 'Stakeholder', value: 'Stakeholder' as PersonType },
    { label: 'Candidate', value: 'Candidate' as PersonType },
  ];

  protected readonly pipelineOptions = [
    { label: 'New', value: 'New' as PipelineStatus },
    { label: 'Screening', value: 'Screening' as PipelineStatus },
    { label: 'Interviewing', value: 'Interviewing' as PipelineStatus },
    { label: 'Offer Stage', value: 'OfferStage' as PipelineStatus },
    { label: 'Hired', value: 'Hired' as PipelineStatus },
    { label: 'Rejected', value: 'Rejected' as PipelineStatus },
    { label: 'Withdrawn', value: 'Withdrawn' as PipelineStatus },
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadPerson(id);
    }
  }

  protected goBack(): void {
    this.router.navigate(['/people']);
  }

  protected saveProfile(): void {
    const p = this.person();
    if (!p) return;

    this.savingProfile.set(true);
    this.peopleService.update(p.id, {
      name: this.name,
      email: this.email || null,
      role: this.role || null,
      team: this.team || null,
      notes: this.notes || null,
    }).subscribe({
      next: (updated) => {
        this.person.set(updated);
        this.savingProfile.set(false);
        this.messageService.add({ severity: 'success', summary: 'Profile updated' });
      },
      error: () => {
        this.savingProfile.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to update profile' });
      },
    });
  }

  protected changeType(): void {
    const p = this.person();
    if (!p || !this.newType) return;

    this.changingType.set(true);
    this.peopleService.changeType(p.id, this.newType).subscribe({
      next: (updated) => {
        this.person.set(updated);
        this.populateFields(updated);
        this.changingType.set(false);
        this.newType = null;
        this.messageService.add({ severity: 'success', summary: 'Type changed' });
      },
      error: () => {
        this.changingType.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to change type' });
      },
    });
  }

  protected saveCareerDetails(): void {
    const p = this.person();
    if (!p) return;

    this.savingCareer.set(true);
    this.peopleService.updateCareerDetails(p.id, {
      level: this.careerLevel || null,
      aspirations: this.careerAspirations || null,
      growthAreas: this.careerGrowthAreas || null,
    }).subscribe({
      next: (updated) => {
        this.person.set(updated);
        this.savingCareer.set(false);
        this.messageService.add({ severity: 'success', summary: 'Career details updated' });
      },
      error: () => {
        this.savingCareer.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to update career details' });
      },
    });
  }

  protected saveCandidateDetails(): void {
    const p = this.person();
    if (!p) return;

    this.savingCandidate.set(true);
    this.peopleService.updateCandidateDetails(p.id, {
      cvNotes: this.cvNotes || null,
      sourceChannel: this.sourceChannel || null,
    }).subscribe({
      next: (updated) => {
        this.person.set(updated);
        this.savingCandidate.set(false);
        this.messageService.add({ severity: 'success', summary: 'Candidate details updated' });
      },
      error: () => {
        this.savingCandidate.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to update candidate details' });
      },
    });
  }

  protected advancePipeline(): void {
    const p = this.person();
    if (!p || !this.newPipelineStatus) return;

    this.advancingPipeline.set(true);
    this.peopleService.advancePipeline(p.id, this.newPipelineStatus).subscribe({
      next: (updated) => {
        this.person.set(updated);
        this.advancingPipeline.set(false);
        this.newPipelineStatus = null;
        this.messageService.add({ severity: 'success', summary: 'Pipeline status updated' });
      },
      error: () => {
        this.advancingPipeline.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to advance pipeline' });
      },
    });
  }

  protected confirmArchive(): void {
    this.confirmationService.confirm({
      message: 'Are you sure you want to archive this person?',
      header: 'Confirm Archive',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.archive(),
    });
  }

  protected formatType(type: PersonType): string {
    switch (type) {
      case 'DirectReport': return 'Direct Report';
      case 'Stakeholder': return 'Stakeholder';
      case 'Candidate': return 'Candidate';
    }
  }

  protected typeSeverity(type: PersonType): 'info' | 'warn' | 'success' {
    switch (type) {
      case 'DirectReport': return 'info';
      case 'Stakeholder': return 'warn';
      case 'Candidate': return 'success';
    }
  }

  protected pipelineSeverity(status: PipelineStatus): 'info' | 'warn' | 'success' | 'danger' | 'secondary' {
    switch (status) {
      case 'New': return 'info';
      case 'Screening':
      case 'Interviewing':
      case 'OfferStage': return 'warn';
      case 'Hired': return 'success';
      case 'Rejected': return 'danger';
      case 'Withdrawn': return 'secondary';
    }
  }

  private archive(): void {
    const p = this.person();
    if (!p) return;

    this.peopleService.archive(p.id).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Person archived' });
        this.router.navigate(['/people']);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Failed to archive person' });
      },
    });
  }

  private loadPerson(id: string): void {
    this.loading.set(true);
    this.peopleService.get(id).subscribe({
      next: (person) => {
        this.person.set(person);
        this.populateFields(person);
        this.loading.set(false);
        this.loadLensData(id);
      },
      error: () => {
        this.loading.set(false);
        this.router.navigate(['/people']);
      },
    });
  }

  private loadLensData(personId: string): void {
    this.commitmentsService.list(undefined, 'Open', personId).subscribe({
      next: (list) => this.openCommitments.set(list),
    });
  }

  /**
   * Toggles the Profile & details edit panel. When opening, re-populate the
   * draft fields from the current person so stale edits from a previous
   * "close without save" cycle don't persist. When closing, clear any type-
   * change / pipeline draft state too.
   */
  protected toggleProfileEdit(): void {
    const next = !this.profileEditOpen();
    this.profileEditOpen.set(next);
    if (next) {
      const p = this.person();
      if (p) this.populateFields(p);
    } else {
      this.newType = null;
      this.newPipelineStatus = null;
    }
  }

  private populateFields(person: Person): void {
    this.name = person.name;
    this.email = person.email ?? '';
    this.role = person.role ?? '';
    this.team = person.team ?? '';
    this.notes = person.notes ?? '';
    this.careerLevel = person.careerDetails?.level ?? '';
    this.careerAspirations = person.careerDetails?.aspirations ?? '';
    this.careerGrowthAreas = person.careerDetails?.growthAreas ?? '';
    this.cvNotes = person.candidateDetails?.cvNotes ?? '';
    this.sourceChannel = person.candidateDetails?.sourceChannel ?? '';
  }
}
