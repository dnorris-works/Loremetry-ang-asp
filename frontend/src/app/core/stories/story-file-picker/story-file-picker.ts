import { Component, ElementRef, computed, input, output, signal, viewChild } from '@angular/core';

import {
  STORY_FILE_ACCEPT,
  STORY_FILE_EXTENSIONS,
  STORY_FILE_PICKER_TYPES,
} from '../story-file.constants';

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
  readonly options = input<string[]>([]);
  readonly value = input<string[]>([]);
  readonly valueChange = output<string[]>();
  readonly browseError = output<string>();

  protected readonly accept = STORY_FILE_ACCEPT;
  protected readonly isRecentOpen = signal(false);
  protected readonly isListCollapsed = signal(false);
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
        this.emitSelectedFiles(files);
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

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files ? Array.from(input.files) : [];

    if (files.length === 0) {
      return;
    }

    this.emitSelectedFiles(files);
    input.value = '';
  }

  protected selectOption(option: string): void {
    this.valueChange.emit(this.mergeFiles(this.value(), [option]));
    this.isRecentOpen.set(false);
    this.isListCollapsed.set(false);
  }

  protected removeFile(fileName: string): void {
    this.valueChange.emit(this.value().filter((file) => file !== fileName));
  }

  protected filteredOptions(): string[] {
    const selected = new Set(this.value());
    return this.options().filter((option) => !selected.has(option));
  }

  private emitSelectedFiles(files: File[]): void {
    const allowedFiles = files
      .map((file) => file.name)
      .filter((fileName) => this.isAllowedFile(fileName));
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

    this.valueChange.emit(this.mergeFiles(this.value(), allowedFiles));
    this.isRecentOpen.set(false);
    this.isListCollapsed.set(false);
  }

  private mergeFiles(current: string[], next: string[]): string[] {
    return [...new Set([...current, ...next])];
  }

  private isAllowedFile(fileName: string): boolean {
    const lowerName = fileName.toLowerCase();
    return STORY_FILE_EXTENSIONS.some((extension) => lowerName.endsWith(extension));
  }
}
