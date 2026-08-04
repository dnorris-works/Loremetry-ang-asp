import { Injectable, computed, inject, signal } from '@angular/core';

import { SeriesService } from '../series/series.service';
import { StoriesService } from '../stories/stories.service';
import { WritingDocumentContext } from './writing.models';

@Injectable({ providedIn: 'root' })
export class WritingService {
  private readonly storiesService = inject(StoriesService);
  private readonly seriesService = inject(SeriesService);

  readonly isPanelOpen = signal(false);
  readonly title = signal('');
  readonly content = signal('');
  readonly savedContent = signal('');
  readonly documentContext = signal<WritingDocumentContext | null>(null);
  readonly isSaving = signal(false);
  readonly saveError = signal<string | null>(null);

  readonly isDirty = computed(() => this.content() !== this.savedContent());
  readonly isDocumentMode = computed(() => this.documentContext() !== null);

  openPanel(): void {
    this.documentContext.set(null);
    this.title.set('');
    this.content.set('');
    this.savedContent.set('');
    this.saveError.set(null);
    this.isPanelOpen.set(true);
  }

  openDocument(params: {
    fileName: string;
    content: string;
    mimeType: string;
    context: WritingDocumentContext;
  }): void {
    this.documentContext.set(params.context);
    this.title.set(params.fileName);
    this.content.set(params.content);
    this.savedContent.set(params.content);
    this.saveError.set(null);
    this.isPanelOpen.set(true);
  }

  closePanel(): void {
    this.isPanelOpen.set(false);
    this.documentContext.set(null);
    this.title.set('');
    this.content.set('');
    this.savedContent.set('');
    this.saveError.set(null);
  }

  async save(): Promise<boolean> {
    const context = this.documentContext();
    const textContent = this.content();

    if (!context) {
      return false;
    }

    this.isSaving.set(true);
    this.saveError.set(null);

    try {
      if (context.parentId > 0) {
        let documentId = context.documentId ?? (await this.lookupDocumentId(context));

        if (!documentId) {
          documentId = await this.lookupDocumentId(context, true);
        }

        if (documentId) {
          const resolvedId = await this.persistWithRetry(context, documentId, textContent);
          if (!resolvedId) {
            return false;
          }

          this.documentContext.set({ ...context, documentId: resolvedId });
          this.savedContent.set(textContent);
          return true;
        }

        this.saveError.set(
          `Could not find "${context.fileName}" in the ${context.source}. Save the ${context.source} first or reopen the file.`,
        );
        return false;
      }

      if (context.source === 'story') {
        this.storiesService.updatePanelDocumentText(
          context.fileName,
          context.category,
          textContent,
        );
      } else {
        this.seriesService.updatePanelDocumentText(
          context.fileName,
          context.category,
          textContent,
        );
      }

      this.savedContent.set(textContent);
      return true;
    } finally {
      this.isSaving.set(false);
    }
  }

  reset(): void {
    this.closePanel();
  }

  private async persistWithRetry(
    context: WritingDocumentContext,
    documentId: number,
    textContent: string,
  ): Promise<number | null> {
    if (await this.persistDocumentText(context, documentId, textContent)) {
      return documentId;
    }

    const refreshedId = await this.lookupDocumentId(context, true);
    if (!refreshedId || refreshedId === documentId) {
      return null;
    }

    return (await this.persistDocumentText(context, refreshedId, textContent)) ? refreshedId : null;
  }

  private async persistDocumentText(
    context: WritingDocumentContext,
    documentId: number,
    textContent: string,
  ): Promise<boolean> {
    const saved =
      context.source === 'story'
        ? await this.storiesService.updateStoryDocumentText(
            context.parentId,
            documentId,
            textContent,
          )
        : await this.seriesService.updateSeriesDocumentText(
            context.parentId,
            documentId,
            textContent,
          );

    if (saved) {
      return true;
    }

    const serviceError =
      context.source === 'story'
        ? this.storiesService.saveError()
        : this.seriesService.saveError();

    this.saveError.set(serviceError ?? 'Failed to save document.');
    return false;
  }

  private async lookupDocumentId(
    context: WritingDocumentContext,
    forceRefresh = false,
  ): Promise<number | null> {
    if (context.source === 'story') {
      const detail = await this.storiesService.ensureEditingDetail(context.parentId, forceRefresh);

      const document = detail?.documents.find(
        (item) => item.fileName === context.fileName && item.kind === context.category,
      );
      return document?.id ?? null;
    }

    const detail = await this.seriesService.ensureEditingDetail(context.parentId, forceRefresh);

    const document = detail?.bibleDocuments.find(
      (item) => item.fileName === context.fileName && item.category === context.category,
    );
    return document?.id ?? null;
  }
}
