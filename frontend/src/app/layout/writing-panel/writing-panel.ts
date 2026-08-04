import { Component, computed, inject, signal, viewChild } from '@angular/core';

import { MarkdownEditor } from '../../core/editor/markdown-editor/markdown-editor';
import { writingDestinationKey } from '../../core/writing/writing.models';
import { WritingService } from '../../core/writing/writing.service';
import { WritingAssistant } from './writing-assistant/writing-assistant';

const MIN_ASSISTANT_WIDTH = 240;
const MAX_ASSISTANT_WIDTH = 560;
const DEFAULT_ASSISTANT_WIDTH = 320;

@Component({
  selector: 'app-writing-panel',
  imports: [MarkdownEditor, WritingAssistant],
  templateUrl: './writing-panel.html',
  styleUrl: './writing-panel.css',
})
export class WritingPanel {
  private readonly writingService = inject(WritingService);
  private readonly markdownEditor = viewChild(MarkdownEditor);

  protected readonly title = this.writingService.title;
  protected readonly content = this.writingService.content;
  protected readonly documentContext = this.writingService.documentContext;
  protected readonly documentTypes = this.writingService.documentTypes;
  protected readonly documentTypeCode = this.writingService.documentTypeCode;
  protected readonly documentTypeDisplayName = this.writingService.documentTypeDisplayName;
  protected readonly destinationKey = this.writingService.destinationKey;
  protected readonly selectedDestination = this.writingService.selectedDestination;
  protected readonly destinationSeries = this.writingService.destinationSeries;
  protected readonly destinationStories = this.writingService.destinationStories;
  protected readonly hasDestinations = this.writingService.hasDestinations;
  protected readonly isDocumentMode = this.writingService.isDocumentMode;
  protected readonly isDirty = this.writingService.isDirty;
  protected readonly isSaving = this.writingService.isSaving;
  protected readonly saveError = this.writingService.saveError;

  protected readonly assistantWidth = signal(DEFAULT_ASSISTANT_WIDTH);
  protected readonly isResizing = signal(false);

  protected readonly assistantPaneStyle = computed(() => ({
    width: `${this.assistantWidth()}px`,
  }));

  protected readonly headerTitle = computed(() =>
    this.isDocumentMode() ? this.title() : 'Write',
  );

  protected readonly assistantDocumentTitle = computed(() => {
    if (this.isDocumentMode()) {
      return this.title();
    }

    const draftTitle = this.title().trim();
    return draftTitle || 'Untitled draft';
  });

  protected readonly destinationDisplayName = computed(() => {
    const destination = this.selectedDestination();
    if (destination) {
      return destination.name;
    }

    const context = this.documentContext();
    if (!context) {
      return '—';
    }

    return context.source === 'story' ? 'Story' : 'Series';
  });

  protected readonly subtitle = computed(() => {
    const context = this.documentContext();
    if (!context) {
      return 'Choose a destination and document type, then start writing.';
    }

    return 'Saved document · destination and type are fixed for this file.';
  });

  protected close(): void {
    this.writingService.closePanel();
  }

  protected onTitleInput(event: Event): void {
    this.writingService.title.set((event.target as HTMLInputElement).value);
  }

  protected onDocumentTypeChange(event: Event): void {
    this.writingService.documentTypeCode.set((event.target as HTMLSelectElement).value);
  }

  protected onDestinationChange(event: Event): void {
    this.writingService.setDestinationKey((event.target as HTMLSelectElement).value);
  }

  protected destinationOptionValue(
    source: 'story' | 'series',
    id: number,
  ): string {
    return writingDestinationKey(source, id);
  }

  protected onContentChange(markdown: string): void {
    this.writingService.content.set(markdown);
  }

  protected onSplitPointerDown(event: PointerEvent): void {
    event.preventDefault();

    const handle = event.currentTarget as HTMLElement;
    const startX = event.clientX;
    const startWidth = this.assistantWidth();

    this.isResizing.set(true);
    handle.setPointerCapture(event.pointerId);

    const onPointerMove = (moveEvent: PointerEvent): void => {
      const delta = startX - moveEvent.clientX;
      this.assistantWidth.set(
        Math.min(MAX_ASSISTANT_WIDTH, Math.max(MIN_ASSISTANT_WIDTH, startWidth + delta)),
      );
    };

    const onPointerUp = (upEvent: PointerEvent): void => {
      this.isResizing.set(false);
      handle.releasePointerCapture(upEvent.pointerId);
      handle.removeEventListener('pointermove', onPointerMove);
      handle.removeEventListener('pointerup', onPointerUp);
      handle.removeEventListener('pointercancel', onPointerUp);
    };

    handle.addEventListener('pointermove', onPointerMove);
    handle.addEventListener('pointerup', onPointerUp);
    handle.addEventListener('pointercancel', onPointerUp);
  }

  protected onSplitKeyDown(event: KeyboardEvent): void {
    const step = event.shiftKey ? 32 : 16;

    switch (event.key) {
      case 'ArrowLeft':
        event.preventDefault();
        this.assistantWidth.update((width) =>
          Math.min(MAX_ASSISTANT_WIDTH, width + step),
        );
        break;
      case 'ArrowRight':
        event.preventDefault();
        this.assistantWidth.update((width) =>
          Math.max(MIN_ASSISTANT_WIDTH, width - step),
        );
        break;
      case 'Home':
        event.preventDefault();
        this.assistantWidth.set(MAX_ASSISTANT_WIDTH);
        break;
      case 'End':
        event.preventDefault();
        this.assistantWidth.set(MIN_ASSISTANT_WIDTH);
        break;
    }
  }

  protected async save(): Promise<void> {
    const editor = this.markdownEditor();
    if (editor) {
      this.writingService.content.set(editor.flushMarkdown());
    }

    await this.writingService.save();
  }
}
