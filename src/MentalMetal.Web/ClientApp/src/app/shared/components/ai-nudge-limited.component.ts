import { ChangeDetectionStrategy, Component, computed, inject, output } from '@angular/core';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AiNudgeService } from '../services/ai-nudge.service';

@Component({
  selector: 'app-ai-nudge-limited',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule],
  template: `
    @if (visible()) {
      <div class="flex flex-col items-center gap-4 p-6 rounded-lg bg-surface-50 border border-surface-200 text-center">
        <i class="pi pi-exclamation-circle text-3xl text-muted-color"></i>
        <p class="text-sm font-medium">
          You've used your {{ dailyLimit() }} free AI operations today
        </p>
        <div class="flex gap-2">
          <p-button label="Set Up AI Provider" (onClick)="goToSettings()" />
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
  private readonly router = inject(Router);
  readonly dismissed = output<void>();

  readonly visible = computed(() => this.nudgeService.nudgeState() === 'Limited');
  readonly dailyLimit = computed(() => this.nudgeService.tasteDailyLimit());

  goToSettings(): void {
    this.router.navigate(['/settings']);
  }

  continueTomorrow(): void {
    this.dismissed.emit();
  }
}
