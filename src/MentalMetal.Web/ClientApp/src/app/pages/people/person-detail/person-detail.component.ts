import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
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
import { OneOnOnesService } from '../../../shared/services/one-on-ones.service';
import { ObservationsService } from '../../../shared/services/observations.service';
import { GoalsService } from '../../../shared/services/goals.service';
import { CommitmentsService } from '../../../shared/services/commitments.service';
import { DelegationsService } from '../../../shared/services/delegations.service';
import { OneOnOne } from '../../../shared/models/one-on-one.model';
import { Observation } from '../../../shared/models/observation.model';
import { Goal, PersonEvidenceSummary } from '../../../shared/models/goal.model';
import { Commitment } from '../../../shared/models/commitment.model';
import { Delegation } from '../../../shared/models/delegation.model';

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
        <div class="flex items-center gap-4">
          <p-button icon="pi pi-arrow-left" [text]="true" (onClick)="goBack()" />
          <h1 class="text-2xl font-bold flex-1">{{ person()!.name }}</h1>
          <p-tag [value]="formatType(person()!.type)" [severity]="typeSeverity(person()!.type)" />
        </div>

        <!-- Profile Section -->
        <section class="flex flex-col gap-4">
          <h2 class="text-xl font-semibold">Profile</h2>

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
        </section>

        <!-- Type Change Section -->
        <section class="flex flex-col gap-4">
          <h2 class="text-xl font-semibold">Change Type</h2>
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
        </section>

        <!-- Career Details (DirectReport only) -->
        @if (person()!.type === 'DirectReport') {
          <section class="flex flex-col gap-4">
            <h2 class="text-xl font-semibold">Career Details</h2>

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
          </section>
        }

        <!-- Candidate Details (Candidate only) -->
        @if (person()!.type === 'Candidate') {
          <section class="flex flex-col gap-4">
            <h2 class="text-xl font-semibold">Candidate Details</h2>

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
          </section>
        }

        <!-- People-Lens Sections -->
        <section class="flex flex-col gap-3 border-t pt-6">
          <h2 class="text-xl font-semibold">Recent 1:1s</h2>
          @if (recentOneOnOnes().length === 0) {
            <p class="text-muted-color text-sm">No one-on-ones recorded.</p>
          } @else {
            <ul class="flex flex-col gap-2">
              @for (o of recentOneOnOnes(); track o.id) {
                <li class="flex flex-col gap-1 p-3 rounded bg-surface-50">
                  <div class="flex items-center gap-2">
                    <span class="font-medium">{{ o.occurredAt | date: 'mediumDate' }}</span>
                    @if (o.moodRating !== null) {
                      <p-tag [value]="'mood ' + o.moodRating + '/5'" severity="info" />
                    }
                  </div>
                  @if (o.notes) {
                    <p class="text-sm">{{ o.notes }}</p>
                  }
                  @if (o.actionItems.length > 0 || o.followUps.length > 0) {
                    <p class="text-xs text-muted-color">
                      {{ o.actionItems.length }} action items · {{ o.followUps.length }} follow-ups
                    </p>
                  }
                </li>
              }
            </ul>
          }
        </section>

        <section class="flex flex-col gap-3">
          <h2 class="text-xl font-semibold">Observations</h2>
          @if (observations().length === 0) {
            <p class="text-muted-color text-sm">No observations recorded.</p>
          } @else {
            <ul class="flex flex-col gap-2">
              @for (obs of observations(); track obs.id) {
                <li class="flex items-start gap-2 p-3 rounded bg-surface-50">
                  <p-tag [value]="obs.tag" [severity]="observationSeverity(obs.tag)" />
                  <div class="flex flex-col gap-1 flex-1">
                    <p class="text-sm">{{ obs.description }}</p>
                    <span class="text-xs text-muted-color">{{ obs.occurredAt | date: 'mediumDate' }}</span>
                  </div>
                </li>
              }
            </ul>
          }
        </section>

        <section class="flex flex-col gap-3">
          <h2 class="text-xl font-semibold">Active Goals</h2>
          @if (activeGoals().length === 0) {
            <p class="text-muted-color text-sm">No active goals.</p>
          } @else {
            <ul class="flex flex-col gap-2">
              @for (g of activeGoals(); track g.id) {
                <li class="flex flex-col gap-1 p-3 rounded bg-surface-50">
                  <div class="flex items-center gap-2">
                    <span class="font-medium">{{ g.title }}</span>
                    <p-tag [value]="g.goalType" severity="info" />
                  </div>
                  @if (g.targetDate) {
                    <span class="text-xs text-muted-color">Target {{ g.targetDate | date: 'mediumDate' }}</span>
                  }
                  @if (g.checkIns.length > 0 && g.checkIns[0].progress !== null) {
                    <span class="text-xs text-muted-color">Latest progress: {{ g.checkIns[0].progress }}%</span>
                  }
                </li>
              }
            </ul>
          }
        </section>

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

        <section class="flex flex-col gap-3">
          <h2 class="text-xl font-semibold">Active Delegations</h2>
          @if (activeDelegations().length === 0) {
            <p class="text-muted-color text-sm">No active delegations.</p>
          } @else {
            <ul class="flex flex-col gap-2">
              @for (d of activeDelegations(); track d.id) {
                <li class="flex items-center gap-2 p-3 rounded bg-surface-50">
                  <p-tag [value]="d.status" severity="info" />
                  <span class="flex-1 text-sm">{{ d.description }}</span>
                </li>
              }
            </ul>
          }
        </section>

        <section class="flex flex-col gap-3">
          <h2 class="text-xl font-semibold">Performance Evidence (current quarter)</h2>
          @if (evidence()?.hasAny) {
            <div class="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
              <div class="p-3 rounded bg-surface-50"><div class="text-xs text-muted-color">Wins</div><div class="text-lg font-semibold">{{ evidence()!.observationsWin }}</div></div>
              <div class="p-3 rounded bg-surface-50"><div class="text-xs text-muted-color">Concerns</div><div class="text-lg font-semibold">{{ evidence()!.observationsConcern }}</div></div>
              <div class="p-3 rounded bg-surface-50"><div class="text-xs text-muted-color">Goals Achieved</div><div class="text-lg font-semibold">{{ evidence()!.goalsAchieved }}</div></div>
              <div class="p-3 rounded bg-surface-50"><div class="text-xs text-muted-color">Goals Missed</div><div class="text-lg font-semibold">{{ evidence()!.goalsMissed }}</div></div>
              <div class="p-3 rounded bg-surface-50"><div class="text-xs text-muted-color">Commitments (on time)</div><div class="text-lg font-semibold">{{ evidence()!.commitmentsCompletedOnTime }}</div></div>
              <div class="p-3 rounded bg-surface-50"><div class="text-xs text-muted-color">Commitments (late)</div><div class="text-lg font-semibold">{{ evidence()!.commitmentsCompletedLate }}</div></div>
              <div class="p-3 rounded bg-surface-50"><div class="text-xs text-muted-color">Delegations complete</div><div class="text-lg font-semibold">{{ evidence()!.delegationsCompleted }}</div></div>
              <div class="p-3 rounded bg-surface-50"><div class="text-xs text-muted-color">Delegations active</div><div class="text-lg font-semibold">{{ evidence()!.delegationsInProgress }}</div></div>
            </div>
          } @else {
            <p class="text-muted-color text-sm">No evidence recorded in this period.</p>
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
  private readonly oneOnOnesService = inject(OneOnOnesService);
  private readonly observationsService = inject(ObservationsService);
  private readonly goalsService = inject(GoalsService);
  private readonly commitmentsService = inject(CommitmentsService);
  private readonly delegationsService = inject(DelegationsService);

  readonly person = signal<Person | null>(null);
  readonly loading = signal(true);
  readonly savingProfile = signal(false);
  readonly savingCareer = signal(false);
  readonly savingCandidate = signal(false);
  readonly changingType = signal(false);
  readonly advancingPipeline = signal(false);

  readonly recentOneOnOnes = signal<OneOnOne[]>([]);
  readonly observations = signal<Observation[]>([]);
  readonly activeGoals = signal<Goal[]>([]);
  readonly openCommitments = signal<Commitment[]>([]);
  readonly activeDelegations = signal<Delegation[]>([]);
  readonly evidence = signal<PersonEvidenceSummary | null>(null);

  protected observationSeverity(tag: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (tag) {
      case 'Win': return 'success';
      case 'Growth': return 'info';
      case 'FeedbackGiven': return 'secondary';
      case 'Concern': return 'danger';
      default: return 'secondary';
    }
  }

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
    this.oneOnOnesService.list(personId).subscribe({
      next: (list) => this.recentOneOnOnes.set(list.slice(0, 5)),
    });
    this.observationsService.list(personId).subscribe({
      next: (list) => this.observations.set(list.slice(0, 10)),
    });
    this.goalsService.list(personId, undefined, 'Active').subscribe({
      next: (list) => this.activeGoals.set(list),
    });
    this.commitmentsService.list(undefined, 'Open', personId).subscribe({
      next: (list) => this.openCommitments.set(list),
    });
    this.delegationsService.list(undefined, undefined, personId).subscribe({
      next: (list) => this.activeDelegations.set(list.filter((d) => d.status !== 'Completed')),
    });
    this.goalsService.getPersonEvidenceSummary(personId).subscribe({
      next: (s) => this.evidence.set(s),
    });
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
