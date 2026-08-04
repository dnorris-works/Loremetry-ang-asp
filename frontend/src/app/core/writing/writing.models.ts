export type WritingDocumentCategory = 'manuscript' | 'character' | 'location';

export interface WritingDocumentContext {
  source: 'story' | 'series';
  parentId: number;
  documentId?: number;
  category: WritingDocumentCategory;
  fileName: string;
  mimeType: string;
}
