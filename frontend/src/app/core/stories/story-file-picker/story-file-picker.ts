import {
  Component,
  ElementRef,
  computed,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';

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
    'Browse, drag and drop, or add from recent files (.md, .txt, or .docx).',
  );
  readonly listTitle = input('Selected files');
  readonly pickerTypeLabel = input('Files');
  readonly collapsible = input(false);
  readonly defaultExpanded = input(false);
  readonly options = input<StoryDocumentInput[]>([]);
  readonly value = input<StoryDocumentInput[]>([]);
  readonly valueChange = output<StoryDocumentInput[]>();
  readonly browseError = output<string>();

  protected readonly accept = STORY_FILE_ACCEPT;
  protected readonly isRecentOpen = signal(false);
  protected readonly isExpanded = signal(false);
  protected readonly isReading = signal(false);
  protected readonly isDragOver = signal(false);
  protected readonly selectedKeys = signal<Set<string>>(new Set());
  protected readonly listboxId = `story-file-listbox-${crypto.randomUUID()}`;

  protected readonly allSelected = computed(() => {
    const files = this.value();
    if (files.length === 0) {
      return false;
    }

    const selected = this.selectedKeys();
    return files.every((file) => selected.has(storyDocumentKey(file)));
  });

  protected readonly selectedCount = computed(() => this.selectedKeys().size);

  private readonly fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');
  private readonly selectAllCheckbox = viewChild<ElementRef<HTMLInputElement>>('selectAllCheckbox');
  private dragDepth = 0;

  constructor() {
    this.isExpanded.set(this.defaultExpanded());

    effect(() => {
      const files = this.value();
      const validKeys = new Set(files.map((file) => storyDocumentKey(file)));

      this.selectedKeys.update((keys) => {
        const next = new Set([...keys].filter((key) => validKeys.has(key)));
        return next.size === keys.size ? keys : next;
      });
    });

    effect(() => {
      const checkbox = this.selectAllCheckbox()?.nativeElement;
      if (!checkbox) {
        return;
      }

      const files = this.value();
      const selected = this.selectedKeys();
      const selectedInList = files.filter((file) => selected.has(storyDocumentKey(file))).length;

      checkbox.indeterminate = selectedInList > 0 && selectedInList < files.length;
      checkbox.checked = files.length > 0 && selectedInList === files.length;
    });
  }

  protected toggleRecentList(): void {
    if (this.filteredOptions().length === 0) {
      return;
    }

    this.isRecentOpen.update((open) => !open);
  }

  protected toggleExpanded(): void {
    this.isExpanded.update((expanded) => !expanded);
  }

  protected showBody(): boolean {
    return !this.collapsible() || this.isExpanded();
  }

  protected onDragEnter(event: DragEvent): void {
    if (this.isReading() || !this.hasFileTransfer(event)) {
      return;
    }

    event.preventDefault();
    this.dragDepth += 1;
    this.isDragOver.set(true);
  }

  protected onDragOver(event: DragEvent): void {
    if (this.isReading() || !this.hasFileTransfer(event)) {
      return;
    }

    event.preventDefault();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'copy';
    }
  }

  protected onDragLeave(event: DragEvent): void {
    if (this.isReading()) {
      return;
    }

    event.preventDefault();
    this.dragDepth = Math.max(0, this.dragDepth - 1);
    if (this.dragDepth === 0) {
      this.isDragOver.set(false);
    }
  }

  protected async onDrop(event: DragEvent): Promise<void> {
    event.preventDefault();
    this.dragDepth = 0;
    this.isDragOver.set(false);

    if (this.isReading()) {
      return;
    }

    const files = event.dataTransfer?.files ? Array.from(event.dataTransfer.files) : [];
    if (files.length === 0) {
      return;
    }

    await this.emitSelectedFiles(files);
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
  }

  protected isSelected(file: StoryDocumentInput): boolean {
    return this.selectedKeys().has(storyDocumentKey(file));
  }

  protected toggleFileSelection(file: StoryDocumentInput): void {
    const key = storyDocumentKey(file);
    this.selectedKeys.update((keys) => {
      const next = new Set(keys);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  }

  protected toggleSelectAll(): void {
    const files = this.value();

    if (this.allSelected()) {
      this.selectedKeys.set(new Set());
      return;
    }

    this.selectedKeys.set(new Set(files.map((file) => storyDocumentKey(file))));
  }

  protected removeSelected(): void {
    const selected = this.selectedKeys();
    this.valueChange.emit(
      this.value().filter((file) => !selected.has(storyDocumentKey(file))),
    );
    this.selectedKeys.set(new Set());
  }

  protected removeFile(fileName: string): void {
    const key = fileName.toLowerCase();
    this.selectedKeys.update((keys) => {
      const next = new Set(keys);
      next.delete(key);
      return next;
    });
    this.valueChange.emit(this.value().filter((file) => file.fileName !== fileName));
  }

  protected filteredOptions(): StoryDocumentInput[] {
    const selected = new Set(this.value().map((file) => storyDocumentKey(file)));
    return this.options().filter((option) => !selected.has(storyDocumentKey(option)));
  }

  protected fileTypeLabel(fileName: string): string {
    const extension = fileName.slice(fileName.lastIndexOf('.')).toLowerCase();
    switch (extension) {
      case '.md':
        return 'Markdown';
      case '.txt':
        return 'Text';
      case '.docx':
        return 'Word';
      default:
        return extension.replace('.', '').toUpperCase() || 'File';
    }
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
      if (this.collapsible()) {
        this.isExpanded.set(true);
      }
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

  private hasFileTransfer(event: DragEvent): boolean {
    const types = event.dataTransfer?.types;
    return types ? [...types].includes('Files') : false;
  }
}
