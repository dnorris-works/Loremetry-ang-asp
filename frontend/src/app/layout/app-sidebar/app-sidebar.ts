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
    this.storiesService.closePanel();
    this.seriesService.openAddPanel();
  }

  protected openAddStoryPanel(event: Event): void {
    event.stopPropagation();
    this.seriesService.closePanel();
    this.storiesService.openAddPanel();
  }

  protected openEditSeries(id: number): void {
    this.storiesService.closePanel();
    void this.seriesService.openEditPanel(id);
  }

  protected openEditStory(id: number): void {
    this.seriesService.closePanel();
    void this.storiesService.openEditPanel(id);
  }

  protected isSeriesSelected(id: number): boolean {
    return this.seriesService.isPanelOpen() && this.seriesService.editingId() === id;
  }

  protected isStorySelected(id: number): boolean {
    return this.storiesService.isPanelOpen() && this.storiesService.editingId() === id;
  }
}
