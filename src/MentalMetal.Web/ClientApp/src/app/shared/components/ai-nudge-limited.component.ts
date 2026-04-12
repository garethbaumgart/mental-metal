import { ChangeDetectionStrategy, Component, computed, inject, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AiNudgeService } from '../services/ai-nudge.service';

@Component({
  selector: 'app-ai-nudge-limited',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, RouterLink],
  template: `
    @if (visible()) {
      <div class="flex flex-col items-center gap-4 p-6 rounded-lg bg-surface-50 border border-surface-200 text-center">
        <i class="pi pi-exclamation-circle text-3xl text-muted-color"></i>
        <p class="text-sm font-medium">
          You've used your {{ dailyLimit() }} free AI operations today
        </p>
        <div class="flex gap-2">
          <a routerLink="/settings">
            <p-button label="Set Up AI Provider" />
          </a>
          <p-button
            label="Continue tomorrow"
            [outlined]="true"
            (onClick)="continueTomorrow()"
          />
        </div>
      </div>
    }
  `,
})
export class AiNudgeLimitedComponent {
  private readonly nudgeService = inject(AiNudgeService);
  readonly dismissed = output<void>();

  readonly visible = computed(() => this.nudgeService.nudgeState() === 'Limited');
  readonly dailyLimit = computed(() => this.nudgeService.tasteDailyLimit());

  continueTomorrow(): void {
    this.dismissed.emit();
  }
}
