import { Component, inject } from '@angular/core';

import { ReviewPanelService } from '../../core/review/review-panel.service';
import { DiffHunkView } from './diff-hunk-view/diff-hunk-view';

@Component({
  selector: 'app-review-panel',
  imports: [DiffHunkView],
  templateUrl: './review-panel.html',
  styleUrl: './review-panel.css',
})
export class ReviewPanel {
  private readonly reviewPanelService = inject(ReviewPanelService);

  protected readonly hunks = this.reviewPanelService.hunks;

  protected close(): void {
    this.reviewPanelService.closePanel();
  }
}
