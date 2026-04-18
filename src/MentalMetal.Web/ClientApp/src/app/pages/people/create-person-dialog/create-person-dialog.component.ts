import { ChangeDetectionStrategy, Component, inject, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ChipModule } from 'primeng/chip';
import { PeopleService } from '../../../shared/services/people.service';
import { CreatePersonRequest, Person, PersonType } from '../../../shared/models/person.model';

@Component({
  selector: 'app-create-person-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, DialogModule, InputTextModule, SelectModule, ChipModule],
  template: `
    <p-dialog
      header="Add Person"
      [visible]="visible()"
      (visibleChange)="visible.set($event)"
      [modal]="true"
      [style]="{ width: '28rem' }"
    >
      <div class="flex flex-col gap-4 pt-4">
        <div class="flex flex-col gap-2">
          <label for="personName" class="text-sm font-medium text-muted-color">Name *</label>
          <input pInputText id="personName" [(ngModel)]="name" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="personType" class="text-sm font-medium text-muted-color">Type *</label>
          <p-select
            id="personType"
            [options]="typeOptions"
            [(ngModel)]="type"
            placeholder="Select type"
            class="w-full"
          />
        </div>

        <div class="flex flex-col gap-2">
          <label for="personEmail" class="text-sm font-medium text-muted-color">Email</label>
          <input pInputText id="personEmail" [(ngModel)]="email" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="personRole" class="text-sm font-medium text-muted-color">Role</label>
          <input pInputText id="personRole" [(ngModel)]="role" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="personTeam" class="text-sm font-medium text-muted-color">Team</label>
          <input pInputText id="personTeam" [(ngModel)]="team" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="aliasInput" class="text-sm font-medium text-muted-color">Aliases</label>
          <div class="flex gap-2">
            <input pInputText id="aliasInput" [(ngModel)]="newAlias" class="flex-1" placeholder="Add alias..." (keydown.enter)="addAlias()" />
            <p-button icon="pi pi-plus" [outlined]="true" size="small" (onClick)="addAlias()" [disabled]="!newAlias.trim()" />
          </div>
          @if (aliases.length > 0) {
            <div class="flex flex-wrap gap-1 mt-1">
              @for (alias of aliases; track alias) {
                <p-chip [label]="alias" [removable]="true" (onRemove)="removeAlias(alias)" />
              }
            </div>
          }
        </div>
      </div>

      <ng-template #footer>
        <div class="flex justify-end gap-2">
          <p-button label="Cancel" severity="secondary" (onClick)="visible.set(false)" />
          <p-button
            label="Create"
            (onClick)="onSubmit()"
            [loading]="submitting()"
            [disabled]="!isValid()"
          />
        </div>
      </ng-template>
    </p-dialog>
  `,
})
export class CreatePersonDialogComponent {
  private readonly peopleService = inject(PeopleService);

  readonly visible = model(false);
  readonly created = output<Person>();

  protected readonly submitting = signal(false);
  protected name = '';
  protected type: PersonType | null = null;
  protected email = '';
  protected role = '';
  protected team = '';
  protected newAlias = '';
  protected aliases: string[] = [];

  protected readonly typeOptions = [
    { label: 'Direct Report', value: 'DirectReport' as PersonType },
    { label: 'Peer', value: 'Peer' as PersonType },
    { label: 'Stakeholder', value: 'Stakeholder' as PersonType },
    { label: 'External', value: 'External' as PersonType },
  ];

  protected isValid(): boolean {
    return this.name.trim().length > 0 && this.type !== null;
  }

  protected addAlias(): void {
    const trimmed = this.newAlias.trim();
    if (trimmed && !this.aliases.some(a => a.toLowerCase() === trimmed.toLowerCase())) {
      this.aliases = [...this.aliases, trimmed];
    }
    this.newAlias = '';
  }

  protected removeAlias(alias: string): void {
    this.aliases = this.aliases.filter(a => a !== alias);
  }

  protected onSubmit(): void {
    if (!this.isValid() || !this.type) return;

    this.submitting.set(true);
    const request: CreatePersonRequest = {
      name: this.name.trim(),
      type: this.type,
      ...(this.email.trim() && { email: this.email.trim() }),
      ...(this.role.trim() && { role: this.role.trim() }),
      ...(this.team.trim() && { team: this.team.trim() }),
      ...(this.aliases.length > 0 && { aliases: this.aliases }),
    };

    this.peopleService.create(request).subscribe({
      next: (person) => {
        this.submitting.set(false);
        this.created.emit(person);
        this.resetForm();
        this.visible.set(false);
      },
      error: () => this.submitting.set(false),
    });
  }

  private resetForm(): void {
    this.name = '';
    this.type = null;
    this.email = '';
    this.role = '';
    this.team = '';
    this.newAlias = '';
    this.aliases = [];
  }
}
