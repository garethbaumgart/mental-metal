import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { PeopleService } from '../../../shared/services/people.service';
import { Person, PersonType } from '../../../shared/models/person.model';
import { CreatePersonDialogComponent } from '../create-person-dialog/create-person-dialog.component';

@Component({
  selector: 'app-people-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, SelectModule, TableModule, TagModule, CreatePersonDialogComponent],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">People</h1>
        <p-button label="Add Person" icon="pi pi-plus" (onClick)="showCreateDialog.set(true)" />
      </div>

      <div class="flex items-center gap-4">
        <p-select
          [options]="typeFilterOptions"
          [(ngModel)]="selectedType"
          (ngModelChange)="onTypeFilterChange()"
          placeholder="All Types"
          [showClear]="true"
          class="w-48"
        />
      </div>

      @if (loading()) {
        <div class="flex justify-center p-8">
          <i class="pi pi-spinner pi-spin text-2xl"></i>
        </div>
      } @else if (people().length === 0) {
        <div class="flex flex-col items-center gap-4 p-12">
          <i class="pi pi-users text-4xl text-muted-color"></i>
          <p class="text-muted-color">No people found. Add your first person to get started.</p>
        </div>
      } @else {
        <p-table
          [value]="people()"
          [rows]="20"
          [paginator]="people().length > 20"
          [rowHover]="true"
          styleClass="p-datatable-sm"
        >
          <ng-template #header>
            <tr>
              <th>Name</th>
              <th>Type</th>
              <th>Role</th>
              <th>Team</th>
            </tr>
          </ng-template>
          <ng-template #body let-person>
            <tr class="cursor-pointer" (click)="onRowClick(person)">
              <td class="font-medium">{{ person.name }}</td>
              <td>
                <p-tag [value]="formatType(person.type)" [severity]="typeSeverity(person.type)" />
              </td>
              <td>{{ person.role || '—' }}</td>
              <td>{{ person.team || '—' }}</td>
            </tr>
          </ng-template>
        </p-table>
      }

      <app-create-person-dialog
        [(visible)]="showCreateDialog"
        (created)="onPersonCreated($event)"
      />
    </div>
  `,
})
export class PeopleListComponent implements OnInit {
  private readonly peopleService = inject(PeopleService);
  private readonly router = inject(Router);

  readonly people = signal<Person[]>([]);
  readonly loading = signal(true);
  readonly showCreateDialog = signal(false);
  protected selectedType: PersonType | null = null;

  protected readonly typeFilterOptions = [
    { label: 'Direct Report', value: 'DirectReport' as PersonType },
    { label: 'Peer', value: 'Peer' as PersonType },
    { label: 'Stakeholder', value: 'Stakeholder' as PersonType },
    { label: 'Candidate', value: 'Candidate' as PersonType },
    { label: 'External', value: 'External' as PersonType },
  ];

  ngOnInit(): void {
    this.loadPeople();
  }

  protected onTypeFilterChange(): void {
    this.loadPeople();
  }

  protected onRowClick(person: Person): void {
    this.router.navigate(['/people', person.id]);
  }

  protected onPersonCreated(person: Person): void {
    this.people.update((list) => [...list, person]);
  }

  protected formatType(type: PersonType): string {
    switch (type) {
      case 'DirectReport': return 'Direct Report';
      case 'Peer': return 'Peer';
      case 'Stakeholder': return 'Stakeholder';
      case 'Candidate': return 'Candidate';
      case 'External': return 'External';
    }
  }

  protected typeSeverity(type: PersonType): 'info' | 'warn' | 'success' | 'secondary' | 'contrast' {
    switch (type) {
      case 'DirectReport': return 'info';
      case 'Peer': return 'success';
      case 'Stakeholder': return 'warn';
      case 'Candidate': return 'contrast';
      case 'External': return 'secondary';
    }
  }

  private loadPeople(): void {
    this.loading.set(true);
    this.peopleService.list(this.selectedType ?? undefined).subscribe({
      next: (people) => {
        this.people.set(people);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
