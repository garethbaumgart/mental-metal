import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { WeeklyBriefingPage } from './weekly-briefing.page';
import { Briefing } from '../../shared/models/briefing.model';

describe('WeeklyBriefingPage', () => {
  let fixture: ComponentFixture<WeeklyBriefingPage>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WeeklyBriefingPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(WeeklyBriefingPage);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function fakeBriefing(): Briefing {
    return {
      id: 'b1',
      type: 'Weekly',
      scopeKey: 'weekly:2026-W16',
      generatedAtUtc: '2026-04-14T08:00:00Z',
      markdownBody: '# Focus this week\n\n- Ship spec',
      model: 'test-model',
      inputTokens: 200,
      outputTokens: 100,
      factsSummary: {},
    };
  }

  it('loads the weekly briefing on init', () => {
    fixture.detectChanges();
    const req = httpMock.expectOne((r) => r.url === '/api/briefings/weekly' && r.method === 'POST');
    req.flush(fakeBriefing());
    fixture.detectChanges();

    const html = fixture.nativeElement.innerHTML as string;
    expect(html).toContain('Focus this week');
  });

  it('regenerate forces a new request', () => {
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/briefings/weekly' && r.method === 'POST').flush(fakeBriefing());
    fixture.detectChanges();

    fixture.componentInstance.regenerate();
    fixture.detectChanges();

    const req2 = httpMock.expectOne((r) => r.url === '/api/briefings/weekly' && r.method === 'POST');
    expect(req2.request.params.get('force')).toBe('true');
    req2.flush(fakeBriefing());
  });

  it('renders provider-not-configured state on 409', () => {
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/briefings/weekly' && r.method === 'POST').flush(
      { error: 'AI provider not configured.', code: 'ai_provider_not_configured' },
      { status: 409, statusText: 'Conflict' },
    );
    fixture.detectChanges();

    const html = fixture.nativeElement.innerHTML as string;
    expect(html).toContain('Configure your AI provider');
  });

  it('renders server message and settings link on 502 provider error', () => {
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/briefings/weekly' && r.method === 'POST').flush(
      { error: 'AI provider request failed. Please try again or check your provider configuration.' },
      { status: 502, statusText: 'Bad Gateway' },
    );
    fixture.detectChanges();

    const html = fixture.nativeElement.innerHTML as string;
    expect(html).toContain('AI provider request failed');
    expect(html).toContain('Check AI provider settings');
    expect(html).toContain('/settings');
  });

  it('renders server message and settings link on 429 rate limit', () => {
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/briefings/weekly' && r.method === 'POST').flush(
      { error: 'Daily AI request budget reached.' },
      { status: 429, statusText: 'Too Many Requests' },
    );
    fixture.detectChanges();

    const html = fixture.nativeElement.innerHTML as string;
    expect(html).toContain('Daily AI request budget reached');
    expect(html).toContain('Check AI provider settings');
  });

  it('renders generic fallback on 500', () => {
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/briefings/weekly' && r.method === 'POST').flush(
      'boom',
      { status: 500, statusText: 'Server Error' },
    );
    fixture.detectChanges();

    const html = fixture.nativeElement.innerHTML as string;
    expect(html).toContain('Failed to generate briefing');
    expect(html).not.toContain('Check AI provider settings');
  });
});
