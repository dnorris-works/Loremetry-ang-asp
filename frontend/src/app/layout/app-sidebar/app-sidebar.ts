import { CdkDrag, CdkDragDrop, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
import { Component, computed, inject, signal } from '@angular/core';

import { AddStoryToSeriesDialog } from '../add-story-to-series-dialog/add-story-to-series-dialog';
import { SeriesService } from '../../core/series/series.service';
import { Story } from '../../core/stories/story.models';
import { StoriesService } from '../../core/stories/stories.service';
import { WritingService } from '../../core/writing/writing.service';

@Component({
  selector: 'app-sidebar',
  imports: [AddStoryToSeriesDialog, CdkDrag, CdkDropList],
  templateUrl: './app-sidebar.html',
  styleUrl: './app-sidebar.css',
})
export class AppSidebar {
  private readonly seriesService = inject(SeriesService);
  private readonly storiesService = inject(StoriesService);
  private readonly writingService = inject(WritingService);

  protected readonly series = this.seriesService.series;
  protected readonly stories = this.storiesService.stories;
  protected readonly unassignedStories = computed(() =>
    this.stories().filter((story) => !story.seriesId),
  );
  protected readonly seriesDropListIds = computed(() =>
    this.series().map((item) => this.seriesDropListId(item.id)),
  );
  protected readonly isSeriesOpen = signal(false);
  protected readonly isStoriesOpen = signal(false);
  protected readonly addToSeriesStory = signal<Story | null>(null);
  protected readonly isAssigning = signal(false);
  protected readonly assignError = signal<string | null>(null);

  protected toggleSeries(): void {
    this.isSeriesOpen.update((open) => !open);
  }

  protected toggleStories(): void {
    this.isStoriesOpen.update((open) => !open);
  }

  protected openAddSeriesPanel(event: Event): void {
    event.stopPropagation();
    this.writingService.closePanel();
    this.storiesService.closePanel();
    this.seriesService.openAddPanel();
  }

  protected openAddStoryPanel(event: Event): void {
    event.stopPropagation();
    this.writingService.closePanel();
    this.seriesService.closePanel();
    this.storiesService.openAddPanel();
  }

  protected openEditSeries(id: number): void {
    this.writingService.closePanel();
    this.storiesService.closePanel();
    void this.seriesService.openEditPanel(id);
  }

  protected openEditStory(id: number): void {
    this.writingService.closePanel();
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
    return this.stories()
      .filter((story) => story.seriesId === seriesId)
      .sort((left, right) => left.seriesSortOrder - right.seriesSortOrder);
  }

  protected seriesDropListId(seriesId: number): string {
    return `series-drop-${seriesId}`;
  }

  protected onStoryDragStarted(): void {
    this.assignError.set(null);
    this.isSeriesOpen.set(true);
  }

  protected async onSeriesStoriesDropped(
    event: CdkDragDrop<Story[]>,
    seriesId: number,
  ): Promise<void> {
    if (this.isAssigning()) {
      return;
    }

    const currentStories = this.storiesForSeries(seriesId);
    const orderedIds = currentStories.map((story) => story.id);

    if (event.previousContainer === event.container) {
      if (event.previousIndex === event.currentIndex) {
        return;
      }

      moveItemInArray(orderedIds, event.previousIndex, event.currentIndex);
    } else {
      const story = event.item.data as Story;
      if (!story || story.seriesId === seriesId) {
        return;
      }

      orderedIds.splice(event.currentIndex, 0, story.id);
    }

    this.isAssigning.set(true);
    this.assignError.set(null);

    const saved = await this.storiesService.setSeriesStoryOrder(seriesId, orderedIds);

    this.isAssigning.set(false);

    if (!saved) {
      this.assignError.set(
        this.storiesService.saveError() ?? 'Failed to update story order.',
      );
    }
  }
}
