import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { CaptureDetailComponent } from './capture-detail.component';
import { CapturesService } from '../../../shared/services/captures.service';
import { PeopleService } from '../../../shared/services/people.service';
import { InitiativesService } from '../../../shared/services/initiatives.service';
import { Capture, AiExtraction } from '../../../shared/models/capture.model';

function buildCapture(overrides: Partial<Capture> = {}): Capture {
  return {
    id: 'cap-1',
    userId: 'user-1',
    rawContent: 'Test content',
    captureType: 'QuickNote',
    captureSource: null,
    processingStatus: 'Processed',
    aiExtraction: null,
    failureReason: null,
    linkedPersonIds: [],
    linkedInitiativeIds: [],
    spawnedCommitmentIds: [],
    title: 'Test',
    capturedAt: '2026-04-21T00:00:00Z',
    processedAt: '2026-04-21T00:01:00Z',
    updatedAt: '2026-04-21T00:01:00Z',
    ...overrides,
  };
}

function buildExtraction(overrides: Partial<AiExtraction> = {}): AiExtraction {
  return {
    summary: 'Test summary',
    peopleMentioned: [],
    commitments: [],
    decisions: [],
    risks: [],
    initiativeTags: [],
    extractedAt: '2026-04-21T00:01:00Z',
    detectedCaptureType: null,
    ...overrides,
  };
}

describe('CaptureDetailComponent', () => {
  let fixture: ComponentFixture<CaptureDetailComponent>;
  let component: CaptureDetailComponent;

  const mockCapturesService = {
    get: vi.fn().mockReturnValue(of(buildCapture())),
    retry: vi.fn(),
    updateMetadata: vi.fn(),
    resolvePersonMention: vi.fn(),
    resolveInitiativeTag: vi.fn(),
    getTranscript: vi.fn().mockReturnValue(of(null)),
    updateSpeakers: vi.fn(),
  };

  const mockPeopleService = { list: vi.fn().mockReturnValue(of([])) };
  const mockInitiativesService = { list: vi.fn().mockReturnValue(of([])) };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CaptureDetailComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: { get: () => 'cap-1' },
              queryParamMap: { get: () => null },
            },
          },
        },
        { provide: CapturesService, useValue: mockCapturesService },
        { provide: PeopleService, useValue: mockPeopleService },
        { provide: InitiativesService, useValue: mockInitiativesService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CaptureDetailComponent);
    component = fixture.componentInstance;
  });

  it('detectedCaptureType returns null when extraction has no detected type', () => {
    (component as any).capture.set(
      buildCapture({
        processingStatus: 'Processed',
        aiExtraction: buildExtraction({ detectedCaptureType: null }),
      }),
    );

    expect((component as any).detectedCaptureType()).toBeNull();
  });

  it('detectedCaptureType returns null when no extraction exists', () => {
    (component as any).capture.set(
      buildCapture({ processingStatus: 'Raw', aiExtraction: null }),
    );

    expect((component as any).detectedCaptureType()).toBeNull();
  });

  it('detectedCaptureType returns the type when extraction has detectedCaptureType', () => {
    (component as any).capture.set(
      buildCapture({
        processingStatus: 'Processed',
        captureType: 'Transcript',
        aiExtraction: buildExtraction({ detectedCaptureType: 'Transcript' }),
      }),
    );

    expect((component as any).detectedCaptureType()).toBe('Transcript');
  });

  it('detectedCaptureType returns MeetingNotes when extraction detected MeetingNotes', () => {
    (component as any).capture.set(
      buildCapture({
        processingStatus: 'Processed',
        captureType: 'MeetingNotes',
        aiExtraction: buildExtraction({ detectedCaptureType: 'MeetingNotes' }),
      }),
    );

    expect((component as any).detectedCaptureType()).toBe('MeetingNotes');
  });
});
