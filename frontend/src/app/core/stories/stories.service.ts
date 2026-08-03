import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, firstValueFrom, of } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { SeriesApiService } from '../series/series-api.service';
import { StoriesApiService } from './stories-api.service';
import { CreateStoryRequest, Story, StoryDetail, StoryDocumentInput } from './story.models';
import { storyDocumentKey } from './story-file.utils';

@Injectable({ providedIn: 'root' })
export class StoriesService {
  private readonly storiesApi = inject(StoriesApiService);
  private readonly seriesApi = inject(SeriesApiService);
  private readonly auth = inject(AuthService);

  readonly stories = signal<Story[]>([]);
  readonly isPanelOpen = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly editingDetail = signal<StoryDetail | null>(null);
  readonly isPanelLoading = signal(false);
  readonly panelLoadError = signal<string | null>(null);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly recentManuscriptFiles = signal<StoryDocumentInput[]>([]);
  readonly recentCharacterFiles = signal<StoryDocumentInput[]>([]);
  readonly recentLocationFiles = signal<StoryDocumentInput[]>([]);

  readonly isEditing = computed(() => this.editingId() !== null);

  constructor() {
    effect(() => {
      if (this.auth.enteredApp()) {
        void this.loadStories();
        return;
      }

      this.reset();
    });
  }

  openAddPanel(): void {
    this.editingId.set(null);
    this.editingDetail.set(null);
    this.isPanelOpen.set(true);
    this.saveError.set(null);
    this.panelLoadError.set(null);
  }

  async openEditPanel(id: number): Promise<void> {
    this.editingId.set(id);
    this.isPanelOpen.set(true);
    this.saveError.set(null);
    this.panelLoadError.set(null);
    this.isPanelLoading.set(true);

    try {
      const detail = await firstValueFrom(this.storiesApi.getStory(id));
      this.editingDetail.set(detail);
    } catch (error) {
      this.panelLoadError.set(this.readErrorMessage(error, 'Failed to load story.'));
      this.editingDetail.set(null);
    } finally {
      this.isPanelLoading.set(false);
    }
  }

  closePanel(): void {
    this.isPanelOpen.set(false);
    this.editingId.set(null);
    this.editingDetail.set(null);
    this.saveError.set(null);
    this.panelLoadError.set(null);
  }

  async addStory(request: CreateStoryRequest): Promise<Story | null> {
    this.isSaving.set(true);
    this.saveError.set(null);

    try {
      const story = await firstValueFrom(
        this.storiesApi.createStory({
          name: request.name.trim(),
          manuscripts: request.manuscripts,
          characters: request.characters,
          locations: request.locations,
        }),
      );

      this.stories.update((stories) => [story, ...stories]);
      this.rememberManuscriptFiles(request.manuscripts);
      this.rememberCharacterFiles(request.characters);
      this.rememberLocationFiles(request.locations);
      this.closePanel();
      return story;
    } catch (error) {
      this.saveError.set(this.readErrorMessage(error, 'Failed to save story.'));
      return null;
    } finally {
      this.isSaving.set(false);
    }
  }

  async updateStory(id: number, request: CreateStoryRequest): Promise<Story | null> {
    this.isSaving.set(true);
    this.saveError.set(null);

    try {
      const story = await firstValueFrom(
        this.storiesApi.updateStory(id, {
          name: request.name.trim(),
          manuscripts: request.manuscripts,
          characters: request.characters,
          locations: request.locations,
        }),
      );

      this.stories.update((stories) =>
        stories.map((item) => (item.id === story.id ? story : item)),
      );
      this.rememberManuscriptFiles(request.manuscripts);
      this.rememberCharacterFiles(request.characters);
      this.rememberLocationFiles(request.locations);
      this.closePanel();
      return story;
    } catch (error) {
      this.saveError.set(this.readErrorMessage(error, 'Failed to update story.'));
      return null;
    } finally {
      this.isSaving.set(false);
    }
  }

  async assignToSeries(storyId: number, seriesId: number): Promise<Story | null> {
    this.saveError.set(null);

    try {
      const story = await firstValueFrom(this.storiesApi.assignStoryToSeries(storyId, seriesId));
      this.stories.update((stories) =>
        stories.map((item) => (item.id === story.id ? story : item)),
      );
      return story;
    } catch (error) {
      this.saveError.set(this.readErrorMessage(error, 'Failed to add story to series.'));
      return null;
    }
  }

  async setSeriesStoryOrder(seriesId: number, storyIds: number[]): Promise<boolean> {
    this.saveError.set(null);

    try {
      const updatedStories = await firstValueFrom(
        this.seriesApi.reorderSeriesStories(seriesId, storyIds),
      );
      const updatedById = new Map(updatedStories.map((story) => [story.id, story]));
      this.stories.update((stories) =>
        stories.map((story) => updatedById.get(story.id) ?? story),
      );
      return true;
    } catch (error) {
      this.saveError.set(this.readErrorMessage(error, 'Failed to reorder stories.'));
      return false;
    }
  }

  rememberManuscriptFiles(files: StoryDocumentInput[]): void {
    this.rememberFiles(files, this.recentManuscriptFiles);
  }

  rememberCharacterFiles(files: StoryDocumentInput[]): void {
    this.rememberFiles(files, this.recentCharacterFiles);
  }

  rememberLocationFiles(files: StoryDocumentInput[]): void {
    this.rememberFiles(files, this.recentLocationFiles);
  }

  private async loadStories(): Promise<void> {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.storiesApi
      .listStories()
      .pipe(
        catchError((error: HttpErrorResponse) => {
          this.loadError.set(this.readErrorMessage(error, 'Failed to load stories.'));
          return of(null);
        }),
      )
      .subscribe((stories) => {
        this.isLoading.set(false);

        if (stories) {
          this.stories.set(stories);
        }
      });
  }

  private rememberFiles(
    files: StoryDocumentInput[],
    target: ReturnType<typeof signal<StoryDocumentInput[]>>,
  ): void {
    for (const file of files) {
      const key = storyDocumentKey(file);
      target.update((existing) => [
        file,
        ...existing.filter((item) => storyDocumentKey(item) !== key),
      ]);
    }
  }

  private reset(): void {
    this.stories.set([]);
    this.isPanelOpen.set(false);
    this.editingId.set(null);
    this.editingDetail.set(null);
    this.isPanelLoading.set(false);
    this.isLoading.set(false);
    this.isSaving.set(false);
    this.loadError.set(null);
    this.saveError.set(null);
    this.panelLoadError.set(null);
    this.recentManuscriptFiles.set([]);
    this.recentCharacterFiles.set([]);
    this.recentLocationFiles.set([]);
  }

  private readErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      if (typeof error.error?.message === 'string') {
        return error.error.message;
      }
    }

    return fallback;
  }
}
