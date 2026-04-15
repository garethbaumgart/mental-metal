import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { MorningBriefingWidgetComponent } from './morning-briefing-widget.component';
import { Briefing } from '../../shared/models/briefing.model';

describe('MorningBriefingWidgetComponent', () => {
  let fixture: ComponentFixture<MorningBriefingWidgetComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MorningBriefingWidgetComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MorningBriefingWidgetComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function fakeBriefing(overrides: Partial<Briefing> = {}): Briefing {
    return {
      id: 'b1',
      type: 'Morning',
      scopeKey: 'morning:2026-04-14',
      generatedAtUtc: '2026-04-14T08:00:00Z',
      markdownBody: '# Today\n\nFocus on shipping.',
      model: 'test-model',
      inputTokens: 100,
      outputTokens: 50,
      factsSummary: {},
      ...overrides,
    };
  }

  function flushFirst(body: Briefing): void {
    fixture.detectChanges();
    const req = httpMock.expectOne((r) => r.url === '/api/briefings/morning' && r.method === 'POST');
    expect(req.request.params.has('force')).toBe(false);
    req.flush(body);
    fixture.detectChanges();
  }

  it('renders the briefing markdown after first load', () => {
    flushFirst(fakeBriefing());

    const html = fixture.nativeElement.innerHTML as string;
    expect(html).toContain('Today');
    expect(html).toContain('Focus on shipping');
  });

  it('regenerate button calls the endpoint with force=true', () => {
    flushFirst(fakeBriefing());

    const component = fixture.componentInstance;
    component.regenerate();
    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url === '/api/briefings/morning' && r.method === 'POST');
    expect(req.request.params.get('force')).toBe('true');
    req.flush(fakeBriefing({ id: 'b2', markdownBody: '# Updated\n\nNew focus.' }));
    fixture.detectChanges();

    const html = fixture.nativeElement.innerHTML as string;
    expect(html).toContain('Updated');
  });

  it('renders the provider-not-configured empty state on 409', () => {
    fixture.detectChanges();
    const req = httpMock.expectOne((r) => r.url === '/api/briefings/morning' && r.method === 'POST');
    req.flush(
      { error: 'AI provider is not configured.', code: 'ai_provider_not_configured' },
      { status: 409, statusText: 'Conflict' },
    );
    fixture.detectChanges();

    const html = fixture.nativeElement.innerHTML as string;
    expect(html).toContain('Configure your AI provider');
    expect(html).toContain('/settings');
  });

  it('renders a generic error state on other failures', () => {
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/briefings/morning' && r.method === 'POST')
      .flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    const html = fixture.nativeElement.innerHTML as string;
    expect(html).toContain('Failed to generate briefing');
    // No settings link on generic 5xx
    expect(html).not.toContain('Check AI provider settings');
  });

  it('renders server-provided message and settings link on 502 provider error', () => {
    // Matches the sanitized message returned by the AiErrorMiddleware in
    // Program.cs — briefing 502s never expose the raw provider exception.
    const sanitized = 'AI provider request failed. Please try again or check your provider configuration.';
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/briefings/morning' && r.method === 'POST')
      .flush({ error: sanitized }, { status: 502, statusText: 'Bad Gateway' });
    fixture.detectChanges();

    const html = fixture.nativeElement.innerHTML as string;
    expect(html).toContain('AI provider request failed');
    expect(html).toContain('Check AI provider settings');
    expect(html).toContain('/settings');
  });
});
