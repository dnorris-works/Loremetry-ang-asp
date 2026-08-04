import { Component, computed, inject, viewChild } from '@angular/core';

import { MarkdownEditor } from '../../core/editor/markdown-editor/markdown-editor';
import { writingDestinationKey } from '../../core/writing/writing.models';
import { WritingService } from '../../core/writing/writing.service';

@Component({
  selector: 'app-writing-panel',
  imports: [MarkdownEditor],
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
  protected readonly destinationSeries = this.writingService.destinationSeries;
  protected readonly destinationStories = this.writingService.destinationStories;
  protected readonly hasDestinations = this.writingService.hasDestinations;
  protected readonly isDocumentMode = this.writingService.isDocumentMode;
  protected readonly isDirty = this.writingService.isDirty;
  protected readonly isSaving = this.writingService.isSaving;
  protected readonly saveError = this.writingService.saveError;

  protected readonly headerTitle = computed(() =>
    this.isDocumentMode() ? this.title() : 'Write',
  );

  protected readonly subtitle = computed(() => {
    const context = this.documentContext();
    if (!context) {
      return 'Draft with formatting tools. Content is stored as Markdown.';
    }

    const ownerLabel = context.source === 'story' ? 'Story document' : 'Series document';
    return `${ownerLabel} · ${this.documentTypeDisplayName()}`;
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

  protected async save(): Promise<void> {
    const editor = this.markdownEditor();
    if (editor) {
      this.writingService.content.set(editor.flushMarkdown());
    }

    await this.writingService.save();
  }
}
