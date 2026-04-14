import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { InterviewsService } from '../../../shared/services/interviews.service';
import { PeopleService } from '../../../shared/services/people.service';
import {
  INTERVIEW_STAGES,
  Interview,
  InterviewStage,
} from '../../../shared/models/interview.model';
import { Person } from '../../../shared/models/person.model';

@Component({
  selector: 'app-interviews-pipeline',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    ButtonModule,
    CardModule,
    DialogModule,
    InputTextModule,
    SelectModule,
    TagModule,
    ToastModule,
  ],
  providers: [MessageService],
  template: `
    <p-toast />
    <div class="flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">Interviews</h1>
        <p-button label="New Interview" icon="pi pi-plus" (onClick)="openCreate()" />
      </div>

      @if (loading()) {
        <div class="flex justify-center p-8"><i class="pi pi-spinner pi-spin text-2xl"></i></div>
      } @else if (items().length === 0) {
        <div class="flex flex-col items-center gap-4 p-12 bg-surface-50 rounded-md">
          <i class="pi pi-briefcase text-4xl text-muted-color"></i>
          <p class="text-muted-color">No interviews yet. Start a pipeline with "New Interview".</p>
        </div>
      } @else {
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          @for (stage of stages; track stage) {
            @if (grouped()[stage]?.length) {
              <div class="flex flex-col gap-3">
                <div class="flex items-center justify-between">
                  <h2 class="text-sm font-semibold uppercase text-muted-color">{{ stageLabel(stage) }}</h2>
                  <p-tag [value]="grouped()[stage].length.toString()" severity="secondary" />
                </div>
                @for (iv of grouped()[stage]; track iv.id) {
                  <a [routerLink]="['/interviews', iv.id]" class="no-underline">
                    <p-card>
                      <div class="flex flex-col gap-2">
                        <div class="font-semibold">{{ iv.roleTitle }}</div>
                        <div class="text-sm text-muted-color">{{ personName(iv.candidatePersonId) }}</div>
                        @if (iv.scheduledAtUtc) {
                          <div class="text-xs text-muted-color">
                            <i class="pi pi-calendar"></i> {{ iv.scheduledAtUtc | date: 'mediumDate' }}
                          </div>
                        }
                        @if (iv.decision) {
                          <p-tag [value]="iv.decision" severity="info" />
                        }
                      </div>
                    </p-card>
                  </a>
                }
              </div>
            }
          }
        </div>
      }

      <p-dialog
        [(visible)]="showCreate"
        header="New Interview"
        [modal]="true"
        [style]="{ width: '480px' }"
      >
        <div class="flex flex-col gap-4">
          <div class="flex flex-col gap-2">
            <label for="person" class="text-sm font-medium text-muted-color">Candidate</label>
            <p-select id="person" [options]="peopleOptions()" [(ngModel)]="draftPersonId" placeholder="Select candidate" class="w-full" />
          </div>
          <div class="flex flex-col gap-2">
            <label for="role" class="text-sm font-medium text-muted-color">Role title</label>
            <input pInputText id="role" [(ngModel)]="draftRole" class="w-full" />
          </div>
        </div>
        <ng-template #footer>
          <p-button label="Cancel" [text]="true" (onClick)="showCreate.set(false)" />
          <p-button
            label="Create"
            (onClick)="submit()"
            [disabled]="!draftPersonId || !draftRole.trim()"
          />
        </ng-template>
      </p-dialog>
    </div>
  `,
})
export class InterviewsPipelineComponent implements OnInit {
  private readonly service = inject(InterviewsService);
  private readonly peopleService = inject(PeopleService);
  private readonly messageService = inject(MessageService);

  readonly items = signal<Interview[]>([]);
  readonly people = signal<Person[]>([]);
  readonly loading = signal(true);
  readonly showCreate = signal(false);

  protected readonly stages = INTERVIEW_STAGES;
  protected readonly grouped = computed(() => {
    const map: Record<string, Interview[]> = {};
    for (const stage of INTERVIEW_STAGES) map[stage] = [];
    for (const iv of this.items()) map[iv.stage].push(iv);
    return map;
  });

  protected draftPersonId: string | null = null;
  protected draftRole = '';

  protected peopleOptions() {
    return this.people().map((p) => ({ label: p.name, value: p.id }));
  }

  protected personName(id: string): string {
    return this.people().find((p) => p.id === id)?.name ?? '(unknown)';
  }

  protected stageLabel(stage: InterviewStage): string {
    return stage.replace(/([A-Z])/g, ' $1').trim();
  }

  ngOnInit(): void {
    this.peopleService.list().subscribe({ next: (p) => this.people.set(p) });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.list().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to load' });
      },
    });
  }

  openCreate(): void {
    this.draftPersonId = null;
    this.draftRole = '';
    this.showCreate.set(true);
  }

  submit(): void {
    if (!this.draftPersonId || !this.draftRole.trim()) return;
    this.service
      .create({ candidatePersonId: this.draftPersonId, roleTitle: this.draftRole.trim() })
      .subscribe({
        next: (iv) => {
          this.items.update((list) => [iv, ...list]);
          this.showCreate.set(false);
          this.messageService.add({ severity: 'success', summary: 'Interview created' });
        },
        error: () =>
          this.messageService.add({ severity: 'error', summary: 'Failed to create' }),
      });
  }
}
