import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { ChipModule } from 'primeng/chip';
import { SourceReference, SourceReferenceEntityType } from '../../../shared/models/chat-thread.model';

@Component({
  selector: 'app-source-reference-chip',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ChipModule],
  template: `
    <button
      type="button"
      class="source-chip"
      [title]="reference().snippetText || label()"
      (click)="navigate()"
    >
      <p-chip [label]="label()" [styleClass]="'text-xs'" />
    </button>
  `,
  styles: [`
    .source-chip {
      background: none;
      border: none;
      padding: 0;
      cursor: pointer;
    }
  `],
})
export class SourceReferenceChipComponent {
  readonly reference = input.required<SourceReference>();
  // Optional: when the chip is rendered inside an initiative-chat context, LivingBrief
  // chips need the initiative id to route correctly. In the global chat we route to the
  // initiative-detail Living Brief tab using the reference's own EntityId (which is the
  // initiative's id when the source was Initiative-typed) — but for nested LivingBrief*
  // refs we still need the explicit context. When omitted, LivingBrief* chips no-op.
  readonly initiativeId = input<string | null>(null);

  private readonly router = inject(Router);

  protected readonly label = computed(() => {
    const r = this.reference();
    const short = this.shortType(r.entityType);
    const snippet = r.snippetText?.trim();
    return snippet ? `[${short}] ${this.truncate(snippet, 40)}` : `[${short}]`;
  });

  protected navigate(): void {
    const r = this.reference();
    const initiativeId = this.initiativeId();
    switch (r.entityType) {
      case 'Capture':
        this.router.navigate(['/captures', r.entityId]);
        break;
      case 'Commitment':
        this.router.navigate(['/commitments', r.entityId]);
        break;
      case 'Delegation':
        this.router.navigate(['/delegations', r.entityId]);
        break;
      case 'Initiative':
        this.router.navigate(['/initiatives', r.entityId]);
        break;
      case 'Person':
        this.router.navigate(['/people', r.entityId]);
        break;
      case 'LivingBriefDecision':
      case 'LivingBriefRisk':
      case 'LivingBriefRequirements':
      case 'LivingBriefDesignDirection':
        // Route to the initiative's Living Brief tab with an anchor hint. In the global-
        // chat surface, initiativeId may be null — degrade to a no-op rather than breaking.
        if (initiativeId) {
          this.router.navigate(['/initiatives', initiativeId], {
            fragment: `${r.entityType}-${r.entityId}`,
          });
        }
        break;
      case 'Observation':
      case 'Goal':
      case 'OneOnOne':
        // Forward-compatible reservations for the people-lens capability. Today these chips
        // would never be emitted by the backend; future work routes them to person detail.
        break;
    }
  }

  private shortType(t: SourceReferenceEntityType): string {
    switch (t) {
      case 'LivingBriefDecision': return 'D';
      case 'LivingBriefRisk': return 'R';
      case 'LivingBriefRequirements': return 'Req';
      case 'LivingBriefDesignDirection': return 'DD';
      case 'Capture': return 'Cap';
      case 'Commitment': return 'C';
      case 'Delegation': return 'Del';
      case 'Initiative': return 'I';
      case 'Person': return 'P';
      case 'Observation': return 'O';
      case 'Goal': return 'G';
      case 'OneOnOne': return '1:1';
    }
  }

  private truncate(s: string, n: number): string {
    return s.length <= n ? s : `${s.slice(0, n)}…`;
  }
}
