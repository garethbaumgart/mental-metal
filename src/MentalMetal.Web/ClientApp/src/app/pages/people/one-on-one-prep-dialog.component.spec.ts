import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { OneOnOnePrepDialogComponent } from './one-on-one-prep-dialog.component';
import { Briefing } from '../../shared/models/briefing.model';

@Component({
  selector: 'app-host',
  standalone: true,
  imports: [OneOnOnePrepDialogComponent],
  template: `
    <app-one-on-one-prep-dialog
      [visible]="visible()"
      [personId]="personId"
      (visibleChange)="visible.set($event)"
    />
  `,
})
class HostComponent {
  readonly visible = signal(false);
  readonly personId = 'person-1';
}

describe('OneOnOnePrepDialogComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function briefing(): Briefing {
    return {
      id: 'b1',
      type: 'OneOnOnePrep',
      scopeKey: 'oneonone:abc',
      generatedAtUtc: '2026-04-14T08:00:00Z',
      markdownBody: '# Context\n\nSarah is doing well.',
      model: 'test-model',
      inputTokens: 50,
      outputTokens: 30,
      factsSummary: {},
    };
  }

  it('does not call the endpoint until visible', () => {
    fixture.detectChanges();
    httpMock.expectNone((r) => r.url.includes('/api/briefings/one-on-one'));
  });

  it('loads the prep sheet when becoming visible', async () => {
    fixture.componentInstance.visible.set(true);
    fixture.detectChanges();
    await fixture.whenStable();

    const req = httpMock.expectOne(
      (r) => r.url === '/api/briefings/one-on-one/person-1' && r.method === 'POST',
    );
    expect(req.request.params.has('force')).toBe(false);
    req.flush(briefing());
    fixture.detectChanges();

    const html = fixture.nativeElement.innerHTML as string;
    expect(html).toContain('Context');
  });

  it('renders 409 empty state', async () => {
    fixture.componentInstance.visible.set(true);
    fixture.detectChanges();
    // Drain the effect microtask + initial detectChanges, then the request fires.
    await fixture.whenStable();
    fixture.detectChanges();

    httpMock.expectOne((r) => r.url === '/api/briefings/one-on-one/person-1').flush(
      { error: 'AI provider not configured.', code: 'ai_provider_not_configured' },
      { status: 409, statusText: 'Conflict' },
    );
    await fixture.whenStable();
    fixture.detectChanges();

    const html = fixture.nativeElement.innerHTML as string;
    expect(html).toContain('Configure your AI provider');
  });
});
