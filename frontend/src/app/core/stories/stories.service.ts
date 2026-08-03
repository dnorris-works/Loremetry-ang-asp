import { Injectable, effect, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, firstValueFrom, of } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { StoriesApiService } from './stories-api.service';
import { CreateStoryRequest, Story, StoryDocumentInput } from './story.models';
import { storyDocumentKey } from './story-file.utils';

@Injectable({ providedIn: 'root' })
export class StoriesService {
  private readonly storiesApi = inject(StoriesApiService);
  private readonly auth = inject(AuthService);

  readonly stories = signal<Story[]>([]);
  readonly isAddPanelOpen = signal(false);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly recentManuscriptFiles = signal<StoryDocumentInput[]>([]);
  readonly recentBibleFiles = signal<StoryDocumentInput[]>([]);

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
    this.isAddPanelOpen.set(true);
    this.saveError.set(null);
  }

  closeAddPanel(): void {
    this.isAddPanelOpen.set(false);
    this.saveError.set(null);
  }

  async addStory(request: CreateStoryRequest): Promise<Story | null> {
    this.isSaving.set(true);
    this.saveError.set(null);

    try {
      const story = await firstValueFrom(
        this.storiesApi.createStory({
          name: request.name.trim(),
          manuscripts: request.manuscripts,
          bibles: request.bibles,
        }),
      );

      this.stories.update((stories) => [story, ...stories]);
      this.rememberManuscriptFiles(request.manuscripts);
      this.rememberBibleFiles(request.bibles);
      this.closeAddPanel();
      return story;
    } catch (error) {
      this.saveError.set(this.readErrorMessage(error, 'Failed to save story.'));
      return null;
    } finally {
      this.isSaving.set(false);
    }
  }

  rememberManuscriptFiles(files: StoryDocumentInput[]): void {
    this.rememberFiles(files, this.recentManuscriptFiles);
  }

  rememberBibleFiles(files: StoryDocumentInput[]): void {
    this.rememberFiles(files, this.recentBibleFiles);
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
    this.isAddPanelOpen.set(false);
    this.isLoading.set(false);
    this.isSaving.set(false);
    this.loadError.set(null);
    this.saveError.set(null);
    this.recentManuscriptFiles.set([]);
    this.recentBibleFiles.set([]);
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
