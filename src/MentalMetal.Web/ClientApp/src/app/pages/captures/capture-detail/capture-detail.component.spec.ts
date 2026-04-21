import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
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
    quickCreateAndResolve: vi.fn(),
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

  it('unresolvedPeople returns empty array when no extraction', () => {
    (component as any).capture.set(
      buildCapture({ processingStatus: 'Raw', aiExtraction: null }),
    );

    expect((component as any).unresolvedPeople()).toEqual([]);
  });

  it('unresolvedPeople returns unresolved mentions only', () => {
    (component as any).capture.set(
      buildCapture({
        processingStatus: 'Processed',
        aiExtraction: buildExtraction({
          peopleMentioned: [
            { rawName: 'Sarah', personId: null, context: 'discussed project' },
            { rawName: 'Alice', personId: 'person-1', context: null },
            { rawName: 'Mike', personId: null, context: null },
          ],
        }),
      }),
    );

    const unresolved = (component as any).unresolvedPeople();
    expect(unresolved).toHaveLength(2);
    expect(unresolved[0].rawName).toBe('Sarah');
    expect(unresolved[1].rawName).toBe('Mike');
  });

  it('unresolvedPeople returns empty when all resolved', () => {
    (component as any).capture.set(
      buildCapture({
        processingStatus: 'Processed',
        aiExtraction: buildExtraction({
          peopleMentioned: [
            { rawName: 'Alice', personId: 'person-1', context: null },
          ],
        }),
      }),
    );

    expect((component as any).unresolvedPeople()).toHaveLength(0);
  });

  it('hasUnresolvedMentions is true when there are unresolved people', () => {
    (component as any).capture.set(
      buildCapture({
        processingStatus: 'Processed',
        aiExtraction: buildExtraction({
          peopleMentioned: [
            { rawName: 'Sarah', personId: null, context: null },
          ],
        }),
      }),
    );

    expect((component as any).hasUnresolvedMentions()).toBe(true);
  });

  it('hasUnresolvedMentions is false when all resolved', () => {
    (component as any).capture.set(
      buildCapture({
        processingStatus: 'Processed',
        aiExtraction: buildExtraction({
          peopleMentioned: [
            { rawName: 'Alice', personId: 'person-1', context: null },
          ],
        }),
      }),
    );

    expect((component as any).hasUnresolvedMentions()).toBe(false);
  });

  it('openQuickCreate pre-fills name and sets default type', () => {
    (component as any).capture.set(buildCapture());
    (component as any).openQuickCreate('Sarah');

    expect((component as any).quickCreateRawName()).toBe('Sarah');
    expect((component as any).quickCreateName()).toBe('Sarah');
    expect((component as any).quickCreateType()).toBe('Stakeholder');
    expect((component as any).quickCreateVisible()).toBe(true);
  });

  it('submitQuickCreate calls service and updates capture on success', () => {
    const updatedCapture = buildCapture({
      aiExtraction: buildExtraction({
        peopleMentioned: [{ rawName: 'Sarah', personId: 'new-person-id', context: null }],
      }),
    });
    mockCapturesService.quickCreateAndResolve.mockReturnValue(of(updatedCapture));

    (component as any).capture.set(
      buildCapture({
        aiExtraction: buildExtraction({
          peopleMentioned: [{ rawName: 'Sarah', personId: null, context: null }],
        }),
      }),
    );
    (component as any).quickCreateRawName.set('Sarah');
    (component as any).quickCreateName.set('Sarah Chen');
    (component as any).quickCreateType.set('Stakeholder');

    (component as any).submitQuickCreate();

    expect(mockCapturesService.quickCreateAndResolve).toHaveBeenCalledWith(
      'cap-1', 'Sarah', 'Sarah Chen', 'Stakeholder',
    );
  });

  it('submitQuickCreate handles 409 conflict with warning message', () => {
    mockCapturesService.quickCreateAndResolve.mockReturnValue(
      throwError(() => ({ status: 409, error: { error: 'Duplicate name' } })),
    );

    (component as any).capture.set(
      buildCapture({
        aiExtraction: buildExtraction({
          peopleMentioned: [{ rawName: 'Sarah', personId: null, context: null }],
        }),
      }),
    );
    // Open the dialog first so quickCreateVisible is true
    (component as any).openQuickCreate('Sarah');
    (component as any).quickCreateName.set('Sarah Chen');

    (component as any).submitQuickCreate();

    expect((component as any).quickCreateSubmitting()).toBe(false);
    // Dialog should remain open on conflict
    expect((component as any).quickCreateVisible()).toBe(true);
  });
});
