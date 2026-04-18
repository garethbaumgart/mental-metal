import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, beforeAll, afterEach } from 'vitest';
import { QuickCaptureDialogComponent } from './quick-capture-dialog.component';

describe('QuickCaptureDialogComponent', () => {
  let fixture: ComponentFixture<QuickCaptureDialogComponent>;
  let httpMock: HttpTestingController;

  beforeAll(() => {
    if (!window.matchMedia) {
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        configurable: true,
        value: (query: string) => ({
          matches: false,
          media: query,
          addEventListener: () => undefined,
          removeEventListener: () => undefined,
          addListener: () => undefined,
          removeListener: () => undefined,
          dispatchEvent: () => false,
          onchange: null,
        }),
      });
    }
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [QuickCaptureDialogComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(QuickCaptureDialogComponent);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('visible', true);
    fixture.detectChanges();

    // The component checks Deepgram availability when opened — flush that request
    flushDeepgramStatusCheck();
  });

  afterEach(() => httpMock.verify());

  /** Flush the Deepgram status check that fires when the dialog opens. */
  function flushDeepgramStatusCheck(): void {
    const req = httpMock.match('/api/transcription/status');
    for (const r of req) {
      r.flush({ available: false, reason: 'Not configured' });
    }
  }

  function textarea(): HTMLTextAreaElement {
    // PrimeNG dialog portal-renders outside the component; search the whole body.
    const el = document.querySelector('textarea#captureContent') as HTMLTextAreaElement | null;
    if (!el) throw new Error('content textarea not found');
    return el;
  }

  function submitButton(): HTMLButtonElement {
    const btns = Array.from(document.querySelectorAll('button')) as HTMLButtonElement[];
    const btn = btns.find((b) => (b.textContent ?? '').trim().toLowerCase().includes('capture'));
    if (!btn) throw new Error('Capture button not found');
    return btn;
  }

  it('submit is disabled when content is empty', () => {
    expect(submitButton().disabled).toBe(true);
  });

  it('defaults to QuickNote and sends it on submit', () => {
    const ta = textarea();
    ta.value = 'Thought from nowhere';
    ta.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    submitButton().click();

    const req = httpMock.expectOne('/api/captures');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toMatchObject({
      rawContent: 'Thought from nowhere',
      type: 'QuickNote',
    });
    expect(req.request.body.title).toBeUndefined();
    expect(req.request.body.source).toBe('Typed');
    req.flush({ id: 'c1' });
  });

  it('Enter in textarea submits; Shift+Enter does not', () => {
    const ta = textarea();
    ta.value = 'hello';
    ta.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    // Shift+Enter: no submit
    ta.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', shiftKey: true, bubbles: true, cancelable: true }));
    fixture.detectChanges();
    httpMock.expectNone('/api/captures');

    // Enter alone: submit
    ta.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
    fixture.detectChanges();
    httpMock.expectOne('/api/captures').flush({ id: 'c1' });
  });
});
