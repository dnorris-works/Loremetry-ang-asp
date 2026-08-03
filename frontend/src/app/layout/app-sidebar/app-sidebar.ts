import { Component, inject, signal } from '@angular/core';

import { SeriesService } from '../../core/series/series.service';
import { StoriesService } from '../../core/stories/stories.service';

@Component({
  selector: 'app-sidebar',
  templateUrl: './app-sidebar.html',
  styleUrl: './app-sidebar.css',
})
export class AppSidebar {
  private readonly seriesService = inject(SeriesService);
  private readonly storiesService = inject(StoriesService);

  protected readonly series = this.seriesService.series;
  protected readonly stories = this.storiesService.stories;
  protected readonly isSeriesOpen = signal(false);
  protected readonly isStoriesOpen = signal(false);

  protected toggleSeries(): void {
    this.isSeriesOpen.update((open) => !open);
  }

  protected toggleStories(): void {
    this.isStoriesOpen.update((open) => !open);
  }

  protected openAddSeriesPanel(event: Event): void {
    event.stopPropagation();
    this.storiesService.closeAddPanel();
    this.seriesService.openAddPanel();
  }

  protected openAddStoryPanel(event: Event): void {
    event.stopPropagation();
    this.seriesService.closeAddPanel();
    this.storiesService.openAddPanel();
  }
}
