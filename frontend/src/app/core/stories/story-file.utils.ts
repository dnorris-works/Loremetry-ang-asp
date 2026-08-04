import { StoryDocumentInput } from './story.models';

const DOCX_MIME =
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document';

export interface StoryFileSelection {
  file: File;
  fileName: string;
}

export function isMarkdownFileName(fileName: string): boolean {
  return fileName.toLowerCase().endsWith('.md');
}

export function storyFileRelativePath(file: File): string {
  const relativePath = (file as File & { webkitRelativePath?: string }).webkitRelativePath;
  const raw = relativePath?.trim() ? relativePath : file.name;
  return raw.replace(/\\/g, '/');
}

export function toStoryFileSelection(file: File, fileName?: string): StoryFileSelection {
  return {
    file,
    fileName: fileName ?? storyFileRelativePath(file),
  };
}

export function filterMarkdownFiles(files: File[]): StoryFileSelection[] {
  return files
    .map((file) => toStoryFileSelection(file))
    .filter((selection) => isMarkdownFileName(selection.fileName));
}

export async function collectMarkdownFilesFromDirectory(
  directory: FileSystemDirectoryHandle,
  parentPath = '',
): Promise<StoryFileSelection[]> {
  const selections: StoryFileSelection[] = [];

  for await (const [name, handle] of directory.entries()) {
    const path = parentPath ? `${parentPath}/${name}` : name;

    if (handle.kind === 'file') {
      if (isMarkdownFileName(name)) {
        selections.push({
          file: await handle.getFile(),
          fileName: path,
        });
      }
      continue;
    }

    selections.push(...(await collectMarkdownFilesFromDirectory(handle, path)));
  }

  return selections;
}

export async function collectMarkdownFilesFromDataTransfer(
  dataTransfer: DataTransfer,
): Promise<StoryFileSelection[]> {
  const items = dataTransfer.items ? Array.from(dataTransfer.items) : [];
  const entries = items
    .map((item) => item.webkitGetAsEntry?.())
    .filter((entry): entry is FileSystemEntry => !!entry);

  if (entries.length === 0) {
    return filterMarkdownFiles(Array.from(dataTransfer.files));
  }

  const selections: StoryFileSelection[] = [];
  for (const entry of entries) {
    selections.push(...(await collectMarkdownFilesFromEntry(entry)));
  }

  return selections;
}

async function collectMarkdownFilesFromEntry(
  entry: FileSystemEntry,
  parentPath = '',
): Promise<StoryFileSelection[]> {
  const path = parentPath ? `${parentPath}/${entry.name}` : entry.name;

  if (entry.isFile) {
    if (!isMarkdownFileName(entry.name)) {
      return [];
    }

    const file = await readEntryFile(entry as FileSystemFileEntry);
    return [{ file, fileName: path }];
  }

  if (!entry.isDirectory) {
    return [];
  }

  const reader = (entry as FileSystemDirectoryEntry).createReader();
  const selections: StoryFileSelection[] = [];

  while (true) {
    const entries = await readDirectoryEntries(reader);
    if (entries.length === 0) {
      break;
    }

    for (const child of entries) {
      selections.push(...(await collectMarkdownFilesFromEntry(child, path)));
    }
  }

  return selections;
}

function readEntryFile(entry: FileSystemFileEntry): Promise<File> {
  return new Promise((resolve, reject) => {
    entry.file(resolve, reject);
  });
}

function readDirectoryEntries(reader: FileSystemDirectoryReader): Promise<FileSystemEntry[]> {
  return new Promise((resolve, reject) => {
    reader.readEntries(resolve, reject);
  });
}

export async function readStoryFile(selection: StoryFileSelection): Promise<StoryDocumentInput>;
export async function readStoryFile(file: File, fileName?: string): Promise<StoryDocumentInput>;
export async function readStoryFile(
  fileOrSelection: File | StoryFileSelection,
  fileName?: string,
): Promise<StoryDocumentInput> {
  const file = fileOrSelection instanceof File ? fileOrSelection : fileOrSelection.file;
  const resolvedName =
    fileOrSelection instanceof File
      ? fileName ?? storyFileRelativePath(file)
      : fileOrSelection.fileName;

  const lowerName = resolvedName.toLowerCase();

  if (lowerName.endsWith('.md')) {
    return {
      fileName: resolvedName,
      mimeType: file.type || 'text/markdown',
      textContent: await file.text(),
    };
  }

  if (lowerName.endsWith('.txt')) {
    return {
      fileName: resolvedName,
      mimeType: file.type || 'text/plain',
      textContent: await file.text(),
    };
  }

  if (lowerName.endsWith('.docx')) {
    const buffer = await file.arrayBuffer();
    return {
      fileName: resolvedName,
      mimeType: file.type || DOCX_MIME,
      binaryContentBase64: arrayBufferToBase64(buffer),
    };
  }

  throw new Error(`Unsupported file type: ${resolvedName}`);
}

function arrayBufferToBase64(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = '';

  for (const byte of bytes) {
    binary += String.fromCharCode(byte);
  }

  return btoa(binary);
}

export function storyDocumentKey(document: StoryDocumentInput): string {
  return document.fileName.toLowerCase();
}
