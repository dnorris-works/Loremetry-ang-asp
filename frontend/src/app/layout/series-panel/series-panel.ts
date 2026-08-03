import { Component, inject, signal } from '@angular/core';

import { StoryFilePicker } from '../../core/stories/story-file-picker/story-file-picker';
import { StoryDocumentInput } from '../../core/stories/story.models';
import { SeriesService } from '../../core/series/series.service';

@Component({
  selector: 'app-series-panel',
  imports: [StoryFilePicker],
  templateUrl: './series-panel.html',
  styleUrl: './series-panel.css',
})
export class SeriesPanel {
  private readonly seriesService = inject(SeriesService);

  protected readonly seriesName = signal('');
  protected readonly bibleFiles = signal<StoryDocumentInput[]>([]);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly recentBibleFiles = this.seriesService.recentBibleFiles;
  protected readonly isSaving = this.seriesService.isSaving;

  protected close(): void {
    this.resetForm();
    this.seriesService.closeAddPanel();
  }

  protected async save(): Promise<void> {
    this.errorMessage.set(null);

    const name = this.seriesName().trim();
    const bibles = this.bibleFiles();

    if (!name) {
      this.errorMessage.set('Series name is required.');
      return;
    }

    const item = await this.seriesService.addSeries({ name, bibles });

    if (item) {
      this.resetForm();
      return;
    }

    this.errorMessage.set(this.seriesService.saveError() ?? 'Failed to save series.');
  }

  protected onSeriesNameInput(event: Event): void {
    this.seriesName.set((event.target as HTMLInputElement).value);
  }

  protected onBibleFilesChange(files: StoryDocumentInput[]): void {
    this.bibleFiles.set(files);
    this.seriesService.rememberBibleFiles(files);
  }

  protected onBrowseError(message: string): void {
    this.errorMessage.set(message);
  }

  private resetForm(): void {
    this.seriesName.set('');
    this.bibleFiles.set([]);
    this.errorMessage.set(null);
  }
}
