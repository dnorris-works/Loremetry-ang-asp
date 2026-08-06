import { Injectable, signal } from '@angular/core';

import { ReviewSuggestionHunk } from './review.models';
import { SAMPLE_REVIEW_HUNKS } from './sample-review-hunks';

@Injectable({ providedIn: 'root' })
export class ReviewPanelService {
  readonly isPanelOpen = signal(false);
  /** Placeholder hunks until report pipelines populate this panel. */
  readonly hunks = signal<ReadonlyArray<ReviewSuggestionHunk>>(SAMPLE_REVIEW_HUNKS);

  openPanel(): void {
    this.isPanelOpen.set(true);
  }

  closePanel(): void {
    this.isPanelOpen.set(false);
  }
}
