import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { TagModule } from 'primeng/tag';
import { ChipModule } from 'primeng/chip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { AutoCompleteModule, AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { DatePickerModule } from 'primeng/datepicker';
import { TabsModule } from 'primeng/tabs';
import { ConfirmationService, MessageService } from 'primeng/api';
import { InitiativesService } from '../../../shared/services/initiatives.service';
import { PeopleService } from '../../../shared/services/people.service';
import { Initiative, InitiativeStatus, Milestone } from '../../../shared/models/initiative.model';
import { Person } from '../../../shared/models/person.model';
@Component({
  selector: 'app-initiative-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    TagModule,
    ChipModule,
    ToastModule,
    ConfirmDialogModule,
    AutoCompleteModule,
    DatePickerModule,
    TabsModule,
  ],
  styles: [`
    .milestone-card, .milestone-form {
      border-color: var(--p-content-border-color);
    }
  `],
  providers: [MessageService, ConfirmationService],
  template: `
    <p-toast />
    <p-confirmDialog />

    @if (loading()) {
      <div class="flex justify-center p-8">
        <i class="pi pi-spinner pi-spin text-2xl"></i>
      </div>
    } @else if (initiative()) {
      <div class="max-w-2xl mx-auto flex flex-col gap-8">
        <!-- Header -->
        <div class="flex items-center gap-4">
          <p-button icon="pi pi-arrow-left" [text]="true" (onClick)="goBack()" />
          <h1 class="text-2xl font-bold flex-1">{{ initiative()!.title }}</h1>
          <p-tag
            [value]="formatStatus(initiative()!.status)"
            [severity]="statusSeverity(initiative()!.status)"
          />
        </div>

        <p-tabs value="overview">
          <p-tablist>
            <p-tab value="overview">Overview</p-tab>
          </p-tablist>
          <p-tabpanels>
            <p-tabpanel value="overview">
              <div class="flex flex-col gap-8">

        <!-- Title Section -->
        <section class="flex flex-col gap-4">
          <h2 class="text-xl font-semibold">Title</h2>
          <div class="flex items-end gap-4">
            <div class="flex flex-col gap-2 flex-1">
              <label for="title" class="text-sm font-medium text-muted-color">Initiative Title</label>
              <input pInputText id="title" [(ngModel)]="title" class="w-full" />
            </div>
            <p-button
              label="Save"
              (onClick)="saveTitle()"
              [loading]="savingTitle()"
              [disabled]="!title.trim() || title.trim() === initiative()!.title"
            />
          </div>
        </section>

        <!-- Status Transitions -->
        @if (statusActions().length > 0) {
          <section class="flex flex-col gap-4">
            <h2 class="text-xl font-semibold">Status</h2>
            <div class="flex items-center gap-3">
              @for (action of statusActions(); track action.status) {
                <p-button
                  [label]="action.label"
                  [severity]="action.severity"
                  [outlined]="true"
                  (onClick)="changeStatus(action.status)"
                  [loading]="changingStatus()"
                />
              }
            </div>
          </section>
        }

        <!-- Milestones Section -->
        <section class="flex flex-col gap-4">
          <h2 class="text-xl font-semibold">Milestones</h2>

          @if (initiative()!.milestones.length === 0) {
            <p class="text-muted-color text-sm">No milestones yet.</p>
          } @else {
            <div class="flex flex-col gap-3">
              @for (milestone of initiative()!.milestones; track milestone.id) {
                <div class="flex items-center gap-3 p-3 rounded-md border milestone-card">
                  @if (milestone.isCompleted) {
                    <i class="pi pi-check-circle text-lg" style="color: var(--p-green-500)"></i>
                  } @else {
                    <i class="pi pi-circle text-lg text-muted-color"></i>
                  }
                  <div class="flex-1">
                    <div class="font-medium">{{ milestone.title }}</div>
                    <div class="text-sm text-muted-color">
                      {{ milestone.targetDate }}
                      @if (milestone.description) {
                        — {{ milestone.description }}
                      }
                    </div>
                  </div>
                  <div class="flex items-center gap-1">
                    @if (!milestone.isCompleted) {
                      <p-button
                        icon="pi pi-check"
                        [text]="true"
                        severity="success"

                        (onClick)="completeMilestone(milestone)"
                      />
                    }
                    <p-button
                      icon="pi pi-trash"
                      [text]="true"
                      severity="danger"

                      (onClick)="confirmRemoveMilestone(milestone)"
                    />
                  </div>
                </div>
              }
            </div>
          }

          <!-- Add Milestone Form -->
          <div class="flex flex-col gap-3 p-4 rounded-md border milestone-card">
            <h3 class="text-sm font-semibold">Add Milestone</h3>
            <div class="flex flex-col gap-2">
              <label for="msTitle" class="text-sm font-medium text-muted-color">Title *</label>
              <input pInputText id="msTitle" [(ngModel)]="newMilestoneTitle" class="w-full" />
            </div>
            <div class="flex flex-col gap-2">
              <label for="msDate" class="text-sm font-medium text-muted-color">Target Date *</label>
              <p-datepicker id="msDate" [(ngModel)]="newMilestoneDate" dateFormat="yy-mm-dd" class="w-full" />
            </div>
            <div class="flex flex-col gap-2">
              <label for="msDesc" class="text-sm font-medium text-muted-color">Description</label>
              <input pInputText id="msDesc" [(ngModel)]="newMilestoneDescription" class="w-full" />
            </div>
            <p-button
              label="Add Milestone"
              icon="pi pi-plus"
              [outlined]="true"
              (onClick)="addMilestone()"
              [loading]="addingMilestone()"
              [disabled]="!newMilestoneTitle.trim() || !newMilestoneDate"
            />
          </div>
        </section>

        <!-- Linked People Section -->
        <section class="flex flex-col gap-4">
          <h2 class="text-xl font-semibold">Linked People</h2>

          @if (linkedPeople().length === 0 && initiative()!.linkedPersonIds.length === 0) {
            <p class="text-muted-color text-sm">No linked people yet.</p>
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
                [forceSelection]="true"
                [minLength]="2"
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
              </div>
            </p-tabpanel>
          </p-tabpanels>
        </p-tabs>
      </div>
    }
  `,
})
export class InitiativeDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly initiativesService = inject(InitiativesService);
  private readonly peopleService = inject(PeopleService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly initiative = signal<Initiative | null>(null);
  readonly loading = signal(true);
  readonly savingTitle = signal(false);
  readonly changingStatus = signal(false);
  readonly addingMilestone = signal(false);
  readonly linkingPerson = signal(false);
  readonly linkedPeople = signal<Person[]>([]);
  readonly peopleSuggestions = signal<Person[]>([]);
  readonly statusActions = signal<StatusAction[]>([]);

  // Title editing
  protected title = '';

  // Milestone form
  protected newMilestoneTitle = '';
  protected newMilestoneDate: Date | null = null;
  protected newMilestoneDescription = '';

  // Person linking
  protected selectedPerson: Person | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadInitiative(id);
    }
  }

  protected goBack(): void {
    this.router.navigate(['/initiatives']);
  }

  protected saveTitle(): void {
    const i = this.initiative();
    if (!i || !this.title.trim()) return;

    this.savingTitle.set(true);
    this.initiativesService.updateTitle(i.id, { title: this.title.trim() }).subscribe({
      next: (updated) => {
        this.initiative.set(updated);
        this.title = updated.title;
        this.savingTitle.set(false);
        this.messageService.add({ severity: 'success', summary: 'Title updated' });
      },
      error: () => {
        this.savingTitle.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to update title' });
      },
    });
  }

  protected changeStatus(newStatus: InitiativeStatus): void {
    const i = this.initiative();
    if (!i) return;

    this.changingStatus.set(true);
    this.initiativesService.changeStatus(i.id, newStatus).subscribe({
      next: (updated) => {
        this.initiative.set(updated);
        this.updateStatusActions(updated.status);
        this.changingStatus.set(false);
        this.messageService.add({ severity: 'success', summary: 'Status changed' });
      },
      error: () => {
        this.changingStatus.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to change status' });
      },
    });
  }

  protected addMilestone(): void {
    const i = this.initiative();
    if (!i || !this.newMilestoneTitle.trim() || !this.newMilestoneDate) return;

    this.addingMilestone.set(true);
    const targetDate = this.formatDate(this.newMilestoneDate);
    this.initiativesService.addMilestone(i.id, {
      title: this.newMilestoneTitle.trim(),
      targetDate,
      ...(this.newMilestoneDescription.trim() && { description: this.newMilestoneDescription.trim() }),
    }).subscribe({
      next: (updated) => {
        this.initiative.set(updated);
        this.newMilestoneTitle = '';
        this.newMilestoneDate = null;
        this.newMilestoneDescription = '';
        this.addingMilestone.set(false);
        this.messageService.add({ severity: 'success', summary: 'Milestone added' });
      },
      error: () => {
        this.addingMilestone.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed to add milestone' });
      },
    });
  }

  protected completeMilestone(milestone: Milestone): void {
    const i = this.initiative();
    if (!i) return;

    this.initiativesService.completeMilestone(i.id, milestone.id).subscribe({
      next: (updated) => {
        this.initiative.set(updated);
        this.messageService.add({ severity: 'success', summary: 'Milestone completed' });
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Failed to complete milestone' });
      },
    });
  }

  protected confirmRemoveMilestone(milestone: Milestone): void {
    this.confirmationService.confirm({
      message: `Remove milestone "${milestone.title}"?`,
      header: 'Confirm Remove',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.removeMilestone(milestone),
    });
  }

  protected searchPeople(event: AutoCompleteCompleteEvent): void {
    const i = this.initiative();
    if (!i) return;

    this.peopleService.list().subscribe({
      next: (people) => {
        const linkedIds = new Set(i.linkedPersonIds);
        this.peopleSuggestions.set(
          people.filter((p) => !linkedIds.has(p.id) && p.name.toLowerCase().includes(event.query.toLowerCase()))
        );
      },
    });
  }

  protected linkPerson(): void {
    const i = this.initiative();
    if (!i || !this.selectedPerson) return;

    this.linkingPerson.set(true);
    const personToLink = this.selectedPerson;
    this.initiativesService.linkPerson(i.id, personToLink.id).subscribe({
      next: (updated) => {
        this.initiative.set(updated);
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
    const i = this.initiative();
    if (!i) return;

    this.initiativesService.unlinkPerson(i.id, personId).subscribe({
      next: () => {
        this.initiative.update((init) => init ? {
          ...init,
          linkedPersonIds: init.linkedPersonIds.filter((id) => id !== personId),
        } : null);
        this.linkedPeople.update((list) => list.filter((p) => p.id !== personId));
        this.messageService.add({ severity: 'success', summary: 'Person unlinked' });
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Failed to unlink person' });
      },
    });
  }

  protected formatStatus(status: InitiativeStatus): string {
    switch (status) {
      case 'Active': return 'Active';
      case 'OnHold': return 'On Hold';
      case 'Completed': return 'Completed';
      case 'Cancelled': return 'Cancelled';
    }
  }

  protected statusSeverity(status: InitiativeStatus): 'success' | 'warn' | 'info' | 'danger' {
    switch (status) {
      case 'Active': return 'success';
      case 'OnHold': return 'warn';
      case 'Completed': return 'info';
      case 'Cancelled': return 'danger';
    }
  }

  private removeMilestone(milestone: Milestone): void {
    const i = this.initiative();
    if (!i) return;

    this.initiativesService.removeMilestone(i.id, milestone.id).subscribe({
      next: () => {
        this.initiative.update((init) => init ? {
          ...init,
          milestones: init.milestones.filter((m) => m.id !== milestone.id),
        } : null);
        this.messageService.add({ severity: 'success', summary: 'Milestone removed' });
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Failed to remove milestone' });
      },
    });
  }

  private loadInitiative(id: string): void {
    this.loading.set(true);
    this.initiativesService.get(id).subscribe({
      next: (initiative) => {
        this.initiative.set(initiative);
        this.title = initiative.title;
        this.updateStatusActions(initiative.status);
        this.loadLinkedPeople(initiative.linkedPersonIds);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.router.navigate(['/initiatives']);
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

  private updateStatusActions(status: InitiativeStatus): void {
    switch (status) {
      case 'Active':
        this.statusActions.set([
          { label: 'Put On Hold', status: 'OnHold', severity: 'warn' },
          { label: 'Complete', status: 'Completed', severity: 'info' },
          { label: 'Cancel', status: 'Cancelled', severity: 'danger' },
        ]);
        break;
      case 'OnHold':
        this.statusActions.set([
          { label: 'Resume', status: 'Active', severity: 'success' },
        ]);
        break;
      default:
        this.statusActions.set([]);
        break;
    }
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}

interface StatusAction {
  label: string;
  status: InitiativeStatus;
  severity: 'success' | 'warn' | 'info' | 'danger';
}
