import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { MessageService } from 'primeng/api';
import { AiProviderSettingsComponent } from './ai-provider-settings.component';

describe('AiProviderSettingsComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AiProviderSettingsComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        MessageService,
      ],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create the component', () => {
    const fixture = TestBed.createComponent(AiProviderSettingsComponent);
    fixture.detectChanges();

    // Flush the status load triggered by ngOnInit
    httpMock.expectOne('/api/users/me/ai-provider').flush({
      isConfigured: false,
      provider: null,
      model: null,
      maxTokens: null,
      tasteBudget: { remaining: 5, dailyLimit: 5, isEnabled: true },
    });

    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should load models when provider is selected', () => {
    const fixture = TestBed.createComponent(AiProviderSettingsComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/users/me/ai-provider').flush({
      isConfigured: false,
      provider: null,
      model: null,
      maxTokens: null,
      tasteBudget: { remaining: 5, dailyLimit: 5, isEnabled: true },
    });

    fixture.componentInstance.selectProvider('Anthropic');

    const modelReq = httpMock.expectOne('/api/ai/models?provider=Anthropic');
    modelReq.flush({
      provider: 'Anthropic',
      models: [
        { id: 'claude-sonnet-4-20250514', name: 'Claude Sonnet 4', isDefault: true },
      ],
    });

    expect(fixture.componentInstance.models().length).toBe(1);
  });

  it('should disable save when API key entered but not validated', () => {
    const fixture = TestBed.createComponent(AiProviderSettingsComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/users/me/ai-provider').flush({
      isConfigured: false,
      provider: null,
      model: null,
      maxTokens: null,
      tasteBudget: { remaining: 5, dailyLimit: 5, isEnabled: true },
    });

    fixture.componentInstance.selectProvider('Anthropic');

    const modelReq = httpMock.expectOne('/api/ai/models?provider=Anthropic');
    modelReq.flush({
      provider: 'Anthropic',
      models: [
        { id: 'claude-sonnet-4-20250514', name: 'Claude Sonnet 4', isDefault: true },
      ],
    });

    // Enter an API key but no validation has run yet
    fixture.componentInstance['apiKey'] = 'sk-ant-test-key-1234567890';

    // canSave should be false — validation hasn't passed
    expect(fixture.componentInstance.canSave()).toBe(false);
  });

  it('should reset validation state on API key input', () => {
    const fixture = TestBed.createComponent(AiProviderSettingsComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/users/me/ai-provider').flush({
      isConfigured: false,
      provider: null,
      model: null,
      maxTokens: null,
      tasteBudget: { remaining: 5, dailyLimit: 5, isEnabled: true },
    });

    fixture.componentInstance.selectProvider('OpenAI');
    httpMock.expectOne('/api/ai/models?provider=OpenAI').flush({
      provider: 'OpenAI',
      models: [{ id: 'gpt-4o', name: 'GPT-4o', isDefault: true }],
    });

    // Simulate a previous successful validation
    fixture.componentInstance.validationResult.set('success');
    fixture.componentInstance.validationMessage.set('Connected to GPT-4o');

    // Typing a new key should clear the stale validation
    fixture.componentInstance.onApiKeyInput();
    expect(fixture.componentInstance.validationResult()).toBeNull();
    expect(fixture.componentInstance.validationMessage()).toBeNull();
  });

  it('should reset apiKey and validation on provider switch', () => {
    const fixture = TestBed.createComponent(AiProviderSettingsComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/users/me/ai-provider').flush({
      isConfigured: false,
      provider: null,
      model: null,
      maxTokens: null,
      tasteBudget: { remaining: 5, dailyLimit: 5, isEnabled: true },
    });

    fixture.componentInstance.selectProvider('OpenAI');
    httpMock.expectOne('/api/ai/models?provider=OpenAI').flush({
      provider: 'OpenAI',
      models: [{ id: 'gpt-4o', name: 'GPT-4o', isDefault: true }],
    });

    // Enter API key and simulate validation
    fixture.componentInstance['apiKey'] = 'sk-test-key-1234567890abcdef';
    fixture.componentInstance.validationResult.set('success');

    // Switch provider
    fixture.componentInstance.selectProvider('Anthropic');
    httpMock.expectOne('/api/ai/models?provider=Anthropic').flush({
      provider: 'Anthropic',
      models: [{ id: 'claude-sonnet-4-20250514', name: 'Claude Sonnet 4', isDefault: true }],
    });

    expect(fixture.componentInstance['apiKey']).toBe('');
    expect(fixture.componentInstance.validationResult()).toBeNull();
    expect(fixture.componentInstance.validating()).toBe(false);
  });

  it('should pre-populate form when provider is configured', () => {
    const fixture = TestBed.createComponent(AiProviderSettingsComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/users/me/ai-provider').flush({
      isConfigured: true,
      provider: 'OpenAI',
      model: 'gpt-4o',
      maxTokens: null,
      tasteBudget: { remaining: 5, dailyLimit: 5, isEnabled: true },
    });

    // The component reads status synchronously after flush — but loadStatus is async.
    // The status signal updates after the subscribe callback, so we need to trigger
    // change detection again and handle the models request from selectProvider.
    fixture.detectChanges();

    // If the component auto-selects based on status, it will request models
    const modelReqs = httpMock.match('/api/ai/models?provider=OpenAI');
    modelReqs.forEach(req => req.flush({
      provider: 'OpenAI',
      models: [{ id: 'gpt-4o', name: 'GPT-4o', isDefault: true }],
    }));

    expect(fixture.componentInstance.selectedProvider()).toBe('OpenAI');
    expect(fixture.componentInstance.isConfigured()).toBe(true);
  });
});
