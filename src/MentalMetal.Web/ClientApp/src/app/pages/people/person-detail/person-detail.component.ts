import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { ChipModule } from 'primeng/chip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { PeopleService } from '../../../shared/services/people.service';
import { Person, PersonType } from '../../../shared/models/person.model';
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
    ChipModule,
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

        <!-- Aliases -->
        <section class="flex flex-col gap-3">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-semibold">Aliases</h2>
          </div>
          <div class="flex flex-wrap gap-1">
            @for (alias of person()!.aliases; track alias) {
              <p-chip [label]="alias" [removable]="true" (onRemove)="removeAlias(alias)" />
            }
          </div>
          <div class="flex gap-2">
            <input pInputText [(ngModel)]="newAlias" class="flex-1" placeholder="Add alias..." (keydown.enter)="addAlias()" />
            <p-button icon="pi pi-plus" [outlined]="true" size="small" (onClick)="addAlias()" [disabled]="!newAlias.trim()" />
          </div>
        </section>

        <section class="flex flex-col gap-3">
          <h2 class="text-xl font-semibold">Open Commitments</h2>
          @if (openCommitments().length === 0) {
            <p class="text-muted-color text-sm">No open commitments.</p>
          } @else {
            <ul class="flex flex-col gap-2">
              @for (c of openCommitments(); track c.id) {
                <li class="flex items-center gap-2 p-3 rounded bg-surface-50">
                  <p-tag [value]="c.direction === 'MineToThem' ? 'Mine' : 'Theirs'" severity="secondary" />
                  <span class="flex-1 text-sm">{{ c.description }}</span>
                  @if (c.dueDate) {
                    <span class="text-xs text-muted-color">Due {{ c.dueDate | date: 'mediumDate' }}</span>
                  }
                </li>
              }
            </ul>
          }
        </section>

        <!-- Profile & Details -->
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

          <dl class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
            <div>
              <dt class="text-xs text-muted-color">Name</dt>
              <dd>{{ person()!.name }}</dd>
            </div>
            <div>
              <dt class="text-xs text-muted-color">Email</dt>
              <dd>{{ person()!.email || '\u2014' }}</dd>
            </div>
            <div>
              <dt class="text-xs text-muted-color">Role</dt>
              <dd>{{ person()!.role || '\u2014' }}</dd>
            </div>
            <div>
              <dt class="text-xs text-muted-color">Team</dt>
              <dd>{{ person()!.team || '\u2014' }}</dd>
            </div>
            @if (person()!.notes) {
              <div class="sm:col-span-2">
                <dt class="text-xs text-muted-color">Notes</dt>
                <dd class="whitespace-pre-wrap">{{ person()!.notes }}</dd>
              </div>
            }
          </dl>

          @if (profileEditOpen()) {
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
  readonly changingType = signal(false);

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

  // Alias
  protected newAlias = '';

  protected readonly typeOptions = [
    { label: 'Direct Report', value: 'DirectReport' as PersonType },
    { label: 'Peer', value: 'Peer' as PersonType },
    { label: 'Stakeholder', value: 'Stakeholder' as PersonType },
    { label: 'External', value: 'External' as PersonType },
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

  protected addAlias(): void {
    const p = this.person();
    if (!p || !this.newAlias.trim()) return;

    this.peopleService.addAlias(p.id, this.newAlias.trim()).subscribe({
      next: (updated) => {
        this.person.set(updated);
        this.newAlias = '';
        this.messageService.add({ severity: 'success', summary: 'Alias added' });
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Failed to add alias' });
      },
    });
  }

  protected removeAlias(alias: string): void {
    const p = this.person();
    if (!p) return;

    const remaining = p.aliases.filter(a => a !== alias);
    this.peopleService.setAliases(p.id, remaining).subscribe({
      next: (updated) => {
        this.person.set(updated);
        this.messageService.add({ severity: 'success', summary: 'Alias removed' });
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Failed to remove alias' });
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
      case 'Peer': return 'Peer';
      case 'Stakeholder': return 'Stakeholder';
      case 'External': return 'External';
    }
  }

  protected typeSeverity(type: PersonType): 'info' | 'warn' | 'success' | 'secondary' {
    switch (type) {
      case 'DirectReport': return 'info';
      case 'Peer': return 'success';
      case 'Stakeholder': return 'warn';
      case 'External': return 'secondary';
    }
  }

  protected toggleProfileEdit(): void {
    const next = !this.profileEditOpen();
    this.profileEditOpen.set(next);
    if (next) {
      const p = this.person();
      if (p) this.populateFields(p);
    } else {
      this.newType = null;
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

  private populateFields(person: Person): void {
    this.name = person.name;
    this.email = person.email ?? '';
    this.role = person.role ?? '';
    this.team = person.team ?? '';
    this.notes = person.notes ?? '';
  }
}
