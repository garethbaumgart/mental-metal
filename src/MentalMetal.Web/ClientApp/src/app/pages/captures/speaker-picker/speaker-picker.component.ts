import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, shareReplay } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { AutoCompleteModule, AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { PeopleService } from '../../../shared/services/people.service';
import { Person } from '../../../shared/models/person.model';

/**
 * Compact speaker-identification picker. Opens inline below a speaker group,
 * lets the user type a Person name, and emits the selected PersonId back to
 * the parent which calls /api/captures/{id}/speakers.
 */
@Component({
  selector: 'app-speaker-picker',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, AutoCompleteModule],
  template: `
    <div class="flex items-center gap-2">
      <span class="text-xs text-muted-color">Link {{ speakerLabel() }} →</span>
      <p-autoComplete
        [(ngModel)]="selected"
        [suggestions]="suggestions()"
        (completeMethod)="search($event)"
        field="name"
        placeholder="Search person"
        [minLength]="1"
        [forceSelection]="true"
      />
      <p-button
        label="Link"
        size="small"
        [disabled]="!selected"
        (onClick)="emitLink()"
      />
      <p-button
        label="Cancel"
        severity="secondary"
        [text]="true"
        size="small"
        (onClick)="cancelled.emit()"
      />
    </div>
  `,
})
export class SpeakerPickerComponent {
  private readonly peopleService = inject(PeopleService);

  readonly speakerLabel = input.required<string>();
  readonly linked = output<{ speakerLabel: string; personId: string }>();
  readonly cancelled = output<void>();

  protected readonly suggestions = signal<Person[]>([]);
  protected selected: Person | null = null;

  // Cache the people list for the lifetime of the component — typing in the autocomplete
  // would otherwise hit /api/people on every keystroke.
  private people$: Observable<Person[]> | null = null;

  protected search(event: AutoCompleteCompleteEvent): void {
    const query = event.query.toLowerCase();
    this.people$ ??= this.peopleService.list().pipe(shareReplay({ bufferSize: 1, refCount: false }));
    this.people$.subscribe((people) => {
      const filtered = people.filter((p) => p.name.toLowerCase().includes(query));
      this.suggestions.set(filtered);
    });
  }

  protected emitLink(): void {
    if (!this.selected) return;
    this.linked.emit({ speakerLabel: this.speakerLabel(), personId: this.selected.id });
  }
}
