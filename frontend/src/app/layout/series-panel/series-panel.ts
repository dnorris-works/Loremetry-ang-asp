import { Component, effect, inject, signal } from '@angular/core';

import { StoryFilePicker } from '../../core/stories/story-file-picker/story-file-picker';
import { populateSeriesForm } from '../../core/series/series-form.utils';
import { SeriesService } from '../../core/series/series.service';
import { StoryDocumentInput } from '../../core/stories/story.models';

@Component({
  selector: 'app-series-panel',
  imports: [StoryFilePicker],
  templateUrl: './series-panel.html',
  styleUrl: './series-panel.css',
})
export class SeriesPanel {
  private readonly seriesService = inject(SeriesService);

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

  private resetForm(): void {
    this.seriesName.set('');
    this.characterFiles.set([]);
    this.locationFiles.set([]);
    this.errorMessage.set(null);
  }
}
