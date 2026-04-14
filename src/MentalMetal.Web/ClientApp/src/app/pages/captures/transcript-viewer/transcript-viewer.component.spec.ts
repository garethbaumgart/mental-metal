import { groupBySpeaker } from './transcript-viewer.component';
import { TranscriptSegment } from '../../../shared/models/capture.model';

function seg(partial: Partial<TranscriptSegment>): TranscriptSegment {
  return {
    startSeconds: 0,
    endSeconds: 1,
    speakerLabel: 'Speaker A',
    text: 'hi',
    linkedPersonId: null,
    ...partial,
  };
}

describe('groupBySpeaker', () => {
  it('folds consecutive segments with same speaker into one group', () => {
    const segments: TranscriptSegment[] = [
      seg({ startSeconds: 0, endSeconds: 2, speakerLabel: 'A', text: 'hi' }),
      seg({ startSeconds: 2, endSeconds: 4, speakerLabel: 'A', text: 'there' }),
      seg({ startSeconds: 4, endSeconds: 6, speakerLabel: 'B', text: 'hello' }),
    ];

    const groups = groupBySpeaker(segments);

    expect(groups.length).toBe(2);
    expect(groups[0].speakerLabel).toBe('A');
    expect(groups[0].segments.length).toBe(2);
    expect(groups[0].startSeconds).toBe(0);
    expect(groups[0].endSeconds).toBe(4);
    expect(groups[1].speakerLabel).toBe('B');
  });

  it('alternating speakers produce separate groups', () => {
    const segments: TranscriptSegment[] = [
      seg({ speakerLabel: 'A' }),
      seg({ speakerLabel: 'B' }),
      seg({ speakerLabel: 'A' }),
    ];
    const groups = groupBySpeaker(segments);
    expect(groups.length).toBe(3);
  });

  it('empty input returns empty array', () => {
    expect(groupBySpeaker([])).toEqual([]);
  });

  it('preserves linkedPersonId when any segment in the group has one', () => {
    const groups = groupBySpeaker([
      seg({ speakerLabel: 'A', linkedPersonId: null }),
      seg({ speakerLabel: 'A', linkedPersonId: 'person-1' }),
    ]);
    expect(groups[0].linkedPersonId).toBe('person-1');
  });
});
