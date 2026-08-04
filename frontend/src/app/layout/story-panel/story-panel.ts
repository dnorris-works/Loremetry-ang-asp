import { Component, effect, inject, signal } from '@angular/core';

import { StoryFilePicker } from '../../core/stories/story-file-picker/story-file-picker';
import { isEditableTextDocument } from '../../core/stories/story-file.utils';
import { populateStoryForm } from '../../core/stories/story-form.utils';
import { StoryDocumentInput } from '../../core/stories/story.models';
import { StoriesService } from '../../core/stories/stories.service';
import { WritingDocumentCategory } from '../../core/writing/writing.models';
import { WritingService } from '../../core/writing/writing.service';

@Component({
  selector: 'app-story-panel',
  imports: [StoryFilePicker],
  templateUrl: './story-panel.html',
  styleUrl: './story-panel.css',
})
export class StoryPanel {
  private readonly storiesService = inject(StoriesService);
  private readonly writingService = inject(WritingService);

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
      const draft = this.storiesService.panelDraft();
      if (draft && this.storiesService.isPanelOpen()) {
        this.storyName.set(draft.name);
        this.manuscriptFiles.set(draft.manuscripts);
        this.characterFiles.set(draft.characters);
        this.locationFiles.set(draft.locations);
        this.errorMessage.set(null);
        return;
      }

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

  protected openManuscript(file: StoryDocumentInput): void {
    this.openFileInEditor(file, 'manuscript');
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

    const parentId = this.storiesService.editingId();
    if (!parentId) {
      this.writingService.openDocument({
        fileName: file.fileName,
        content: file.textContent ?? '',
        mimeType: file.mimeType,
        context: {
          source: 'story',
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
        source: 'story',
        parentId,
        documentId: file.id,
        category,
        fileName: file.fileName,
        mimeType: file.mimeType,
      },
    });
  }

  private persistPanelDraft(): void {
    this.storiesService.setPanelDraft({
      name: this.storyName(),
      manuscripts: this.manuscriptFiles(),
      characters: this.characterFiles(),
      locations: this.locationFiles(),
    });
  }

  private resetForm(): void {
    this.storyName.set('');
    this.manuscriptFiles.set([]);
    this.characterFiles.set([]);
    this.locationFiles.set([]);
    this.errorMessage.set(null);
  }
}
