export type TextDiffSegmentType = 'equal' | 'delete' | 'insert';

export interface TextDiffSegment {
  type: TextDiffSegmentType;
  text: string;
}

export type ReviewSuggestionSeverity = 'low' | 'medium' | 'high';

export interface ReviewSuggestionHunk {
  id: string;
  reportLabel: string;
  locationLabel: string;
  severity: ReviewSuggestionSeverity;
  contextBefore?: string;
  segments: TextDiffSegment[];
  /** Plain text to copy — typically the proposed replacement phrase or passage. */
  suggestedReplacement: string;
}
