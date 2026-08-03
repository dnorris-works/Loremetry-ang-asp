import { Component, inject, signal } from '@angular/core';

import { StoryFilePicker } from '../../core/stories/story-file-picker/story-file-picker';
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
  protected readonly manuscriptFiles = signal<string[]>([]);
  protected readonly bibleFiles = signal<string[]>([]);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly recentManuscriptFiles = this.storiesService.recentManuscriptFiles;
  protected readonly recentBibleFiles = this.storiesService.recentBibleFiles;

  protected close(): void {
    this.resetForm();
    this.storiesService.closeAddPanel();
  }

  protected save(): void {
    this.errorMessage.set(null);

    const name = this.storyName().trim();
    const manuscriptFiles = this.manuscriptFiles();
    const bibleFiles = this.bibleFiles();

    if (!name) {
      this.errorMessage.set('Story name is required.');
      return;
    }

    if (manuscriptFiles.length === 0) {
      this.errorMessage.set('Select at least one manuscript file before saving.');
      return;
    }

    this.storiesService.addStory({ name, manuscriptFiles, bibleFiles });
    this.resetForm();
  }

  protected onStoryNameInput(event: Event): void {
    this.storyName.set((event.target as HTMLInputElement).value);
  }

  protected onManuscriptFilesChange(files: string[]): void {
    this.manuscriptFiles.set(files);
    this.storiesService.rememberManuscriptFiles(files);
  }

  protected onBibleFilesChange(files: string[]): void {
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
