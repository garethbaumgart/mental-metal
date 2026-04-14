import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { MessageService } from 'primeng/api';
import { DailyCloseOutPageComponent } from './daily-close-out-page.component';
import { CloseOutQueueResponse } from './daily-close-out.models';

describe('DailyCloseOutPageComponent', () => {
  let fixture: ComponentFixture<DailyCloseOutPageComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DailyCloseOutPageComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), MessageService],
    }).compileComponents();

    fixture = TestBed.createComponent(DailyCloseOutPageComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function flushInitial(body: CloseOutQueueResponse): void {
    fixture.detectChanges();
    // Reassign dialog loads people and initiatives on construction — satisfy those first.
    httpMock.expectOne('/api/people').flush([]);
    httpMock.expectOne('/api/initiatives').flush([]);
    // The page refreshes the queue on init.
    httpMock.expectOne('/api/daily-close-out/queue').flush(body);
    fixture.detectChanges();
  }

  it('renders empty state when queue is empty', () => {
    flushInitial({
      items: [],
      counts: { total: 0, raw: 0, processing: 0, processed: 0, failed: 0 },
    });

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Inbox zero');
  });

  it('renders queue items and progress counts', () => {
    flushInitial({
      items: [
        {
          id: 'c1',
          rawContent: 'first capture',
          captureType: 'QuickNote',
          processingStatus: 'Raw',
          extractionStatus: 'None',
          extractionResolved: false,
          aiExtraction: null,
          failureReason: null,
          linkedPersonIds: [],
          linkedInitiativeIds: [],
          title: null,
          capturedAt: '2026-04-14T00:00:00Z',
          processedAt: null,
        },
      ],
      counts: { total: 1, raw: 1, processing: 0, processed: 0, failed: 0 },
    });

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('first capture');
    expect(text).toContain('1 pending');
  });
});
