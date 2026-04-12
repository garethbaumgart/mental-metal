import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AiNudgeService } from '../services/ai-nudge.service';

@Component({
  selector: 'app-ai-nudge-contextual',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, RouterLink],
  template: `
    @if (visible()) {
      <div class="flex items-center justify-between gap-3 p-3 rounded-lg bg-surface-50 border border-surface-200">
        <span class="text-sm text-muted-color">
          Add your own AI key for unlimited access
        </span>
        <div class="flex items-center gap-2">
          <a routerLink="/settings" class="text-sm text-primary hover:underline">Set up</a>
          <p-button
            icon="pi pi-times"
            [text]="true"
            [rounded]="true"
            size="small"
            (onClick)="dismiss()"
          />
        </div>
      </div>
    }
  `,
})
export class AiNudgeContextualComponent {
  private readonly nudgeService = inject(AiNudgeService);

  readonly visible = computed(
    () => this.nudgeService.nudgeState() === 'Tasting' && !this.nudgeService.isDismissed('contextual'),
  );

  dismiss(): void {
    this.nudgeService.dismiss('contextual', 7);
  }
}
