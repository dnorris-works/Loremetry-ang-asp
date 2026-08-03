import { Component, computed, inject, signal } from '@angular/core';

import { AddStoryToSeriesDialog } from '../add-story-to-series-dialog/add-story-to-series-dialog';
import { SeriesService } from '../../core/series/series.service';
import { Story } from '../../core/stories/story.models';
import { StoriesService } from '../../core/stories/stories.service';

@Component({
  selector: 'app-sidebar',
  imports: [AddStoryToSeriesDialog],
  templateUrl: './app-sidebar.html',
  styleUrl: './app-sidebar.css',
})
export class AppSidebar {
  private readonly seriesService = inject(SeriesService);
  private readonly storiesService = inject(StoriesService);

  protected readonly series = this.seriesService.series;
  protected readonly stories = this.storiesService.stories;
  protected readonly unassignedStories = computed(() =>
    this.stories().filter((story) => !story.seriesId),
  );
  protected readonly isSeriesOpen = signal(false);
  protected readonly isStoriesOpen = signal(false);
  protected readonly addToSeriesStory = signal<Story | null>(null);

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

  protected openAddToSeriesModal(story: Story, event: Event): void {
    event.stopPropagation();
    this.addToSeriesStory.set(story);
  }

  protected closeAddToSeriesModal(): void {
    this.addToSeriesStory.set(null);
  }

  protected isSeriesSelected(id: number): boolean {
    return this.seriesService.isPanelOpen() && this.seriesService.editingId() === id;
  }

  protected isStorySelected(id: number): boolean {
    return this.storiesService.isPanelOpen() && this.storiesService.editingId() === id;
  }

  protected storiesForSeries(seriesId: number): Story[] {
    return this.stories().filter((story) => story.seriesId === seriesId);
  }
}
