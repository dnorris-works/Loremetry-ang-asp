import { ReviewSuggestionHunk } from './review.models';
import { buildWordDiffSegments } from './word-diff.utils';

const showDontTellBefore =
  'She was very angry when she saw the letter on the table.';
const showDontTellAfter =
  'Her hands shook when she saw the letter on the table.';

const aiIsmsBefore =
  'In the dimly lit room, a sense of unease permeated the atmosphere.';
const aiIsmsAfter =
  'The lamp flickered. Dust hung in the air and nobody spoke.';

const continuityBefore =
  'Marcus left the tavern at midnight, alone.';
const continuityAfter =
  'Marcus left the tavern at midnight with Elena at his side.';

export const SAMPLE_REVIEW_HUNKS: ReviewSuggestionHunk[] = [
  {
    id: 'sample-sdt-1',
    reportLabel: 'Show don\'t tell',
    locationLabel: 'Ch. 3 · mid-scene',
    severity: 'medium',
    contextBefore: 'The envelope had no return address.',
    segments: buildWordDiffSegments(showDontTellBefore, showDontTellAfter),
    suggestedReplacement: showDontTellAfter,
  },
  {
    id: 'sample-ai-isms-1',
    reportLabel: 'AI-isms',
    locationLabel: 'Ch. 1 · opening',
    severity: 'low',
    contextBefore: 'Rain tapped the window.',
    segments: buildWordDiffSegments(aiIsmsBefore, aiIsmsAfter),
    suggestedReplacement: aiIsmsAfter,
  },
  {
    id: 'sample-continuity-1',
    reportLabel: 'Continuity check',
    locationLabel: 'Ch. 12 · scene break',
    severity: 'high',
    contextBefore: 'Elena was still inside when the bells rang.',
    segments: buildWordDiffSegments(continuityBefore, continuityAfter),
    suggestedReplacement: continuityAfter,
  },
];
