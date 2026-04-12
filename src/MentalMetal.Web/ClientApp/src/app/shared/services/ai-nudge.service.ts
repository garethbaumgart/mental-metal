import { computed, inject, Injectable } from '@angular/core';
import { AiProviderService } from './ai-provider.service';

export type NudgeState = 'Fresh' | 'Tasting' | 'Limited' | 'Unlimited';

const DISMISS_PREFIX = 'ai-nudge-dismiss-';

@Injectable({ providedIn: 'root' })
export class AiNudgeService {
  private readonly aiProviderService = inject(AiProviderService);

  readonly nudgeState = computed<NudgeState>(() => {
    const status = this.aiProviderService.status();
    if (!status) return 'Fresh';
    if (status.isConfigured) return 'Unlimited';
    if (!status.tasteBudget.isEnabled) return 'Fresh';
    if (status.tasteBudget.remaining <= 0) return 'Limited';
    if (status.tasteBudget.remaining < status.tasteBudget.dailyLimit) return 'Tasting';
    return 'Fresh';
  });

  readonly isTasteUser = computed(() => {
    const state = this.nudgeState();
    return state === 'Fresh' || state === 'Tasting' || state === 'Limited';
  });

  readonly tasteRemaining = computed(() => {
    return this.aiProviderService.status()?.tasteBudget.remaining ?? 0;
  });

  readonly tasteDailyLimit = computed(() => {
    return this.aiProviderService.status()?.tasteBudget.dailyLimit ?? 5;
  });

  isDismissed(nudgeType: string): boolean {
    try {
      const raw = localStorage.getItem(DISMISS_PREFIX + nudgeType);
      if (!raw) return false;
      const expiry = parseInt(raw, 10);
      if (Date.now() > expiry) {
        localStorage.removeItem(DISMISS_PREFIX + nudgeType);
        return false;
      }
      return true;
    } catch {
      return false;
    }
  }

  dismiss(nudgeType: string, days = 7): void {
    try {
      const expiry = Date.now() + days * 24 * 60 * 60 * 1000;
      localStorage.setItem(DISMISS_PREFIX + nudgeType, expiry.toString());
    } catch {
      // localStorage may be unavailable
    }
  }
}
