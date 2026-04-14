import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { TabsModule } from 'primeng/tabs';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { InterviewsService } from '../../../shared/services/interviews.service';
import { PeopleService } from '../../../shared/services/people.service';
import {
  INTERVIEW_DECISIONS,
  INTERVIEW_STAGES,
  Interview,
  InterviewDecision,
  InterviewStage,
} from '../../../shared/models/interview.model';
import { Person } from '../../../shared/models/person.model';

@Component({
  selector: 'app-interview-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    ButtonModule,
    CardModule,
    InputTextModule,
    TextareaModule,
    SelectModule,
    TabsModule,
    TagModule,
    ToastModule,
  ],
  providers: [MessageService],
  template: `
    <p-toast />
    @if (loading()) {
      <div class="flex justify-center p-8"><i class="pi pi-spinner pi-spin text-2xl"></i></div>
    } @else if (interview(); as iv) {
      <div class="flex flex-col gap-6">
        <a routerLink="/interviews" class="text-sm text-muted-color"><i class="pi pi-arrow-left"></i> Back to pipeline</a>

        <div class="flex items-center gap-4 flex-wrap">
          <h1 class="text-2xl font-bold flex-1">{{ iv.roleTitle }}</h1>
          <p-tag [value]="stageLabel(iv.stage)" severity="info" />
          @if (iv.decision) {
            <p-tag [value]="iv.decision" severity="success" />
          }
        </div>
        <div class="text-sm text-muted-color">Candidate: {{ personName(iv.candidatePersonId) }}</div>

        <p-tabs value="overview">
          <p-tablist>
            <p-tab value="overview">Overview</p-tab>
            <p-tab value="scorecards">Scorecards</p-tab>
            <p-tab value="transcript">Transcript</p-tab>
            <p-tab value="ai">AI Analysis</p-tab>
          </p-tablist>
          <p-tabpanels>
            <p-tabpanel value="overview">
              <div class="flex flex-col gap-6">
                <section class="flex flex-col gap-3">
                  <h2 class="text-lg font-semibold">Advance stage</h2>
                  <div class="flex gap-3 items-center flex-wrap">
                    <p-select [options]="stageOptions" [(ngModel)]="targetStage" placeholder="Next stage" class="w-64" />
                    <p-button label="Advance" (onClick)="advance()" [disabled]="!targetStage()" />
                  </div>
                </section>

                <section class="flex flex-col gap-3">
                  <h2 class="text-lg font-semibold">Record decision</h2>
                  <div class="flex gap-3 items-center flex-wrap">
                    <p-select [options]="decisionOptions" [(ngModel)]="decisionDraft" placeholder="Decision" class="w-64" />
                    <p-button label="Record" (onClick)="recordDecision()" [disabled]="!decisionDraft()" />
                  </div>
                </section>

                <section class="flex flex-col gap-2 text-sm text-muted-color">
                  <div>Created: {{ iv.createdAtUtc | date: 'medium' }}</div>
                  <div>Updated: {{ iv.updatedAtUtc | date: 'medium' }}</div>
                  @if (iv.completedAtUtc) {
                    <div>Completed: {{ iv.completedAtUtc | date: 'medium' }}</div>
                  }
                </section>
              </div>
            </p-tabpanel>

            <p-tabpanel value="scorecards">
              <div class="flex flex-col gap-4">
                @if (iv.scorecards.length === 0) {
                  <p class="text-muted-color text-sm">No scorecards recorded yet.</p>
                } @else {
                  <div class="flex flex-col gap-3">
                    @for (card of iv.scorecards; track card.id) {
                      <p-card>
                        <div class="flex items-center gap-4 flex-wrap">
                          <div class="flex-1">
                            <div class="font-semibold">{{ card.competency }}</div>
                            @if (card.notes) {
                              <div class="text-sm text-muted-color">{{ card.notes }}</div>
                            }
                          </div>
                          <p-tag [value]="card.rating + '/5'" severity="info" />
                          <p-button icon="pi pi-trash" [text]="true" severity="danger" (onClick)="removeScorecard(card.id)" />
                        </div>
                      </p-card>
                    }
                  </div>
                }

                <div class="flex flex-col gap-3 p-4 bg-surface-50 rounded-md">
                  <h3 class="font-semibold">Add scorecard</h3>
                  <div class="flex flex-col gap-2">
                    <label for="comp" class="text-sm font-medium text-muted-color">Competency</label>
                    <input pInputText id="comp" [(ngModel)]="newCompetency" class="w-full" />
                  </div>
                  <div class="flex flex-col gap-2">
                    <label for="rating" class="text-sm font-medium text-muted-color">Rating (1-5)</label>
                    <p-select id="rating" [options]="ratingOptions" [(ngModel)]="newRating" class="w-full" />
                  </div>
                  <div class="flex flex-col gap-2">
                    <label for="notes" class="text-sm font-medium text-muted-color">Notes (optional)</label>
                    <textarea pTextarea id="notes" [(ngModel)]="newNotes" [rows]="2" class="w-full"></textarea>
                  </div>
                  <p-button label="Add" (onClick)="addScorecard()" [disabled]="!newCompetency().trim() || !newRating()" />
                </div>
              </div>
            </p-tabpanel>

            <p-tabpanel value="transcript">
              <div class="flex flex-col gap-3">
                <label for="tx" class="text-sm font-medium text-muted-color">Raw transcript</label>
                <textarea pTextarea id="tx" [(ngModel)]="transcriptDraft" [rows]="12" class="w-full"></textarea>
                <div>
                  <p-button label="Save transcript" (onClick)="saveTranscript()" [disabled]="!transcriptDraft().trim()" />
                </div>
              </div>
            </p-tabpanel>

            <p-tabpanel value="ai">
              <div class="flex flex-col gap-4">
                @if (!iv.transcript?.rawText) {
                  <p class="text-muted-color text-sm">Save a transcript first to enable AI analysis.</p>
                } @else {
                  <div>
                    <p-button label="Analyze transcript" icon="pi pi-sparkles" (onClick)="analyze()" [loading]="analyzing()" />
                  </div>
                  @if (iv.transcript?.summary) {
                    <p-card>
                      <div class="flex flex-col gap-3">
                        <h3 class="font-semibold">Summary</h3>
                        <p class="whitespace-pre-wrap">{{ iv.transcript!.summary }}</p>
                        @if (iv.transcript!.recommendedDecision) {
                          <div><strong>Recommended:</strong> {{ iv.transcript!.recommendedDecision }}</div>
                        }
                        @if (iv.transcript!.riskSignals.length) {
                          <div>
                            <strong>Risk signals:</strong>
                            <ul class="list-disc pl-6">
                              @for (r of iv.transcript!.riskSignals; track $index) {
                                <li>{{ r }}</li>
                              }
                            </ul>
                          </div>
                        }
                        <div class="text-xs text-muted-color">
                          Analyzed {{ iv.transcript!.analyzedAtUtc | date: 'medium' }}
                          @if (iv.transcript!.model) { · {{ iv.transcript!.model }} }
                        </div>
                      </div>
                    </p-card>
                  }
                }
              </div>
            </p-tabpanel>
          </p-tabpanels>
        </p-tabs>
      </div>
    } @else {
      <p class="text-muted-color">Interview not found.</p>
    }
  `,
})
export class InterviewDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(InterviewsService);
  private readonly peopleService = inject(PeopleService);
  private readonly messageService = inject(MessageService);

  readonly interview = signal<Interview | null>(null);
  readonly people = signal<Person[]>([]);
  readonly loading = signal(true);
  readonly analyzing = signal(false);

  protected readonly stageOptions = INTERVIEW_STAGES.map((s) => ({ label: this.stageLabel(s), value: s }));
  protected readonly decisionOptions = INTERVIEW_DECISIONS.map((d) => ({ label: d, value: d }));
  protected readonly ratingOptions = [1, 2, 3, 4, 5].map((r) => ({ label: r.toString(), value: r }));

  // Signals rather than plain fields: Angular is running zoneless here, so mutating a
  // plain property does not trigger OnPush change detection. ngModel two-way binding
  // against writable signals works natively in Angular 21.
  protected readonly targetStage = signal<InterviewStage | null>(null);
  protected readonly decisionDraft = signal<InterviewDecision | null>(null);
  protected readonly transcriptDraft = signal('');
  protected readonly newCompetency = signal('');
  protected readonly newRating = signal<number | null>(null);
  protected readonly newNotes = signal('');

  protected readonly id = computed(() => this.route.snapshot.paramMap.get('id') ?? '');

  ngOnInit(): void {
    this.peopleService.list().subscribe({ next: (p) => this.people.set(p) });
    this.load();
  }

  stageLabel(stage: InterviewStage): string {
    return stage.replace(/([A-Z])/g, ' $1').trim();
  }

  personName(id: string): string {
    return this.people().find((p) => p.id === id)?.name ?? '(unknown)';
  }

  private load(): void {
    const id = this.id();
    if (!id) return;
    this.loading.set(true);
    this.service.get(id).subscribe({
      next: (iv) => {
        this.interview.set(iv);
        this.transcriptDraft.set(iv.transcript?.rawText ?? '');
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to load' });
      },
    });
  }

  advance(): void {
    const target = this.targetStage();
    if (!target) return;
    this.service.advance(this.id(), { targetStage: target }).subscribe({
      next: (iv) => {
        this.interview.set(iv);
        this.targetStage.set(null);
        this.messageService.add({ severity: 'success', summary: 'Stage advanced' });
      },
      error: (err) =>
        this.messageService.add({
          severity: 'error',
          summary: err?.error?.error ?? 'Failed to advance',
        }),
    });
  }

  recordDecision(): void {
    const decision = this.decisionDraft();
    if (!decision) return;
    this.service.recordDecision(this.id(), { decision }).subscribe({
      next: (iv) => {
        this.interview.set(iv);
        this.decisionDraft.set(null);
        this.messageService.add({ severity: 'success', summary: 'Decision recorded' });
      },
      error: (err) =>
        this.messageService.add({
          severity: 'error',
          summary: err?.error?.error ?? 'Failed to record decision',
        }),
    });
  }

  addScorecard(): void {
    const competency = this.newCompetency().trim();
    const rating = this.newRating();
    if (!competency || !rating) return;
    this.service
      .addScorecard(this.id(), {
        competency,
        rating,
        notes: this.newNotes().trim() || null,
      })
      .subscribe({
        next: () => {
          this.newCompetency.set('');
          this.newRating.set(null);
          this.newNotes.set('');
          this.load();
          this.messageService.add({ severity: 'success', summary: 'Scorecard added' });
        },
        error: () =>
          this.messageService.add({ severity: 'error', summary: 'Failed to add scorecard' }),
      });
  }

  removeScorecard(scorecardId: string): void {
    this.service.removeScorecard(this.id(), scorecardId).subscribe({
      next: () => {
        this.load();
        this.messageService.add({ severity: 'success', summary: 'Scorecard removed' });
      },
      error: () =>
        this.messageService.add({ severity: 'error', summary: 'Failed to remove' }),
    });
  }

  saveTranscript(): void {
    const draft = this.transcriptDraft();
    if (!draft.trim()) return;
    this.service.setTranscript(this.id(), { rawText: draft }).subscribe({
      next: (iv) => {
        this.interview.set(iv);
        this.messageService.add({ severity: 'success', summary: 'Transcript saved' });
      },
      error: (err) =>
        this.messageService.add({
          severity: 'error',
          summary: err?.error?.error ?? 'Failed to save transcript',
        }),
    });
  }

  analyze(): void {
    this.analyzing.set(true);
    this.service.analyze(this.id()).subscribe({
      next: (result) => {
        this.analyzing.set(false);
        // Surface any non-fatal model-output issue (e.g. unrecognised recommendedDecision)
        // before reloading the interview. The warning is informational — the analysis was
        // still persisted.
        if (result?.warning) {
          this.messageService.add({
            severity: 'warn',
            summary: 'Analysis completed with warning',
            detail: result.warning,
            life: 8000,
          });
        } else {
          this.messageService.add({ severity: 'success', summary: 'Analysis complete' });
        }
        this.load();
      },
      error: (err) => {
        this.analyzing.set(false);
        this.messageService.add({
          severity: 'error',
          summary: err?.error?.error ?? 'Analysis failed',
        });
      },
    });
  }
}
