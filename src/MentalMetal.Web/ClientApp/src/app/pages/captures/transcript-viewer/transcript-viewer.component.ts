import { afterNextRender, ChangeDetectionStrategy, Component, computed, ElementRef, input, output, viewChild } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import {
  CaptureTranscript,
  TranscriptSegment,
} from '../../../shared/models/capture.model';

/**
 * Groups consecutive segments by speaker and renders each group with its
 * timecode. Emits an event when a user wants to link a speaker to a Person
 * (the parent wires in the SpeakerPicker).
 */
export interface SpeakerGroup {
  speakerLabel: string;
  linkedPersonId: string | null;
  startSeconds: number;
  endSeconds: number;
  segments: TranscriptSegment[];
}

interface SegmentHighlight {
  before: string;
  highlighted: string;
  after: string;
}

@Component({
  selector: 'app-transcript-viewer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule],
  styles: `
    .source-highlight {
      background: color-mix(in srgb, var(--p-yellow-100) 70%, transparent);
      border-bottom: 2px solid var(--p-yellow-500);
      padding: 1px 2px;
      border-radius: 2px;
    }
  `,
  template: `
    @if (groups().length === 0) {
      <p class="text-sm text-muted-color">No transcript available.</p>
    } @else {
      <div class="flex flex-col gap-4">
        @for (group of groups(); track $index) {
          <div class="rounded border border-surface-200 bg-surface-0 p-3">
            <div class="mb-1 flex items-center justify-between">
              <div class="flex items-center gap-2 text-sm font-medium">
                <span class="text-primary">{{ group.speakerLabel }}</span>
                @if (group.linkedPersonId) {
                  <span class="text-xs text-muted-color">(linked)</span>
                }
                <span class="text-xs text-muted-color">
                  {{ formatTime(group.startSeconds) }} – {{ formatTime(group.endSeconds) }}
                </span>
              </div>
              <p-button
                label="Link speaker"
                size="small"
                severity="secondary"
                [text]="true"
                (onClick)="linkRequested.emit(group.speakerLabel)"
              />
            </div>
            <div class="flex flex-col gap-1">
              @for (seg of group.segments; track $index) {
                @if (segmentHighlight(seg); as hl) {
                  <p class="text-sm leading-relaxed">{{ hl.before }}<mark #highlightMark class="source-highlight">{{ hl.highlighted }}</mark>{{ hl.after }}</p>
                } @else {
                  <p class="text-sm leading-relaxed">{{ seg.text }}</p>
                }
              }
            </div>
          </div>
        }
      </div>
    }
  `,
})
export class TranscriptViewerComponent {
  readonly transcript = input.required<CaptureTranscript | null>();
  readonly highlightStart = input<number | null>(null);
  readonly highlightEnd = input<number | null>(null);
  readonly linkRequested = output<string>();
  readonly highlightMark = viewChild<ElementRef<HTMLElement>>('highlightMark');

  constructor() {
    afterNextRender(() => {
      const el = this.highlightMark()?.nativeElement;
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
    });
  }

  protected readonly groups = computed<SpeakerGroup[]>(() => {
    const t = this.transcript();
    if (!t) return [];
    return groupBySpeaker(t.segments);
  });

  /** Maps cumulative character offsets to determine per-segment highlights. */
  private readonly segmentOffsets = computed(() => {
    const t = this.transcript();
    if (!t) return new Map<TranscriptSegment, number>();
    const offsets = new Map<TranscriptSegment, number>();
    let cursor = 0;
    for (const seg of t.segments) {
      offsets.set(seg, cursor);
      cursor += seg.text.length + 1; // +1 for newline separator
    }
    return offsets;
  });

  protected segmentHighlight(seg: TranscriptSegment): SegmentHighlight | null {
    const start = this.highlightStart();
    const end = this.highlightEnd();
    if (start == null || end == null || start >= end) return null;

    const offsets = this.segmentOffsets();
    const segStart = offsets.get(seg);
    if (segStart == null) return null;
    const segEnd = segStart + seg.text.length;

    // Check if highlight range overlaps this segment
    const overlapStart = Math.max(start - segStart, 0);
    const overlapEnd = Math.min(end - segStart, seg.text.length);
    if (overlapStart >= overlapEnd) return null;

    return {
      before: seg.text.substring(0, overlapStart),
      highlighted: seg.text.substring(overlapStart, overlapEnd),
      after: seg.text.substring(overlapEnd),
    };
  }

  protected formatTime(seconds: number): string {
    const mm = Math.floor(seconds / 60)
      .toString()
      .padStart(2, '0');
    const ss = Math.floor(seconds % 60)
      .toString()
      .padStart(2, '0');
    return `${mm}:${ss}`;
  }
}

/**
 * Exported for component tests. Folds consecutive same-speaker segments
 * into a single group with the span [first.start, last.end].
 */
export function groupBySpeaker(segments: readonly TranscriptSegment[]): SpeakerGroup[] {
  const groups: SpeakerGroup[] = [];
  for (const segment of segments) {
    const current = groups[groups.length - 1];
    if (current && current.speakerLabel === segment.speakerLabel) {
      current.segments.push(segment);
      current.endSeconds = segment.endSeconds;
      // Preserve "linked" across the group if any segment has a link.
      current.linkedPersonId = current.linkedPersonId ?? segment.linkedPersonId;
    } else {
      groups.push({
        speakerLabel: segment.speakerLabel,
        linkedPersonId: segment.linkedPersonId,
        startSeconds: segment.startSeconds,
        endSeconds: segment.endSeconds,
        segments: [segment],
      });
    }
  }
  return groups;
}
