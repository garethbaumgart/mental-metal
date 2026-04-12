import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AiNudgeService } from '../services/ai-nudge.service';

@Component({
  selector: 'app-ai-nudge-fresh',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    @if (visible()) {
      <div class="flex items-center gap-2 text-sm text-muted-color">
        <i class="pi pi-info-circle"></i>
        <span>AI Provider: Not configured</span>
        <a routerLink="/settings" class="text-primary hover:underline">Set up</a>
      </div>
    }
  `,
})
export class AiNudgeFreshComponent {
  private readonly nudgeService = inject(AiNudgeService);

  readonly visible = computed(() => this.nudgeService.nudgeState() === 'Fresh');
}
