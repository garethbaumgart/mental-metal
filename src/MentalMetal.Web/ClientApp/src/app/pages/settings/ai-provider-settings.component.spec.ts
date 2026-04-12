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
