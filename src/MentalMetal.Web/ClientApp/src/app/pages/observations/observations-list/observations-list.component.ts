import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { ObservationsService } from '../../../shared/services/observations.service';
import { PeopleService } from '../../../shared/services/people.service';
import { Observation, ObservationTag } from '../../../shared/models/observation.model';
import { Person } from '../../../shared/models/person.model';

@Component({
  selector: 'app-observations-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    DatePipe,
    ButtonModule,
    DialogModule,
    InputTextModule,
    TextareaModule,
    SelectModule,
    TableModule,
    TagModule,
    ToastModule,
  ],
  providers: [MessageService],
  template: `
    <p-toast />
    <div class="flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">Observations</h1>
        <p-button label="New Observation" icon="pi pi-plus" (onClick)="openCreate()" />
      </div>

      <div class="flex gap-4 flex-wrap">
        <p-select
          [options]="tagFilterOptions"
          [(ngModel)]="filterTag"
          (ngModelChange)="load()"
          placeholder="All tags"
          [showClear]="true"
          class="w-48"
        />
      </div>

      @if (loading()) {
        <div class="flex justify-center p-8"><i class="pi pi-spinner pi-spin text-2xl"></i></div>
      } @else if (items().length === 0) {
        <div class="flex flex-col items-center gap-4 p-12">
          <i class="pi pi-eye text-4xl text-muted-color"></i>
          <p class="text-muted-color">No observations yet.</p>
        </div>
      } @else {
        <p-table [value]="items()" [rowHover]="true" styleClass="p-datatable-sm">
          <ng-template #header>
            <tr>
              <th>Date</th>
              <th>Person</th>
              <th>Tag</th>
              <th>Description</th>
              <th></th>
            </tr>
          </ng-template>
          <ng-template #body let-row>
            <tr>
              <td>{{ row.occurredAt | date: 'mediumDate' }}</td>
              <td>{{ personName(row.personId) }}</td>
              <td><p-tag [value]="row.tag" [severity]="tagSeverity(row.tag)" /></td>
              <td>{{ row.description }}</td>
              <td>
                <p-button icon="pi pi-trash" [text]="true" severity="danger" (onClick)="remove(row)" />
              </td>
            </tr>
          </ng-template>
        </p-table>
      }

      <p-dialog
        [(visible)]="showCreate"
        header="New Observation"
        [modal]="true"
        [style]="{ width: '480px' }"
      >
        <div class="flex flex-col gap-4">
          <div class="flex flex-col gap-2">
            <label for="person" class="text-sm font-medium text-muted-color">Person</label>
            <p-select id="person" [options]="peopleOptions()" [(ngModel)]="draftPersonId" placeholder="Select person" class="w-full" />
          </div>
          <div class="flex flex-col gap-2">
            <label for="desc" class="text-sm font-medium text-muted-color">Description</label>
            <textarea pTextarea id="desc" [(ngModel)]="draftDescription" [rows]="3" class="w-full"></textarea>
          </div>
          <div class="flex flex-col gap-2">
            <label for="tag" class="text-sm font-medium text-muted-color">Tag</label>
            <p-select id="tag" [options]="tagOptions" [(ngModel)]="draftTag" placeholder="Select tag" class="w-full" />
          </div>
        </div>
        <ng-template #footer>
          <p-button label="Cancel" [text]="true" (onClick)="showCreate.set(false)" />
          <p-button
            label="Create"
            (onClick)="submit()"
            [disabled]="!draftPersonId || !draftDescription.trim() || !draftTag"
          />
        </ng-template>
      </p-dialog>
    </div>
  `,
})
export class ObservationsListComponent implements OnInit {
  private readonly service = inject(ObservationsService);
  private readonly peopleService = inject(PeopleService);
  private readonly messageService = inject(MessageService);

  readonly items = signal<Observation[]>([]);
  readonly people = signal<Person[]>([]);
  readonly loading = signal(true);
  readonly showCreate = signal(false);

  protected filterTag: ObservationTag | null = null;
  protected readonly tagOptions = [
    { label: 'Win', value: 'Win' },
    { label: 'Growth', value: 'Growth' },
    { label: 'Concern', value: 'Concern' },
    { label: 'Feedback Given', value: 'FeedbackGiven' },
  ];
  protected readonly tagFilterOptions = this.tagOptions;

  protected draftPersonId: string | null = null;
  protected draftDescription = '';
  protected draftTag: ObservationTag | null = null;

  protected peopleOptions() {
    return this.people().map((p) => ({ label: p.name, value: p.id }));
  }

  protected personName(id: string): string {
    return this.people().find((p) => p.id === id)?.name ?? '(unknown)';
  }

  protected tagSeverity(tag: ObservationTag): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (tag) {
      case 'Win': return 'success';
      case 'Growth': return 'info';
      case 'FeedbackGiven': return 'secondary';
      case 'Concern': return 'danger';
    }
  }

  ngOnInit(): void {
    this.peopleService.list().subscribe({ next: (p) => this.people.set(p) });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.list(undefined, this.filterTag ?? undefined).subscribe({
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
    this.draftDescription = '';
    this.draftTag = null;
    this.showCreate.set(true);
  }

  submit(): void {
    if (!this.draftPersonId || !this.draftTag || !this.draftDescription.trim()) return;
    this.service
      .create({
        personId: this.draftPersonId,
        description: this.draftDescription.trim(),
        tag: this.draftTag,
      })
      .subscribe({
        next: (o) => {
          this.items.update((list) => [o, ...list]);
          this.showCreate.set(false);
          this.messageService.add({ severity: 'success', summary: 'Observation recorded' });
        },
        error: () =>
          this.messageService.add({ severity: 'error', summary: 'Failed to create' }),
      });
  }

  remove(o: Observation): void {
    this.service.delete(o.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((x) => x.id !== o.id));
        this.messageService.add({ severity: 'success', summary: 'Observation deleted' });
      },
      error: () =>
        this.messageService.add({ severity: 'error', summary: 'Failed to delete' }),
    });
  }
}
