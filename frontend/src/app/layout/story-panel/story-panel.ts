import { Component, inject, signal } from '@angular/core';

import { StoryFilePicker } from '../../core/stories/story-file-picker/story-file-picker';
import { StoryDocumentInput } from '../../core/stories/story.models';
import { StoriesService } from '../../core/stories/stories.service';

@Component({
  selector: 'app-story-panel',
  imports: [StoryFilePicker],
  templateUrl: './story-panel.html',
  styleUrl: './story-panel.css',
})
export class StoryPanel {
  private readonly storiesService = inject(StoriesService);

  protected readonly storyName = signal('');
  protected readonly manuscriptFiles = signal<StoryDocumentInput[]>([]);
  protected readonly bibleFiles = signal<StoryDocumentInput[]>([]);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly recentManuscriptFiles = this.storiesService.recentManuscriptFiles;
  protected readonly recentBibleFiles = this.storiesService.recentBibleFiles;
  protected readonly isSaving = this.storiesService.isSaving;

  protected close(): void {
    this.resetForm();
    this.storiesService.closeAddPanel();
  }

  protected async save(): Promise<void> {
    this.errorMessage.set(null);

    const name = this.storyName().trim();
    const manuscripts = this.manuscriptFiles();
    const bibles = this.bibleFiles();

    if (!name) {
      this.errorMessage.set('Story name is required.');
      return;
    }

    if (manuscripts.length === 0) {
      this.errorMessage.set('Select at least one manuscript file before saving.');
      return;
    }

    const story = await this.storiesService.addStory({
      name,
      manuscripts,
      bibles,
    });

    if (story) {
      this.resetForm();
      return;
    }

    this.errorMessage.set(this.storiesService.saveError() ?? 'Failed to save story.');
  }

  protected onStoryNameInput(event: Event): void {
    this.storyName.set((event.target as HTMLInputElement).value);
  }

  protected onManuscriptFilesChange(files: StoryDocumentInput[]): void {
    this.manuscriptFiles.set(files);
    this.storiesService.rememberManuscriptFiles(files);
  }

  protected onBibleFilesChange(files: StoryDocumentInput[]): void {
    this.bibleFiles.set(files);
    this.storiesService.rememberBibleFiles(files);
  }

  protected onBrowseError(message: string): void {
    this.errorMessage.set(message);
  }

  private resetForm(): void {
    this.storyName.set('');
    this.manuscriptFiles.set([]);
    this.bibleFiles.set([]);
    this.errorMessage.set(null);
  }
}
