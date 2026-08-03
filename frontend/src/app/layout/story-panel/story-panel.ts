import { Component, effect, inject, signal } from '@angular/core';

import { StoryFilePicker } from '../../core/stories/story-file-picker/story-file-picker';
import { populateStoryForm } from '../../core/stories/story-form.utils';
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
  protected readonly characterFiles = signal<StoryDocumentInput[]>([]);
  protected readonly locationFiles = signal<StoryDocumentInput[]>([]);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly isEditing = this.storiesService.isEditing;
  protected readonly isPanelLoading = this.storiesService.isPanelLoading;
  protected readonly recentManuscriptFiles = this.storiesService.recentManuscriptFiles;
  protected readonly recentCharacterFiles = this.storiesService.recentCharacterFiles;
  protected readonly recentLocationFiles = this.storiesService.recentLocationFiles;
  protected readonly isSaving = this.storiesService.isSaving;

  constructor() {
    effect(() => {
      const detail = this.storiesService.editingDetail();

      if (detail) {
        const form = populateStoryForm(detail);
        this.storyName.set(form.name);
        this.manuscriptFiles.set(form.manuscripts);
        this.characterFiles.set(form.characters);
        this.locationFiles.set(form.locations);
        this.errorMessage.set(null);
        return;
      }

      if (!this.storiesService.isEditing()) {
        this.resetForm();
      }
    });

    effect(() => {
      const loadError = this.storiesService.panelLoadError();
      if (loadError) {
        this.errorMessage.set(loadError);
      }
    });
  }

  protected close(): void {
    this.resetForm();
    this.storiesService.closePanel();
  }

  protected async save(): Promise<void> {
    this.errorMessage.set(null);

    const name = this.storyName().trim();
    const manuscripts = this.manuscriptFiles();
    const characters = this.characterFiles();
    const locations = this.locationFiles();

    if (!name) {
      this.errorMessage.set('Story name is required.');
      return;
    }

    if (manuscripts.length === 0) {
      this.errorMessage.set('Select at least one manuscript file before saving.');
      return;
    }

    const request = { name, manuscripts, characters, locations };
    const editingId = this.storiesService.editingId();
    const story = editingId
      ? await this.storiesService.updateStory(editingId, request)
      : await this.storiesService.addStory(request);

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

  protected onCharacterFilesChange(files: StoryDocumentInput[]): void {
    this.characterFiles.set(files);
    this.storiesService.rememberCharacterFiles(files);
  }

  protected onLocationFilesChange(files: StoryDocumentInput[]): void {
    this.locationFiles.set(files);
    this.storiesService.rememberLocationFiles(files);
  }

  protected onBrowseError(message: string): void {
    this.errorMessage.set(message);
  }

  private resetForm(): void {
    this.storyName.set('');
    this.manuscriptFiles.set([]);
    this.characterFiles.set([]);
    this.locationFiles.set([]);
    this.errorMessage.set(null);
  }
}
