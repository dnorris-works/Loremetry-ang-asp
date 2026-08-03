import { Injectable, effect, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, firstValueFrom, of } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { StoryDocumentInput } from '../stories/story.models';
import { storyDocumentKey } from '../stories/story-file.utils';
import { SeriesApiService } from './series-api.service';
import { CreateSeriesRequest, Series } from './series.models';

@Injectable({ providedIn: 'root' })
export class SeriesService {
  private readonly seriesApi = inject(SeriesApiService);
  private readonly auth = inject(AuthService);

  readonly series = signal<Series[]>([]);
  readonly isAddPanelOpen = signal(false);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly recentBibleFiles = signal<StoryDocumentInput[]>([]);

  constructor() {
    effect(() => {
      if (this.auth.enteredApp()) {
        void this.loadSeries();
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

  async addSeries(request: CreateSeriesRequest): Promise<Series | null> {
    this.isSaving.set(true);
    this.saveError.set(null);

    try {
      const item = await firstValueFrom(
        this.seriesApi.createSeries({
          name: request.name.trim(),
          bibles: request.bibles,
        }),
      );

      this.series.update((series) => [item, ...series]);
      this.rememberBibleFiles(request.bibles);
      this.closeAddPanel();
      return item;
    } catch (error) {
      this.saveError.set(this.readErrorMessage(error, 'Failed to save series.'));
      return null;
    } finally {
      this.isSaving.set(false);
    }
  }

  rememberBibleFiles(files: StoryDocumentInput[]): void {
    for (const file of files) {
      const key = storyDocumentKey(file);
      this.recentBibleFiles.update((existing) => [
        file,
        ...existing.filter((item) => storyDocumentKey(item) !== key),
      ]);
    }
  }

  private async loadSeries(): Promise<void> {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.seriesApi
      .listSeries()
      .pipe(
        catchError((error: HttpErrorResponse) => {
          this.loadError.set(this.readErrorMessage(error, 'Failed to load series.'));
          return of(null);
        }),
      )
      .subscribe((items) => {
        this.isLoading.set(false);

        if (items) {
          this.series.set(items);
        }
      });
  }

  private reset(): void {
    this.series.set([]);
    this.isAddPanelOpen.set(false);
    this.isLoading.set(false);
    this.isSaving.set(false);
    this.loadError.set(null);
    this.saveError.set(null);
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
