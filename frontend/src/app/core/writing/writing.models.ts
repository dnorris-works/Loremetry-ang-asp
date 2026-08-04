export type WritingDocumentCategory = 'manuscript' | 'character' | 'location';

export type WritingDestinationSource = 'story' | 'series';

export interface WritingDestination {
  source: WritingDestinationSource;
  id: number;
  name: string;
}

export function writingDestinationKey(source: WritingDestinationSource, id: number): string {
  return `${source}:${id}`;
}

export function parseWritingDestinationKey(key: string): WritingDestinationSource | null {
  if (key.startsWith('story:')) {
    return 'story';
  }

  if (key.startsWith('series:')) {
    return 'series';
  }

  return null;
}

export function writingDestinationId(key: string): number | null {
  const match = key.match(/^(?:story|series):(\d+)$/);
  return match ? Number(match[1]) : null;
}

export interface WritingDocumentContext {
  source: 'story' | 'series';
  parentId: number;
  documentId?: number;
  category: WritingDocumentCategory;
  fileName: string;
  mimeType: string;
}
