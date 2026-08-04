import { Component, computed, inject, viewChild } from '@angular/core';

import { MarkdownEditor } from '../../core/editor/markdown-editor/markdown-editor';
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
    return `${ownerLabel} · ${context.category}`;
  });

  protected close(): void {
    this.writingService.closePanel();
  }

  protected onTitleInput(event: Event): void {
    this.writingService.title.set((event.target as HTMLInputElement).value);
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
