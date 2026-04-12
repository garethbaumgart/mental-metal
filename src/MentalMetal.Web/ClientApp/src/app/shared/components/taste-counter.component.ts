import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { AiNudgeService } from '../services/ai-nudge.service';

@Component({
  selector: 'app-taste-counter',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (visible()) {
      <div class="text-xs text-muted-color flex items-center gap-1">
        <i class="pi pi-sparkles"></i>
        <span>{{ remaining() }} of {{ dailyLimit() }} free AI operations remaining today</span>
      </div>
    }
  `,
})
export class TasteCounterComponent {
  private readonly nudgeService = inject(AiNudgeService);

  readonly visible = computed(() => this.nudgeService.isTasteUser());
  readonly remaining = computed(() => this.nudgeService.tasteRemaining());
  readonly dailyLimit = computed(() => this.nudgeService.tasteDailyLimit());
}
