import { ComponentRef } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TriageCardComponent } from './triage-card.component';
import { CloseOutQueueItem } from './daily-close-out.models';

function makeItem(partial: Partial<CloseOutQueueItem> = {}): CloseOutQueueItem {
  return {
    id: 'c1',
    rawContent: 'some content',
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
    ...partial,
  };
}

describe('TriageCardComponent', () => {
  let fixture: ComponentFixture<TriageCardComponent>;
  let component: TriageCardComponent;
  let componentRef: ComponentRef<TriageCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TriageCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TriageCardComponent);
    component = fixture.componentInstance;
    componentRef = fixture.componentRef;
  });

  it('hides Confirm/Discard buttons when not Processed', () => {
    componentRef.setInput('capture', makeItem({ processingStatus: 'Raw' }));
    fixture.detectChanges();

    const html = fixture.nativeElement.textContent as string;
    expect(html).not.toContain('Confirm');
    expect(html).not.toContain('Discard extraction');
    expect(html).toContain('Quick discard');
  });

  it('shows Confirm/Discard when Processed and extraction unresolved', () => {
    componentRef.setInput(
      'capture',
      makeItem({ processingStatus: 'Processed', extractionResolved: false }),
    );
    fixture.detectChanges();

    const html = fixture.nativeElement.textContent as string;
    expect(html).toContain('Confirm');
    expect(html).toContain('Discard extraction');
  });

  it('emits the correct action on button click', () => {
    componentRef.setInput('capture', makeItem());
    fixture.detectChanges();

    let emitted: string | null = null;
    component.action.subscribe((action) => (emitted = action));

    const buttons = fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>;
    const quick = Array.from(buttons).find((b) => (b.textContent ?? '').includes('Quick discard'));
    quick!.click();

    expect(emitted).toBe('quick-discard');
  });
});
