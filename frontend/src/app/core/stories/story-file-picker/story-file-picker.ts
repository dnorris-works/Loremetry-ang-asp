import { Component, ElementRef, computed, input, output, signal, viewChild } from '@angular/core';

import {
  STORY_FILE_ACCEPT,
  STORY_FILE_EXTENSIONS,
  STORY_FILE_PICKER_TYPES,
} from '../story-file.constants';
import { StoryDocumentInput } from '../story.models';
import { readStoryFile, storyDocumentKey } from '../story-file.utils';

@Component({
  selector: 'app-story-file-picker',
  templateUrl: './story-file-picker.html',
  styleUrl: './story-file-picker.css',
})
export class StoryFilePicker {
  readonly label = input('Files');
  readonly description = input(
    'Choose one or more files (.md, .txt, or .docx), or add from recent files.',
  );
  readonly listTitle = input('Selected files');
  readonly pickerTypeLabel = input('Files');
  readonly options = input<StoryDocumentInput[]>([]);
  readonly value = input<StoryDocumentInput[]>([]);
  readonly valueChange = output<StoryDocumentInput[]>();
  readonly browseError = output<string>();

  protected readonly accept = STORY_FILE_ACCEPT;
  protected readonly isRecentOpen = signal(false);
  protected readonly isListCollapsed = signal(false);
  protected readonly isReading = signal(false);
  protected readonly listboxId = `story-file-listbox-${crypto.randomUUID()}`;

  protected readonly listSummary = computed(
    () => `${this.listTitle()} (${this.value().length})`,
  );

  private readonly fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');

  protected toggleRecentList(): void {
    if (this.filteredOptions().length === 0) {
      return;
    }

    this.isRecentOpen.update((open) => !open);
  }

  protected toggleFileList(): void {
    this.isListCollapsed.update((collapsed) => !collapsed);
  }

  protected async browse(): Promise<void> {
    const openFilePicker = (
      window as Window & {
        showOpenFilePicker?: (options?: {
          multiple?: boolean;
          types?: Array<{
            description: string;
            accept: Record<string, string[]>;
          }>;
        }) => Promise<FileSystemFileHandle[]>;
      }
    ).showOpenFilePicker;

    if (openFilePicker) {
      try {
        const handles = await openFilePicker.call(window, {
          multiple: true,
          types: STORY_FILE_PICKER_TYPES.map((type) => ({
            description: this.pickerTypeLabel(),
            accept: type.accept,
          })),
        });
        const files = await Promise.all(handles.map((handle) => handle.getFile()));
        await this.emitSelectedFiles(files);
        return;
      } catch (error) {
        if (error instanceof DOMException && error.name === 'AbortError') {
          return;
        }

        this.browseError.emit(
          error instanceof Error ? error.message : 'Failed to open the file picker.',
        );
        return;
      }
    }

    this.fileInput()?.nativeElement.click();
  }

  protected async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const files = input.files ? Array.from(input.files) : [];

    if (files.length === 0) {
      return;
    }

    await this.emitSelectedFiles(files);
    input.value = '';
  }

  protected selectOption(option: StoryDocumentInput): void {
    this.valueChange.emit(this.mergeFiles(this.value(), [option]));
    this.isRecentOpen.set(false);
    this.isListCollapsed.set(false);
  }

  protected removeFile(fileName: string): void {
    this.valueChange.emit(
      this.value().filter((file) => file.fileName !== fileName),
    );
  }

  protected filteredOptions(): StoryDocumentInput[] {
    const selected = new Set(this.value().map((file) => storyDocumentKey(file)));
    return this.options().filter((option) => !selected.has(storyDocumentKey(option)));
  }

  private async emitSelectedFiles(files: File[]): Promise<void> {
    const allowedFiles = files.filter((file) => this.isAllowedFile(file.name));
    const rejectedCount = files.length - allowedFiles.length;

    if (allowedFiles.length === 0) {
      this.browseError.emit(
        `Only .md, .txt, and .docx ${this.pickerTypeLabel().toLowerCase()} are supported.`,
      );
      return;
    }

    if (rejectedCount > 0) {
      this.browseError.emit(
        `${rejectedCount} file${rejectedCount === 1 ? '' : 's'} skipped. Only .md, .txt, and .docx are supported.`,
      );
    }

    this.isReading.set(true);

    try {
      const documents = await Promise.all(allowedFiles.map((file) => readStoryFile(file)));
      this.valueChange.emit(this.mergeFiles(this.value(), documents));
      this.isRecentOpen.set(false);
      this.isListCollapsed.set(false);
    } catch (error) {
      this.browseError.emit(
        error instanceof Error ? error.message : 'Failed to read the selected files.',
      );
    } finally {
      this.isReading.set(false);
    }
  }

  private mergeFiles(
    current: StoryDocumentInput[],
    next: StoryDocumentInput[],
  ): StoryDocumentInput[] {
    const merged = new Map<string, StoryDocumentInput>();

    for (const file of [...current, ...next]) {
      merged.set(storyDocumentKey(file), file);
    }

    return [...merged.values()];
  }

  private isAllowedFile(fileName: string): boolean {
    const lowerName = fileName.toLowerCase();
    return STORY_FILE_EXTENSIONS.some((extension) => lowerName.endsWith(extension));
  }
}
