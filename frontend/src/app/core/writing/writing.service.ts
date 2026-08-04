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
      if (context.documentId && context.parentId > 0) {
        const saved =
          context.source === 'story'
            ? await this.storiesService.updateStoryDocumentText(
                context.parentId,
                context.documentId,
                textContent,
              )
            : await this.seriesService.updateSeriesDocumentText(
                context.parentId,
                context.documentId,
                textContent,
              );

        if (!saved) {
          this.saveError.set(
            context.source === 'story'
              ? (this.storiesService.saveError() ?? 'Failed to save document.')
              : (this.seriesService.saveError() ?? 'Failed to save document.'),
          );
          return false;
        }
      } else {
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
}
