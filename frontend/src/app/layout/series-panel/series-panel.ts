import { Component, effect, inject, signal } from '@angular/core';

import { populateSeriesForm } from '../../core/series/series-form.utils';
import { SeriesService } from '../../core/series/series.service';
import { StoryFilePicker } from '../../core/stories/story-file-picker/story-file-picker';
import { isEditableTextDocument } from '../../core/stories/story-file.utils';
import { StoryDocumentInput } from '../../core/stories/story.models';
import { WritingDocumentCategory } from '../../core/writing/writing.models';
import { WritingService } from '../../core/writing/writing.service';

@Component({
  selector: 'app-series-panel',
  imports: [StoryFilePicker],
  templateUrl: './series-panel.html',
  styleUrl: './series-panel.css',
})
export class SeriesPanel {
  private readonly seriesService = inject(SeriesService);
  private readonly writingService = inject(WritingService);

  protected readonly seriesName = signal('');
  protected readonly characterFiles = signal<StoryDocumentInput[]>([]);
  protected readonly locationFiles = signal<StoryDocumentInput[]>([]);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly isEditing = this.seriesService.isEditing;
  protected readonly isPanelLoading = this.seriesService.isPanelLoading;
  protected readonly recentCharacterFiles = this.seriesService.recentCharacterFiles;
  protected readonly recentLocationFiles = this.seriesService.recentLocationFiles;
  protected readonly isSaving = this.seriesService.isSaving;

  constructor() {
    effect(() => {
      const draft = this.seriesService.panelDraft();
      if (draft && this.seriesService.isPanelOpen()) {
        this.seriesName.set(draft.name);
        this.characterFiles.set(draft.characters);
        this.locationFiles.set(draft.locations);
        this.errorMessage.set(null);
        return;
      }

      const detail = this.seriesService.editingDetail();

      if (detail) {
        const form = populateSeriesForm(detail);
        this.seriesName.set(form.name);
        this.characterFiles.set(form.characters);
        this.locationFiles.set(form.locations);
        this.errorMessage.set(null);
        return;
      }

      if (!this.seriesService.isEditing()) {
        this.resetForm();
      }
    });

    effect(() => {
      const loadError = this.seriesService.panelLoadError();
      if (loadError) {
        this.errorMessage.set(loadError);
      }
    });
  }

  protected close(): void {
    this.resetForm();
    this.seriesService.closePanel();
  }

  protected async save(): Promise<void> {
    this.errorMessage.set(null);

    const name = this.seriesName().trim();
    const characters = this.characterFiles();
    const locations = this.locationFiles();

    if (!name) {
      this.errorMessage.set('Series name is required.');
      return;
    }

    const request = { name, characters, locations };
    const editingId = this.seriesService.editingId();
    const item = editingId
      ? await this.seriesService.updateSeries(editingId, request)
      : await this.seriesService.addSeries(request);

    if (item) {
      this.resetForm();
      return;
    }

    this.errorMessage.set(this.seriesService.saveError() ?? 'Failed to save series.');
  }

  protected onSeriesNameInput(event: Event): void {
    this.seriesName.set((event.target as HTMLInputElement).value);
  }

  protected onCharacterFilesChange(files: StoryDocumentInput[]): void {
    this.characterFiles.set(files);
    this.seriesService.rememberCharacterFiles(files);
  }

  protected onLocationFilesChange(files: StoryDocumentInput[]): void {
    this.locationFiles.set(files);
    this.seriesService.rememberLocationFiles(files);
  }

  protected onBrowseError(message: string): void {
    this.errorMessage.set(message);
  }

  protected openCharacter(file: StoryDocumentInput): void {
    this.openFileInEditor(file, 'character');
  }

  protected openLocation(file: StoryDocumentInput): void {
    this.openFileInEditor(file, 'location');
  }

  private openFileInEditor(file: StoryDocumentInput, category: WritingDocumentCategory): void {
    if (!isEditableTextDocument(file)) {
      this.errorMessage.set('Only .md and .txt files can be opened in the editor.');
      return;
    }

    this.persistPanelDraft();

    const parentId = this.seriesService.editingId();
    if (!parentId) {
      this.writingService.openDocument({
        fileName: file.fileName,
        content: file.textContent ?? '',
        mimeType: file.mimeType,
        context: {
          source: 'series',
          parentId: 0,
          category,
          fileName: file.fileName,
          mimeType: file.mimeType,
        },
      });
      return;
    }

    this.writingService.openDocument({
      fileName: file.fileName,
      content: file.textContent ?? '',
      mimeType: file.mimeType,
      context: {
        source: 'series',
        parentId,
        documentId: file.id,
        category,
        fileName: file.fileName,
        mimeType: file.mimeType,
      },
    });
  }

  private persistPanelDraft(): void {
    this.seriesService.setPanelDraft({
      name: this.seriesName(),
      characters: this.characterFiles(),
      locations: this.locationFiles(),
    });
  }

  private resetForm(): void {
    this.seriesName.set('');
    this.characterFiles.set([]);
    this.locationFiles.set([]);
    this.errorMessage.set(null);
  }
}
