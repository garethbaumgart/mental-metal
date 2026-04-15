import { describe, it, expect } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { QuickCaptureUiService } from './quick-capture-ui.service';
import { Capture } from '../models/capture.model';

describe('QuickCaptureUiService', () => {
  function fakeCapture(): Capture {
    return {
      id: 'c1',
      userId: 'u1',
      rawContent: 'hi',
      captureType: 'QuickNote',
      processingStatus: 'Raw',
      capturedAt: '2026-04-16T00:00:00Z',
      title: null,
      source: null,
      linkedPersonIds: [],
      linkedInitiativeIds: [],
    } as unknown as Capture;
  }

  it('starts closed', () => {
    const svc = TestBed.configureTestingModule({}).inject(QuickCaptureUiService);
    expect(svc.visible()).toBe(false);
  });

  it('open() sets visible and is idempotent', () => {
    const svc = TestBed.configureTestingModule({}).inject(QuickCaptureUiService);
    svc.open();
    expect(svc.visible()).toBe(true);
    svc.open(); // should not throw or toggle
    expect(svc.visible()).toBe(true);
  });

  it('close() resets visible', () => {
    const svc = TestBed.configureTestingModule({}).inject(QuickCaptureUiService);
    svc.open();
    svc.close();
    expect(svc.visible()).toBe(false);
  });

  it('captureCreated$ notifies subscribers', () => {
    const svc = TestBed.configureTestingModule({}).inject(QuickCaptureUiService);
    const received: Capture[] = [];
    svc.captureCreated$.subscribe((c) => received.push(c));
    const c = fakeCapture();
    svc.notifyCreated(c);
    expect(received).toEqual([c]);
  });
});
