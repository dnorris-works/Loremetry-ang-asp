import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, firstValueFrom, of } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { StoryDocumentInput } from '../stories/story.models';
import { storyDocumentKey } from '../stories/story-file.utils';
import { SeriesApiService } from './series-api.service';
import { SeriesPanelDraft, SeriesBibleDocument, Series, SeriesDetail, CreateSeriesRequest } from './series.models';
import { WritingDocumentCategory } from '../writing/writing.models';

@Injectable({ providedIn: 'root' })
export class SeriesService {
  private readonly seriesApi = inject(SeriesApiService);
  private readonly auth = inject(AuthService);

  readonly series = signal<Series[]>([]);
  readonly isPanelOpen = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly editingDetail = signal<SeriesDetail | null>(null);
  readonly isPanelLoading = signal(false);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly panelLoadError = signal<string | null>(null);
  readonly recentCharacterFiles = signal<StoryDocumentInput[]>([]);
  readonly recentLocationFiles = signal<StoryDocumentInput[]>([]);
  readonly panelDraft = signal<SeriesPanelDraft | null>(null);

  readonly isEditing = computed(() => this.editingId() !== null);

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
    this.editingId.set(null);
    this.editingDetail.set(null);
    this.panelDraft.set(null);
    this.isPanelOpen.set(true);
    this.saveError.set(null);
    this.panelLoadError.set(null);
  }

  async openEditPanel(id: number): Promise<void> {
    this.editingId.set(id);
    this.panelDraft.set(null);
    this.isPanelOpen.set(true);
    this.saveError.set(null);
    this.panelLoadError.set(null);
    this.isPanelLoading.set(true);

    try {
      const detail = await firstValueFrom(this.seriesApi.getSeries(id));
      this.editingDetail.set(detail);
    } catch (error) {
      this.panelLoadError.set(this.readErrorMessage(error, 'Failed to load series.'));
      this.editingDetail.set(null);
    } finally {
      this.isPanelLoading.set(false);
    }
  }

  closePanel(): void {
    this.isPanelOpen.set(false);
    this.editingId.set(null);
    this.editingDetail.set(null);
    this.panelDraft.set(null);
    this.saveError.set(null);
    this.panelLoadError.set(null);
  }

  setPanelDraft(draft: SeriesPanelDraft): void {
    this.panelDraft.set(draft);
  }

  updatePanelDocumentText(fileName: string, category: WritingDocumentCategory, textContent: string): void {
    const draft = this.panelDraft();
    if (!draft) {
      return;
    }

    const key = fileName.toLowerCase();
    const updateFiles = (files: StoryDocumentInput[]) =>
      files.map((file) =>
        file.fileName.toLowerCase() === key ? { ...file, textContent } : file,
      );

    if (category === 'character') {
      this.panelDraft.set({ ...draft, characters: updateFiles(draft.characters) });
      return;
    }

    this.panelDraft.set({ ...draft, locations: updateFiles(draft.locations) });
  }

  applyDocumentTextUpdate(document: SeriesBibleDocument): void {
    const detail = this.editingDetail();
    if (!detail) {
      return;
    }

    this.editingDetail.set({
      ...detail,
      bibleDocuments: detail.bibleDocuments.map((item) =>
        item.id === document.id
          ? {
              ...item,
              textContent: document.textContent,
              binaryContentBase64: document.binaryContentBase64,
            }
          : item,
      ),
    });

    this.updatePanelDocumentText(document.fileName, document.category, document.textContent ?? '');
  }

  async updateSeriesDocumentText(
    seriesId: number,
    documentId: number,
    textContent: string,
  ): Promise<SeriesBibleDocument | null> {
    this.saveError.set(null);

    try {
      const document = await firstValueFrom(
        this.seriesApi.updateSeriesDocumentText(seriesId, documentId, textContent),
      );
      this.applyDocumentTextUpdate(document);
      return document;
    } catch (error) {
      this.saveError.set(this.readErrorMessage(error, 'Failed to save document.'));
      return null;
    }
  }

  async addSeries(request: CreateSeriesRequest): Promise<Series | null> {
    this.isSaving.set(true);
    this.saveError.set(null);

    try {
      const item = await firstValueFrom(
        this.seriesApi.createSeries({
          name: request.name.trim(),
          characters: request.characters,
          locations: request.locations,
        }),
      );

      this.series.update((series) => [item, ...series]);
      this.rememberCharacterFiles(request.characters);
      this.rememberLocationFiles(request.locations);
      this.closePanel();
      return item;
    } catch (error) {
      this.saveError.set(this.readErrorMessage(error, 'Failed to save series.'));
      return null;
    } finally {
      this.isSaving.set(false);
    }
  }

  async updateSeries(id: number, request: CreateSeriesRequest): Promise<Series | null> {
    this.isSaving.set(true);
    this.saveError.set(null);

    try {
      const item = await firstValueFrom(
        this.seriesApi.updateSeries(id, {
          name: request.name.trim(),
          characters: request.characters,
          locations: request.locations,
        }),
      );

      this.series.update((series) =>
        series.map((entry) => (entry.id === item.id ? item : entry)),
      );
      this.rememberCharacterFiles(request.characters);
      this.rememberLocationFiles(request.locations);
      this.closePanel();
      return item;
    } catch (error) {
      this.saveError.set(this.readErrorMessage(error, 'Failed to update series.'));
      return null;
    } finally {
      this.isSaving.set(false);
    }
  }

  rememberCharacterFiles(files: StoryDocumentInput[]): void {
    this.rememberFiles(files, this.recentCharacterFiles);
  }

  rememberLocationFiles(files: StoryDocumentInput[]): void {
    this.rememberFiles(files, this.recentLocationFiles);
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
    this.isPanelOpen.set(false);
    this.editingId.set(null);
    this.editingDetail.set(null);
    this.isPanelLoading.set(false);
    this.isLoading.set(false);
    this.isSaving.set(false);
    this.loadError.set(null);
    this.saveError.set(null);
    this.panelLoadError.set(null);
    this.panelDraft.set(null);
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
