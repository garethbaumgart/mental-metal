import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { AiNudgeService } from './ai-nudge.service';
import { AiProviderService } from './ai-provider.service';
import { AiProviderStatus } from '../models/ai-provider.model';

function makeStatus(overrides: Partial<AiProviderStatus> = {}): AiProviderStatus {
  return {
    isConfigured: false,
    provider: null,
    model: null,
    maxTokens: null,
    tasteBudget: { remaining: 5, dailyLimit: 5, isEnabled: true },
    ...overrides,
  };
}

describe('AiNudgeService', () => {
  let nudgeService: AiNudgeService;
  let providerService: AiProviderService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient()],
    });
    nudgeService = TestBed.inject(AiNudgeService);
    providerService = TestBed.inject(AiProviderService);
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should return Fresh when no status loaded', () => {
    expect(nudgeService.nudgeState()).toBe('Fresh');
  });

  it('should return Unlimited when provider is configured', () => {
    providerService.status.set(makeStatus({ isConfigured: true, provider: 'Anthropic' }));
    expect(nudgeService.nudgeState()).toBe('Unlimited');
  });

  it('should return Fresh when taste budget is full', () => {
    providerService.status.set(makeStatus());
    expect(nudgeService.nudgeState()).toBe('Fresh');
  });

  it('should return Tasting when taste budget is partially used', () => {
    providerService.status.set(makeStatus({
      tasteBudget: { remaining: 3, dailyLimit: 5, isEnabled: true },
    }));
    expect(nudgeService.nudgeState()).toBe('Tasting');
  });

  it('should return Limited when taste budget is exhausted', () => {
    providerService.status.set(makeStatus({
      tasteBudget: { remaining: 0, dailyLimit: 5, isEnabled: true },
    }));
    expect(nudgeService.nudgeState()).toBe('Limited');
  });

  it('should return Fresh when taste is not enabled', () => {
    providerService.status.set(makeStatus({
      tasteBudget: { remaining: 0, dailyLimit: 5, isEnabled: false },
    }));
    expect(nudgeService.nudgeState()).toBe('Fresh');
  });

  it('should return isTasteUser=false when taste is disabled', () => {
    providerService.status.set(makeStatus({
      tasteBudget: { remaining: 0, dailyLimit: 5, isEnabled: false },
    }));
    expect(nudgeService.isTasteUser()).toBe(false);
  });

  it('should return isTasteUser=true when taste is enabled and not configured', () => {
    providerService.status.set(makeStatus({
      tasteBudget: { remaining: 3, dailyLimit: 5, isEnabled: true },
    }));
    expect(nudgeService.isTasteUser()).toBe(true);
  });

  it('should dismiss and check dismissal with TTL', () => {
    expect(nudgeService.isDismissed('test')).toBe(false);
    nudgeService.dismiss('test', 7);
    expect(nudgeService.isDismissed('test')).toBe(true);
  });

  it('should report not dismissed when TTL expired', () => {
    // Set expired timestamp
    localStorage.setItem('ai-nudge-dismiss-test', (Date.now() - 1000).toString());
    expect(nudgeService.isDismissed('test')).toBe(false);
  });
});
